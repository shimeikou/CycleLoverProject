using System;
using CyclingLove.Manager;
using CyclingLove.Services;
using CyclingLove.Session;
using Cysharp.Threading.Tasks;
using UI;
using UnityEngine.SceneManagement;
using UnityEngine;

namespace Scene.Init
{
    public sealed class InitScenePresenter : MonoBehaviour
    {
        [SerializeField] private InitSceneViewer view;
        private bool _busy;
        private AudioSettingsData _audioSettingsBeforeEdit;
        private async void Start()
        {
            try
            {
                await new InitSceneModel().Init();
                GameAudioService.Instance.RefreshConfiguration();
                GameAudioService.Instance.ResetToSilence();
                StandardUIManager.Instance?.HideLoading();
                view.Init(() => Open(false).Forget(), () => Open(true).Forget(),
                    OpenAudioSettings, () => Application.Quit());
                view.SetLoadAvailable(SaveDataService.Exists(GameSession.CurrentSaveSlot));
                view.Show();
            }
            catch (Exception ex) { Debug.LogException(ex); view.SetMessage("タイトルの初期化に失敗しました。"); }
        }
        private async UniTask Open(bool load)
        {
            if (_busy || !view.IsInit || view.IsTransition) return;
            _busy = true;
            view.SetBusy(true);
            try
            {
                if (load) await GameFlowManager.Instance.LoadGameFlow();
                else await GameFlowManager.Instance.StartNewGameFlow();
                StandardUIManager.Instance?.ShowLoading();
                await SceneManager.LoadSceneAsync("MainGameScene");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Start/load failed: {ex.GetType().Name}");
                view.SetMessage(load ? "読み込めませんでした。セーブが破損しているか、この試作に対応していません。新しく始めることはできます。" : "開始できませんでした。もう一度お試しください。");
                StandardUIManager.Instance?.HideLoading();
                _busy = false; view.SetBusy(false);
            }
        }

        private void OpenAudioSettings()
        {
            if (_busy) return;
            GameSession.SystemConfig.Audio ??= new AudioSettingsData();
            GameSession.SystemConfig.Audio.Normalize();
            _audioSettingsBeforeEdit = GameSession.SystemConfig.Audio.Snapshot();
            view.ShowAudioSettings(
                _audioSettingsBeforeEdit,
                PreviewAudioSettings,
                settings => PersistAudioSettings(settings).Forget(),
                CancelAudioSettings);
        }

        private void PreviewAudioSettings(AudioSettingsData settings)
        {
            GameSession.SystemConfig.Audio.CopyFrom(settings);
            GameAudioService.Instance.RefreshConfiguration();
        }

        private void CancelAudioSettings()
        {
            if (_audioSettingsBeforeEdit != null)
                GameSession.SystemConfig.Audio.CopyFrom(_audioSettingsBeforeEdit);
            GameAudioService.Instance.RefreshConfiguration();
            _audioSettingsBeforeEdit = null;
            view.SetMessage("サウンド設定の変更を取り消しました。");
        }

        private async UniTask PersistAudioSettings(AudioSettingsData settings)
        {
            if (_busy) return;
            _busy = true;
            view.SetBusy(true);
            PreviewAudioSettings(settings);
            try
            {
                await SaveDataService.SaveSystem(GameSession.SystemConfig);
                _audioSettingsBeforeEdit = null;
                view.SetMessage("サウンド設定を保存しました。");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Audio settings save failed: {ex.GetType().Name}");
                view.SetMessage("設定を保存できませんでした。変更は今回の起動中だけ反映されます。");
            }
            finally
            {
                _busy = false;
                if (view != null) view.SetBusy(false);
            }
        }
    }
}
