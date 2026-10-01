using DT.Tools;
using UnityEngine;

namespace DT.Model
{
    [System.Serializable]
    public class Inertial
    {
        [field: SerializeField]
        public Origin Origin { get; set; } // Origine p/r à la piece
        [field: SerializeField]
        public Parameter  Mass   { get; set; } // Masse de la pièce
        [field: SerializeField]
        public Parameter Ixx    { get; set; } // Matrice d'inertie de la piece
        [field: SerializeField]
        public Parameter Iyy    { get; set; } // Matrice d'inertie de la piece
        [field: SerializeField]
        public Parameter Izz    { get; set; } // Matrice d'inertie de la piece
        [field: SerializeField]
        public Parameter Ixy    { get; set; } // Matrice d'inertie de la piece
        [field: SerializeField]
        public Parameter Ixz    { get; set; } // Matrice d'inertie de la piece
        [field: SerializeField]
        public Parameter Iyz    { get; set; } // Matrice d'inertie de la piece


        public Inertial()
        {
            Mass = new Parameter("Mass");
            Mass.Description = "Masse de la pièce";
            Mass.Type = Type_enum.Float;
            Mass.Unit = Units.GetUnitFromName("kg");

            Ixx = new Parameter("Ixx");
            Ixx.Description = "Matrice d'inertie de la piece";
            Ixx.Type = Type_enum.Float;
            Ixx.Unit = Units.GetUnitFromName("kg*m²");

            Iyy = new Parameter("Iyy");
            Iyy.Description = "Matrice d'inertie de la piece";
            Iyy.Type = Type_enum.Float;
            Iyy.Unit = Units.GetUnitFromName("kg*m²");

            Izz = new Parameter("Izz");
            Izz.Description = "Matrice d'inertie de la piece";
            Izz.Type = Type_enum.Float;
            Izz.Unit = Units.GetUnitFromName("kg*m²");

            Ixy = new Parameter("Ixy");
            Ixy.Description = "Matrice d'inertie de la piece";
            Ixy.Type = Type_enum.Float;
            Ixy.Unit = Units.GetUnitFromName("kg*m²");

            Ixz = new Parameter("Ixz");
            Ixz.Description = "Matrice d'inertie de la piece";
            Ixz.Type = Type_enum.Float;
            Ixz.Unit = Units.GetUnitFromName("kg*m²");

            Iyz = new Parameter("Iyz");
            Iyz.Description = "Matrice d'inertie de la piece";
            Iyz.Type = Type_enum.Float;
            Iyz.Unit = Units.GetUnitFromName("kg*m²");

            Origin = new Origin();


        }
    }
}
