using System.Collections.Generic;
using System.Flow;
using System.GameSession;
using System.Threading;
using CyclingLove.Audio;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Audio;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace System.Service
{
    public interface IAudioClipProvider
    {
        UniTask<AudioClip> LoadAsync(AudioContentEntry entry, CancellationToken cancellationToken);
        void Release(AudioClip clip);
    }

    public sealed class AddressablesAudioClipProvider : IAudioClipProvider
    {
        private readonly Dictionary<AudioClip, List<AsyncOperationHandle<AudioClip>>> _handles = new();

        public async UniTask<AudioClip> LoadAsync(AudioContentEntry entry, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (entry == null) return null;
            if (!string.IsNullOrWhiteSpace(entry.ResourcePath))
                return UnityEngine.Resources.Load<AudioClip>(entry.ResourcePath);
            if (string.IsNullOrWhiteSpace(entry.Address)) return null;

            var locations = Addressables.LoadResourceLocationsAsync(entry.Address, typeof(AudioClip));
            try
            {
                await locations.Task;
                cancellationToken.ThrowIfCancellationRequested();
                if (locations.Status != AsyncOperationStatus.Succeeded ||
                    locations.Result == null || locations.Result.Count == 0)
                    return null;
            }
            finally
            {
                if (locations.IsValid()) Addressables.Release(locations);
            }

            var handle = Addressables.LoadAssetAsync<AudioClip>(entry.Address);
            try
            {
                await handle.Task;
                cancellationToken.ThrowIfCancellationRequested();
                if (handle.Status != AsyncOperationStatus.Succeeded || handle.Result == null)
                {
                    if (handle.IsValid()) Addressables.Release(handle);
                    return null;
                }
                if (!_handles.TryGetValue(handle.Result, out var clipHandles))
                    _handles[handle.Result] = clipHandles = new List<AsyncOperationHandle<AudioClip>>();
                clipHandles.Add(handle);
                return handle.Result;
            }
            catch
            {
                if (handle.IsValid()) Addressables.Release(handle);
                throw;
            }
        }

        public void Release(AudioClip clip)
        {
            if (clip == null || !_handles.Remove(clip, out var handles)) return;
            foreach (var handle in handles)
                if (handle.IsValid()) Addressables.Release(handle);
        }
    }

    /// <summary>
    /// Logical audio IDs are resolved through AudioCatalog. The provider can be replaced
    /// later when encrypted bundles or a custom download cache are introduced.
    /// </summary>
    public sealed class GameAudioService : MonoSingleton<GameAudioService>
    {
        private const float CrossfadeSeconds = 0.35f;
        public const string MixerResourcePath = "Audio/GameAudioMixer";

        [Header("Optional mixer routing")]
        [SerializeField] private AudioMixerGroup bgmGroup;
        [SerializeField] private AudioMixerGroup ambientGroup;
        [SerializeField] private AudioMixerGroup seGroup;

        private readonly Dictionary<string, AudioClip> _clipCache = new(StringComparer.Ordinal);
        private readonly HashSet<string> _unavailableClipIds = new(StringComparer.Ordinal);
        private readonly HashSet<string> _missingResourceWarnings = new(StringComparer.Ordinal);
        private AudioContentCatalog _catalog;
        private IAudioClipProvider _provider;
        private AudioSource[] _bgmSources;
        private AudioSource[] _ambientSources;
        private AudioSource _seSource;
        private int _activeBgm;
        private int _activeAmbient;
        private int _bgmGeneration;
        private int _ambientGeneration;
        private string _requestedBgmId;
        private string _requestedAmbientId;
        private string _lastEnterSeFingerprint;

        protected override void Awake()
        {
            base.Awake();
            _provider = new AddressablesAudioClipProvider();
            ResolveMixerGroups();
            _bgmSources = CreatePair("BGM", bgmGroup);
            _ambientSources = CreatePair("Ambient", ambientGroup);
            _seSource = CreateSource("SE", seGroup);
            try { _catalog = AudioContentLoader.LoadRuntime(); }
            catch (Exception ex) { Debug.LogError($"Audio catalog could not be loaded: {ex.Message}"); }
            RefreshConfiguration();
        }

        public void RefreshConfiguration()
        {
            var settings = global::System.GameSession.GameSession.SystemConfig?.Audio ?? new AudioSettingsData();
            settings.Normalize();
            ApplyLoopVolume(_bgmSources, _activeBgm, _requestedBgmId, settings.BgmVolume);
            ApplyLoopVolume(_ambientSources, _activeAmbient, _requestedAmbientId, settings.AmbientVolume);
            if (_seSource != null) _seSource.volume = 1f;
        }

        public void ConfigureProvider(IAudioClipProvider provider)
        {
            if (provider == null) throw new ArgumentNullException(nameof(provider));
            ResetToSilence();
            ReleaseCachedClips();
            _provider = provider;
            _unavailableClipIds.Clear();
        }

        public void ConfigureMixerGroups(
            AudioMixerGroup bgm, AudioMixerGroup ambient, AudioMixerGroup se)
        {
            bgmGroup = bgm;
            ambientGroup = ambient;
            seGroup = se;
            AssignGroup(_bgmSources, bgmGroup);
            AssignGroup(_ambientSources, ambientGroup);
            if (_seSource != null) _seSource.outputAudioMixerGroup = seGroup;
        }

        public void Synchronize(GameSceneRequest request)
        {
            if (_catalog == null || request == null) return;
            var cue = _catalog.Resolve(request);
            PlayBgm(cue.BgmId);
            PlayAmbient(cue.AmbientId);

            if (string.IsNullOrWhiteSpace(cue.EnterSeId))
            {
                _lastEnterSeFingerprint = null;
                return;
            }
            var fingerprint = string.Join("|", request.ScenarioId, request.NodeId, cue.EnterSeId);
            if (_lastEnterSeFingerprint == fingerprint) return;
            _lastEnterSeFingerprint = fingerprint;
            PlaySe(cue.EnterSeId);
        }

        public void PlayBgm(string id)
        {
            if (_requestedBgmId == id) return;
            _requestedBgmId = id;
            SwitchLoopAsync(id, true, ++_bgmGeneration, this.GetCancellationTokenOnDestroy()).Forget();
        }

        public void PlayAmbient(string id)
        {
            if (_requestedAmbientId == id) return;
            _requestedAmbientId = id;
            SwitchLoopAsync(id, false, ++_ambientGeneration, this.GetCancellationTokenOnDestroy()).Forget();
        }

        public void PlaySe(string id) => PlaySeAsync(id, this.GetCancellationTokenOnDestroy()).Forget();

        public void ResetToSilence()
        {
            _requestedBgmId = null;
            _requestedAmbientId = null;
            _lastEnterSeFingerprint = null;
            _bgmGeneration++;
            _ambientGeneration++;
            StopPair(_bgmSources);
            StopPair(_ambientSources);
            _seSource?.Stop();
        }

        private async UniTaskVoid SwitchLoopAsync(
            string id, bool isBgm, int generation, CancellationToken cancellationToken)
        {
            try
            {
                var pair = isBgm ? _bgmSources : _ambientSources;
                var entry = Find(id, isBgm ? AudioContentKinds.Bgm : AudioContentKinds.Ambient);
                var clip = await LoadClipAsync(entry, cancellationToken);
                if (generation != (isBgm ? _bgmGeneration : _ambientGeneration)) return;

                var oldIndex = isBgm ? _activeBgm : _activeAmbient;
                var newIndex = 1 - oldIndex;
                var outgoing = pair[oldIndex];
                var incoming = pair[newIndex];
                var targetVolume = entry == null ? 0f : EffectiveVolume(
                    entry.Volume * (isBgm ? Settings.BgmVolume : Settings.AmbientVolume));

                incoming.Stop();
                incoming.clip = clip;
                incoming.loop = entry?.Loop ?? true;
                incoming.volume = 0f;
                if (clip != null) incoming.Play();
                await CrossfadeAsync(outgoing, incoming, targetVolume, generation, isBgm, cancellationToken);
                if (generation != (isBgm ? _bgmGeneration : _ambientGeneration)) return;
                outgoing.Stop();
                outgoing.clip = null;
                if (isBgm) _activeBgm = newIndex;
                else _activeAmbient = newIndex;
            }
            catch (OperationCanceledException) { }
            catch (Exception ex) { Debug.LogWarning($"Audio switch failed [{id}]: {ex.GetType().Name}"); }
        }

        private async UniTask CrossfadeAsync(
            AudioSource outgoing, AudioSource incoming, float targetVolume, int generation,
            bool isBgm, CancellationToken cancellationToken)
        {
            var startVolume = outgoing.volume;
            var elapsed = 0f;
            while (elapsed < CrossfadeSeconds)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (generation != (isBgm ? _bgmGeneration : _ambientGeneration)) return;
                elapsed += Time.unscaledDeltaTime;
                var progress = Mathf.Clamp01(elapsed / CrossfadeSeconds);
                outgoing.volume = Mathf.Lerp(startVolume, 0f, progress);
                incoming.volume = Mathf.Lerp(0f, targetVolume, progress);
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }
            outgoing.volume = 0f;
            incoming.volume = targetVolume;
        }

        private async UniTaskVoid PlaySeAsync(string id, CancellationToken cancellationToken)
        {
            try
            {
                var entry = Find(id, AudioContentKinds.Se);
                var clip = await LoadClipAsync(entry, cancellationToken);
                if (entry == null || clip == null || _seSource == null) return;
                _seSource.PlayOneShot(clip, EffectiveVolume(Settings.SeVolume * entry.Volume));
            }
            catch (OperationCanceledException) { }
            catch (Exception ex) { Debug.LogWarning($"Audio SE failed [{id}]: {ex.GetType().Name}"); }
        }

        private AudioContentEntry Find(string id, string expectedKind)
        {
            if (string.IsNullOrWhiteSpace(id) || _catalog == null) return null;
            var entry = _catalog.Find(id);
            return entry?.Kind == expectedKind ? entry : null;
        }

        private async UniTask<AudioClip> LoadClipAsync(
            AudioContentEntry entry, CancellationToken cancellationToken)
        {
            if (entry == null) return null;
            if (_clipCache.TryGetValue(entry.Id, out var cached)) return cached;
            if (_unavailableClipIds.Contains(entry.Id)) return null;
            var clip = await _provider.LoadAsync(entry, cancellationToken);
            if (clip != null) _clipCache[entry.Id] = clip;
            else
            {
                _unavailableClipIds.Add(entry.Id);
                if (!string.IsNullOrWhiteSpace(entry.ResourcePath) && _missingResourceWarnings.Add(entry.Id))
                    Debug.LogWarning($"Audio clip is missing [{entry.Id}]: {entry.ResourcePath}");
            }
            return clip;
        }

        private AudioSource[] CreatePair(string prefix, AudioMixerGroup group) => new[]
        {
            CreateSource(prefix + " A", group),
            CreateSource(prefix + " B", group)
        };

        private AudioSource CreateSource(string sourceName, AudioMixerGroup group)
        {
            var child = new GameObject(sourceName);
            child.transform.SetParent(transform, false);
            var source = child.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.outputAudioMixerGroup = group;
            return source;
        }

        private void ResolveMixerGroups()
        {
            if (bgmGroup != null && ambientGroup != null && seGroup != null) return;
            var mixer = UnityEngine.Resources.Load<AudioMixer>(MixerResourcePath);
            if (mixer == null) return;
            if (bgmGroup == null) bgmGroup = FindGroup(mixer, "BGM");
            if (ambientGroup == null) ambientGroup = FindGroup(mixer, "Ambient");
            if (seGroup == null) seGroup = FindGroup(mixer, "SE");
            if (bgmGroup == null || ambientGroup == null || seGroup == null)
                Debug.LogWarning("GameAudioMixer requires BGM, Ambient, and SE groups");
        }

        private static AudioMixerGroup FindGroup(AudioMixer mixer, string name)
        {
            foreach (var group in mixer.FindMatchingGroups(string.Empty))
                if (group.name == name) return group;
            return null;
        }

        private static void AssignGroup(AudioSource[] sources, AudioMixerGroup group)
        {
            if (sources == null) return;
            foreach (var source in sources)
                if (source != null) source.outputAudioMixerGroup = group;
        }

        private void ApplyLoopVolume(AudioSource[] sources, int activeIndex, string id, float busVolume)
        {
            if (sources == null || activeIndex < 0 || activeIndex >= sources.Length) return;
            var source = sources[activeIndex];
            if (source == null || source.clip == null) return;
            var entry = _catalog?.Find(id);
            source.volume = EffectiveVolume(busVolume * (entry?.Volume ?? 1f));
        }

        private static void StopPair(AudioSource[] sources)
        {
            if (sources == null) return;
            foreach (var source in sources)
            {
                if (source == null) continue;
                source.Stop();
                source.clip = null;
                source.volume = 0f;
            }
        }

        private void ReleaseCachedClips()
        {
            if (_provider != null)
                foreach (var clip in _clipCache.Values)
                    if (clip != null) _provider.Release(clip);
            _clipCache.Clear();
            _unavailableClipIds.Clear();
        }

        private void OnDestroy() => ReleaseCachedClips();

        private AudioSettingsData Settings =>
            global::System.GameSession.GameSession.SystemConfig?.Audio ?? new AudioSettingsData();

        private float EffectiveVolume(float value) => Settings.Muted
            ? 0f
            : Mathf.Clamp01(Settings.MasterVolume * value);
    }
}
