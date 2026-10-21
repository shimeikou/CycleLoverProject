using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.GameSession;


namespace Scene.Init
{
    public class InitSceneViewer : MonoBehaviour
    {
        [SerializeField]
        private Image background;
        [SerializeField]
        private TextMeshProUGUI text;
        
        [SerializeField]
        private Button startButton;
        
        [SerializeField]
        private Button loadButton;
        
        [SerializeField]
        private Button configButton;

        [SerializeField]
        private Button exitButton;
        
        [SerializeField]
        private Camera mainCameraTitle;
        
        [SerializeField]
        private Volume volume;

        [SerializeField] private TextMeshProUGUI statusText;
        private AudioSettingsPanel _audioSettingsPanel;
        private Coroutine _colorTransition;
        private bool _loadAvailable;
        public void SetMessage(string message) { if (statusText != null) statusText.text = message; }
        public void SetLoadAvailable(bool available) { _loadAvailable = available; loadButton.interactable = available; }
        public void SetBusy(bool busy)
        {
            startButton.interactable = !busy;
            loadButton.interactable = !busy && _loadAvailable;
            configButton.interactable = !busy;
            exitButton.interactable = !busy;
        }
        private ColorAdjustments _colorAdjustments;

        public bool IsInit { get; set; }
        public bool IsTransition{ get; private set; }
        public void Init(
            UnityAction onClickStart,
            UnityAction onClickLoad,
            UnityAction onClickConfig,
            UnityAction onClickExit)
        {
            IsInit = false;
            if (volume != null && volume.profile != null) volume.profile.TryGet(out _colorAdjustments);

            var black = new Color(0, 0, 0, 0);
            if (_colorAdjustments != null) _colorAdjustments.colorFilter.value = black;

            SetUpButtons(onClickStart, onClickLoad, onClickConfig, onClickExit);

            IsInit = true;
        }

        private void SetUpButtons(UnityAction onClickStart, UnityAction onClickLoad, UnityAction onClickConfig, UnityAction onClickExit)
        {
            startButton.onClick.RemoveAllListeners();
            startButton.onClick.AddListener((()=>
            {
                if(!IsInit)return;
                onClickStart?.Invoke();
                
            }));
            
            loadButton.onClick.RemoveAllListeners();
            loadButton.onClick.AddListener(onClickLoad);

            configButton.onClick.RemoveAllListeners();
            configButton.onClick.AddListener(onClickConfig);

            exitButton.onClick.RemoveAllListeners();
            exitButton.onClick.AddListener(onClickExit);
        }

        public void Show()
        {
            BeginColorTransition(Color.white);
        }

        public bool ShowAudioSettings(
            AudioSettingsData settings,
            global::System.Action<AudioSettingsData> onPreview,
            global::System.Action<AudioSettingsData> onApply,
            global::System.Action onCancel)
        {
            if (_audioSettingsPanel == null)
            {
                var canvas = GetComponentInParent<Canvas>();
                if (canvas == null) canvas = FindAnyObjectByType<Canvas>();
                if (canvas == null)
                {
                    SetMessage("設定画面を表示できませんでした。");
                    return false;
                }
                _audioSettingsPanel = AudioSettingsPanel.Create(canvas.transform);
            }

            SetBusy(true);
            _audioSettingsPanel.Show(settings, onPreview, onApply, () =>
            {
                onCancel?.Invoke();
                SetBusy(false);
            });
            return true;
        }

        public void HideAndShowLoading(Color color,UnityAction onComplete = null)
        {
            if(IsTransition) return;

            BeginColorTransition(color, onComplete);
        }

        private void BeginColorTransition(Color target, UnityAction onComplete = null)
        {
            if (!_colorAdjustments)
            {
                if (volume != null && volume.profile != null) volume.profile.TryGet(out _colorAdjustments);
            }

            if (!_colorAdjustments)
            {
                IsTransition = false;
                onComplete?.Invoke();
                return;
            }

            if (_colorTransition != null) StopCoroutine(_colorTransition);
            _colorTransition = StartCoroutine(AnimateColor(target, onComplete));
        }

        private IEnumerator AnimateColor(Color target, UnityAction onComplete)
        {
            IsTransition = true;
            var source = _colorAdjustments.colorFilter.value;
            var elapsed = 0f;
            const float duration = 1f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                _colorAdjustments.colorFilter.value = Color.Lerp(source, target, Mathf.Clamp01(elapsed / duration));
                yield return null;
            }

            _colorAdjustments.colorFilter.value = target;
            _colorTransition = null;
            IsTransition = false;
            onComplete?.Invoke();
        }
    }
}
