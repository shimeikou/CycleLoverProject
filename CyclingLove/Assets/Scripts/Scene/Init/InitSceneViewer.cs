using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;


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
            if (!_colorAdjustments)
            {
                if (volume != null && volume.profile != null) volume.profile.TryGet(out _colorAdjustments);
            }

            if(!_colorAdjustments) return;
            IsTransition = true;
            DOTween.To(
                () => _colorAdjustments.colorFilter.value,
                x => _colorAdjustments.colorFilter.value = x,
                Color.white,
                1.0f
            ).OnComplete(() =>
            {
                IsTransition = false;
            });
        }

        public void HideAndShowLoading(Color color,UnityAction onComplete = null)
        {
            if(IsTransition) return;
            
            if (!_colorAdjustments)
            {
                if (volume != null && volume.profile != null) volume.profile.TryGet(out _colorAdjustments);
            }

            if(!_colorAdjustments) return;

            IsTransition = true;
            DOTween.To(
                () => _colorAdjustments.colorFilter.value,
                x => _colorAdjustments.colorFilter.value = x,
                color,
                1.0f
            ).OnComplete(() =>
            {
                onComplete?.Invoke();
                IsTransition = false;
            });
        }
    }
}
