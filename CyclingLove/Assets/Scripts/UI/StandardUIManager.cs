using System;
using UnityEngine;

namespace UI
{
    public class StandardUIManager : MonoBehaviour
    {
        // --- シングルトンパターン ---
        public static StandardUIManager Instance { get; private set; }

        // --- UIコンポーネントの参照 ---
        [Header("ローディング関連")]
        [SerializeField] private GameObject loadingIconRoot; // ローディングアイコンのルート
        [SerializeField] private CanvasGroup loadingCanvasGroup; // フェード制御用
        [SerializeField] private float fadeDuration = 0.3f;
   
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
               
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            loadingIconRoot.SetActive(false);
        }

        public void ShowLoading()
        {
            loadingIconRoot.SetActive(true);
        }
        
    }
}