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
        
        public bool IsInit { get; private set; }
        public bool IsReady { get; private set; }
        public void Init(
            UnityAction onClickStart,
            UnityAction onClickLoad,
            UnityAction onClickConfig,
            UnityAction onClickExit)
        {
            IsInit = false;
            volume.profile.TryGet(out ColorAdjustments colorAdjustments);
            if (!colorAdjustments) return;
            var originalColor = colorAdjustments.colorFilter.value;
            var black = new Color(0, 0, 0, 0);
            colorAdjustments.colorFilter.value = black;

            SetUpButtons(onClickStart, onClickLoad, onClickConfig, onClickExit);

            IsInit = true;
        }

        private void SetUpButtons(UnityAction onClickStart, UnityAction onClickLoad, UnityAction onClickConfig, UnityAction onClickExit)
        {
            startButton.onClick.RemoveAllListeners();
            startButton.onClick.AddListener((()=>
            {
                
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
            volume.profile.TryGet(out ColorAdjustments colorAdjustments);
            if(!colorAdjustments) return;
            IsReady = false;
            DOTween.To(
                () => colorAdjustments.colorFilter.value,
                x => colorAdjustments.colorFilter.value = x,
                Color.white,
                1.0f
            ).OnComplete(() =>
            {
                IsReady = true;
            });
        }
    }
}
