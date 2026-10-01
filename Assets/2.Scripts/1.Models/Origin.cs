using UnityEngine; // Requis pour le type Vector3 (System.Numerics sinon ...)

namespace DT.Model
{
    [System.Serializable]
    public class Origin
    {
        [field: SerializeField]
        public Vector3 XYZ { get; set; } // Position
        [field: SerializeField]
        public Vector3 RPY { get; set; } // Roll Pitch Yaw

        public Origin()
        {
            XYZ = new Vector3();
            RPY = new Vector3();
        }
    }
}
