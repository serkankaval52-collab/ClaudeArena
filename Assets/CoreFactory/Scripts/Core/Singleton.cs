using UnityEngine;

namespace CoreFactory.Core
{
    public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T _instance;
        private static bool _isQuitting;

        public static bool HasInstance => _instance != null;

        public static T Instance
        {
            get
            {
                if (_isQuitting) return null;
                if (_instance != null) return _instance;

#if UNITY_2023_1_OR_NEWER
                _instance = FindAnyObjectByType<T>(FindObjectsInactive.Include);
#else
                _instance = (T)FindObjectOfType(typeof(T), true);
#endif
                if (_instance == null)
                {
                    var obj = new GameObject($"{typeof(T).Name} (Singleton)");
                    _instance = obj.AddComponent<T>();
                    DontDestroyOnLoad(obj);
                }
                return _instance;
            }
        }

        protected bool IsAuthoritativeInstance => (_instance == this);

        protected virtual void Awake()
        {
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

        protected virtual void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        private void OnApplicationQuit()
        {
            _isQuitting = true;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            _instance = null;
            _isQuitting = false;
        }
    }
}