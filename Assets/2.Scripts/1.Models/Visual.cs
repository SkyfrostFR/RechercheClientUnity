using System;
using System.Collections.Generic;
using UnityEngine;

namespace DT.Model
{
    [Serializable]
    public class Visual
    {
        [field: SerializeField]
        public Origin         Origin   { get; set; } // Origine de la visualisation p/r à la piece
        [field: SerializeField]
        public List<Geometry> Geometry { get; set; } // Liste des géométries associées
        [field: SerializeField]
        public bool           Show     { get; set; } // Affichage du visuel de la piece


        public Visual()
        {
            Geometry = new List<Geometry>();
            Show = true;
            Origin = new Origin();
        }

    }
}


