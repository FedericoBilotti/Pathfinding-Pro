using UnityEngine;

namespace Utilities
{
    [DefaultExecutionOrder(-5000)]
    public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        public static T Instance;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this as T;
            InitializeSingleton();
        }

        protected virtual void InitializeSingleton()
        {

        }
    }
}