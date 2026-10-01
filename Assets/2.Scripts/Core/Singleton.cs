using System.Threading;
using UnityEngine;

namespace DT
{
    /// <summary>
    /// A simple class to easily use monobehaviours as singletons.
    /// If no instance of the singleton is present in the scene, then no new instance is created
    /// (in order to prefer null reference exceptions over "hidden" reinstantiation of gameobjects, which
    /// can create issues when unloading the scene and are a bad idea anyway).
    /// If multiple instances of the singleton exist, then the extra instances automatically delete themselves
    /// and display an error in the console.
    ///
    /// (Further rambling: singletons are often viewed as an anti-pattern, and implementing them correctly in
    /// Unity is tricky due to how constructors and Start/Awake methods work (as well as their calling order
    /// in regards to other scripts).
    /// Ideally we'd use some sort of dependency injection instead. But for now this should be enough...)
    /// </summary>
    /// <typeparam name="T">the specialized type inheriting this class</typeparam>
    public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        // Check to see if we're about to be destroyed.
        private static bool m_ShuttingDown = false;
        private static object m_Lock = new object();
        private static T m_Instance;

        private static Thread unityMainThread = null;

        /// <summary>
        /// Access singleton instance through this propriety.
        /// </summary>
        public static T Instance
        {
            get
            {
                if (m_ShuttingDown)
                {
                    Debug.LogWarning("[Singleton] Instance '" + typeof(T) +
                        "' already destroyed. Returning null.");
                    return null;
                }

                lock (m_Lock)
                {
                    if (m_Instance == null)
                    {

                        if (unityMainThread == Thread.CurrentThread)
                        {
                            // Search for existing instance.
                            m_Instance = (T)FindObjectOfType(typeof(T));

                            // Create new instance if one doesn't already exist.
                            if (m_Instance == null)
                            {
                                // Need to create a new GameObject to attach the singleton to.
                                var singletonObject = new GameObject();
                                m_Instance = singletonObject.AddComponent<T>();
                                singletonObject.name = typeof(T).ToString() + " (Singleton)";

                                // Make instance persistent.
                                DontDestroyOnLoad(singletonObject);
                            }
                        }
                        
                    }

                    return m_Instance;
                }
            }
        }

        /*private void OnApplicationQuit()
        {
            m_ShuttingDown = true;
        }*/

        protected virtual void Awake()
        {
            unityMainThread = Thread.CurrentThread;
            if (Instance != this)
            {

            }
            
        }

        /*private void OnDestroy()
        {
            m_ShuttingDown = true;
        }

        private void OnValidate()
        {
            m_ShuttingDown = true;
        }*/
    }
}

