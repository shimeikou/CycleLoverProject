using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Util;

// イベントシステム関連のインターフェース

namespace UI
{
    [RequireComponent(typeof(Button))]
    public class SafeScalableButtonText : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [Header("Target & Settings")] 
        
        [SerializeField] private TextMeshProUGUI targetText = null;
        [SerializeField] private float pressedScale = 1.1f;
        [SerializeField] private float duration = 0.1f;
        
        private Vector3 _defaultScale;
        private Tween _currentTween;

        private void Awake()
        {


            if (targetText == null)
            {
                GameLogger.LogError("TextMeshProUGUIコンポーネントが子要素に見つかりません。", this);
                enabled = false;
                return;
            }
        
            _defaultScale = targetText.transform.localScale;
        }

        private void OnDisable()
        {
            // オブジェクトが無効化されたら、アニメーションを停止し、スケールをリセット
            ResetScale();
        }
    
        // --- IPointerDownHandler ---
        // マウスがボタン上で押されたときに呼び出される
        public void OnPointerDown(PointerEventData eventData)
        {
            AnimateScale(_defaultScale * pressedScale);
        }

        // --- IPointerUpHandler ---
        // マウスがどこかで離されたときに呼び出される
        public void OnPointerUp(PointerEventData eventData)
        {
            // 離されたら元のスケールに戻す
            AnimateScale(_defaultScale);
        }
    
        // --- IPointerExitHandler ---
        // 押した状態でボタンの外にマウスが移動した場合に呼び出される（状態リセットのため）
        public void OnPointerExit(PointerEventData eventData)
        {
            // 押しっぱなしで外れた場合も元のスケールに戻す
            if (eventData.pointerPress == gameObject)
            {
                AnimateScale(_defaultScale);
            }
        }

        private void AnimateScale(Vector3 targetScale)
        {
            _currentTween?.Kill(); // 既存のアニメーションを終了
            _currentTween = targetText.transform.DOScale(targetScale, duration).SetAutoKill(false);
        }

        private void ResetScale()
        {
            _currentTween?.Kill();
            if (targetText != null)
            {
                targetText.transform.localScale = _defaultScale;
            }
        }
    }
}