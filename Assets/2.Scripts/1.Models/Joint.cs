using System.Collections.Generic;
using System;
using UnityEngine;


namespace DT.Model
{
    [Serializable]
    public class Joint : BaseModel
    {
        public enum JointTypes
        {
            Fixed,
            Revolute,
            Prismatic,
            Planar
        }


        [field: SerializeField]
        public string Name         { get; set; } // Nom de la liaison
        [field: SerializeField]
        public string Description  { get; set; } // Description de la liaison
        [field: SerializeField]
        public string ParentId     { get; set; } // Parent link ID
        [field: SerializeField]
        public Origin ParentOrigin { get; set; } // Position de l'origine de la liaison p/r piece parent
        [field: SerializeField]
        public string ChildId      { get; set; } // Child link ID
        [field: SerializeField]
        public Origin ChildOrigin  { get; set; } // Position de l'origine de la liaison p/r piece ENFANT
        [field: SerializeField]
        public bool Constraint     { get; set; } // Liaison contrainte/couplée
        [field: SerializeField]
        public Vector2 factorAndOffset { get; set; } // Liaison contrainte/couplée
        [field: SerializeField]
        public Variable couplingVar { get; set; } // C

        [field: SerializeField]
        public Visual Visual       { get; set; } // Paramètres visuel de la liaision : Utilisation uniquement de SHOW >> TODO : Créer des visuels des liaisons
        [field: SerializeField]
        public JointTypes Type     { get; set; } // Type de liaison
        [field: SerializeField]
        public List<Axe> Axes      { get; set; } // Axes de la liaison


        


    }
}
