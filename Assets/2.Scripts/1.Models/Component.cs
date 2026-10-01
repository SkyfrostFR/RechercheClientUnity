using System;
using System.Collections.Generic;
using UnityEngine;

namespace DT.Model
{
    [Serializable]
    public class Component : BaseModel
    {
        [field: SerializeField]
        public string Name { get; set; }  // Nom du composant
        [field: SerializeField]
        public string Img { get; set; } // Screenshot du composant (Notamment pour la bibliothéque ...)

        [field: SerializeField]
        public List<Link> Links { get; set; } // Liste des pièces du composant
        [field: SerializeReference]
        public List<Component> Components { get; set; }  // Liste des sous composants
        [field: SerializeField]
        public List<Joint> Joints { get; set; } // Liste des liaisons
        [field: SerializeField]
        public List<Parameter> Parameters { get; set; }// Liste des paramètres
        [field: SerializeField]
        public List<Variable> Variables { get; set; } // Liste des Variables
        [field: SerializeField]
        public float CyclicTime { get; set; }  // Cyclic time in s to compute component model



    }
}
