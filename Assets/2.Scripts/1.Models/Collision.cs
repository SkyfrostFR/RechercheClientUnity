using System.Collections.Generic;
using UnityEngine;

namespace DT.Model
{
    [System.Serializable]
    public class Collision
    {

        [field: SerializeField]
        public Origin Origin           { get; set; } // Origine p/r à la piece
        [field: SerializeField]
        public List<Geometry> Geometry { get; set; } // Liste des géométries associées
        [field: SerializeField]
        public bool Show { get; set; }


        public Collision()
        {
            Geometry = new List<Geometry>();
            Show = true;
            Origin = new Origin();
        }
    }
}
