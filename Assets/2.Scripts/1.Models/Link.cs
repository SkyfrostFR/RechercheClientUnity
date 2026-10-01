using System;
using System.Collections.Generic;
using UnityEngine;

namespace DT.Model
{
    [Serializable]
    public partial class LinkModel : BaseModel
    {
        [field: SerializeField]
        public string Name         { get; set; } // Nom de la pièce
        [field: SerializeField]
        public string Description  { get; set; } // Description de la pièce
        [field: SerializeField]
        public string Img          { get; set; }  // Image de la pièce
        [field: SerializeField]
        public Visual Visual       { get; set; } // Représentation 3D/CAD
        [field: SerializeField]
        public Collision Collision { get; set; } // Représentation 3D de l'enveloppe
        [field: SerializeField]
        public Inertial Inertial   { get; set; } // Propriété inertielles


        public LinkModel()
        {
            Visual = new Visual();
            Collision = new Collision();
            Inertial = new Inertial();
        }

    }


    [Serializable]
    public partial class Link : BaseModel
    {
        [field: SerializeField]
        public string    Name       { get; set; } // Nom de la pièce
        [field: SerializeField]
        public LinkModel LinkModel { get; set; } // Modèle de pièce
        [field: SerializeField]
        public bool IsRoot { get; set; } // Root node 

        public Link()
        {
            LinkModel = new LinkModel();
        }

        


    }

     


    }
