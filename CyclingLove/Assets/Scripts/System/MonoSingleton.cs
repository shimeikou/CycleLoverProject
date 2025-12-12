using UnityEngine;

namespace System
{
    public abstract　class MonoSingleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T _instance;
        private static bool _quitting = false;

        public static T Instance
        {
            get
            {
                if (_quitting) return null;

                if (_instance != null) return _instance;

                // シーン上の既存を検索
                _instance = FindFirstObjectByType<T>();
                if (_instance != null) return _instance;

                // 自動生成
                var obj = new GameObject($"(Singleton) {typeof(T).Name}");
                _instance = obj.AddComponent<T>();

                DontDestroyOnLoad(obj);
                return _instance;
            }
        }

        protected virtual void Awake()
        {
            // Instance に触らずに _instance を確認する
            if (_instance == null)
            {
                _instance = this as T;
                DontDestroyOnLoad(gameObject);
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void OnApplicationQuit()
        {
            _quitting = true;
        }
    }
}