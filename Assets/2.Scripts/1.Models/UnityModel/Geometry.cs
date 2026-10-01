using Newtonsoft.Json;
using System;
using UnityEngine;

namespace DT.Model
{
    [Serializable]
    public partial class Geometry
    {
        // Serialized by Unity so the name survives domain reload.
        // gameObject is rebuilt at runtime via EnsureLoaded().
        [field: SerializeField]
        public string gameObjectName { get; set; }

        [JsonIgnore]
        public GameObject gameObject { get; set; }

        public Geometry() { }
        public Geometry(GameObject a_gameObject)
        {
            gameObject = a_gameObject;
            gameObjectName = a_gameObject.name;
        }

        /// <summary>
        /// Assigns gameObject from gameObjectName (GameObject refs are not JSON-serializable).
        /// Falls back to GameObject.Find — avoid duplicate names in the scene.
        /// </summary>
        public void EnsureLoaded()
        {
            if (gameObject == null && !string.IsNullOrEmpty(gameObjectName))
                gameObject = GameObject.Find(gameObjectName);
        }
    }
}
