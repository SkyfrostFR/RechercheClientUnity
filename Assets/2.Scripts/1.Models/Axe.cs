using System;
using UnityEngine;
using DT.Tools;

namespace DT.Model
{
    [Serializable]
    public class Axe
    {
        [field: SerializeField]
        public Axe_enum Type { get; set; } // Degré de liberté de l'axe
        // Full signed axis direction from URDF <axis xyz>, in ROS/URDF convention.
        // Type (Axe_enum) only encodes the dominant axis WITHOUT its sign, so a URDF
        // axis="0 0 -1" would otherwise be indistinguishable from "0 0 1" and the joint
        // would rotate/translate the wrong way (mirroring its limits). Vector3.zero = unset
        // (legacy data) → controllers fall back to the unsigned enum.
        [field: SerializeField]
        public Vector3 AxisVector { get; set; } = Vector3.zero;
        [field: SerializeField]
        public bool Passive { get; set; } // Passive joint
        [field: SerializeField]
        public AxeParam Param { get; set; } // Paramètre de l'axe
        [field: SerializeField]
        public McInfo Info { get; set; } // Etat de l'axe à l'instant t

        [field: SerializeField]
        public McCmd Command { get; set; } // Commande de l'axe à l'instant t


        

        public Axe()
        {
            Param = new AxeParam();
            Info = new McInfo();
            Info.param = Param;
            Command = new McCmd();
        }

        /// <summary>
        /// Degré de liberté de l'axe
        /// </summary>
        [Serializable]
        public enum Axe_enum
        {
            LinX = 0,   // Translation axe X
            LinY = 1,
            LinZ = 2,
            RotX = 3,   // Rotation autour de X
            RotY = 4,  // Rotation autour de Y
            RotZ = 5     // Rotation autour de Z
        }

        /// <summary>
        /// Paramètres de l'axe
        /// </summary>
        [Serializable]
        public class AxeParam
        {
            [field: SerializeField]
            public Parameter PosHome { get; set; }  // Position d’origine, de référence de l’axe.
            [field: SerializeField]
            public Parameter Pos_sw_end { get; set; }  // Fin de course logiciel dans le sens positif.
            [field: SerializeField]
            public Parameter Neg_sw_end { get; set; }  // Fin de course logiciel dans le sens négatif.
            [field: SerializeField]
            public Parameter dS_max { get; set; }  // Vitesse maximale en valeur absolue.
            [field: SerializeField]
            public Parameter d2S_max { get; set; }  // Accélération maximale (valeur absolue).
            [field: SerializeField]
            public Parameter d2S_stop { get; set; }  // Décélération maximale (valeur absolue) lors d’un arrêt d’urgence logiciel.
            [field: SerializeField]
            public Parameter d3S_max { get; set; }  // Jerk maximal (valeur absolue).

            [field: SerializeField]
            public Parameter Ec_Warning { get; set; }  // Erreur de poursuite provoquant un warning (valeur absolue).
            [field: SerializeField]
            public Parameter Ec_Stop { get; set; }  // Erreur de poursuite provoquant un arrêt de l’axe (valeur absolue).
            [field: SerializeField]
            public Parameter C_per { get; set; }  // Couple/effort permanent (valeur absolue).
            [field: SerializeField]
            public Parameter C_max { get; set; }  // Couple/effort maximal (valeur absolue).
            [field: SerializeField]
            public Parameter dC_max { get; set; }  // Vitesse maximale d’évolution du Couple/effort (valeur absolue).
            [field: SerializeField]
            public Parameter d2C_max { get; set; }  // Accélération maximale d’évolution du Couple/effort (valeur absolue  .


            /// <summary>
            /// Extra param ...
            /// TODO : better Implementation
            /// </summary>
            public float stiffness = 10000000000.0f;
            public float damping = 0.01f;
            public float defaultForceLimit = 10000000000.0f;

            /// <summary>
            /// Init param
            /// </summary>
            public AxeParam()
            {
                PosHome = new Parameter("PosHome");
                PosHome.Type = Type_enum.Float;
                PosHome.Description = "Position d’origine, de référence de l’axe";


                Pos_sw_end = new Parameter("Pos_sw_end");
                Pos_sw_end.Type = Type_enum.Float;
                Pos_sw_end.Description = "Fin de course logiciel dans le sens positif";

                Neg_sw_end = new Parameter("Neg_sw_end");
                Neg_sw_end.Type = Type_enum.Float;
                Neg_sw_end.Description = "Fin de course logiciel dans le sens negatif";

                dS_max = new Parameter("dS_max");
                dS_max.Type = Type_enum.Float;
                dS_max.Description = "Vitesse maximale en valeur absolue";
                dS_max.Value = 2500.0f;
                dS_max.Unit = Units.GetUnitFromName("r/min");

                d2S_max = new Parameter("d2S_max");
                d2S_max.Type = Type_enum.Float;
                d2S_max.Description = "Accélération maximale (valeur absolue)";
                d2S_max.Value = 100.0f;
                dS_max.Unit = Units.GetUnitFromName("°/s²");

                d2S_stop = new Parameter("d2S_stop");
                d2S_stop.Type = Type_enum.Float;
                d2S_stop.Description = "Décélération maximale (valeur absolue) lors d’un arrêt d’urgence logiciel";

                d3S_max = new Parameter("d3S_max");
                d3S_max.Type = Type_enum.Float;
                d3S_max.Description = "Jerk Maximal (valeur absolue)";
                d3S_max.Value = 1.0f;


                Ec_Warning = new Parameter("Ec_Warning");
                Ec_Warning.Type = Type_enum.Float;
                Ec_Warning.Description = "Erreur de poursuite provoquant un warning (valeur absolue)";

                Ec_Stop = new Parameter("Ec_Stop");
                Ec_Stop.Type = Type_enum.Float;
                Ec_Stop.Description = "Erreur de poursuite provoquant un arrêt de l’axe (valeur absolue)";

                C_per = new Parameter("C_per");
                C_per.Type = Type_enum.Float;
                C_per.Description = "Couple/effort permanent (valeur absolue)";

                C_max = new Parameter("C_max");
                C_max.Type = Type_enum.Float;
                C_max.Description = "Couple/effort maximal (valeur absolue)";

                dC_max = new Parameter("dC_max");
                dC_max.Type = Type_enum.Float;
                dC_max.Description = "Vitesse maximale d’évolution du Couple/effort (valeur absolue)";

                d2C_max = new Parameter("d2C_max");
                d2C_max.Type = Type_enum.Float;
                d2C_max.Description = "Accélération maximale d’évolution du Couple/effort (valeur absolue)";

            }

        }

        /// <summary>
        /// Info de l'axe à l'instant t
        /// </summary>
        [Serializable]
        public partial class McInfo
        {
            [field: SerializeField]
            public Variable S_act { get; private set; } // Position à l’instant t. Valeur initiale : 0
            [field: SerializeField]
            public Variable dS_act { get; private set; } // Vitesse à l’instant t. Valeur initiale : 0
            [field: SerializeField]
            public Variable d2S_act { get; private set; } // Accélération à l’instant t. Valeur initiale : 0
            [field: SerializeField]
            public Variable C_act { get; private set; } // Couple/effort à l’instant t. Valeur initiale : 0
            [field: SerializeField]
            public Variable dC_act { get; private set; } // Vitesse d’évolution du couple/effort à l’instant t.



            [field: SerializeField]
            public Variable IsHomed { get; private set; } //Capteur home actif.
            [field: SerializeField]
            public Variable LimitSwitchPos { get; private set; } //Capteur de fin de course dans le sens positif actif.
            [field: SerializeField]
            public Variable LimitSwitchNeg { get; private set; } //Capteur de fin de course dans le sens negatif actif.





            public AxeParam param { get; set; } // Param d'axe


            /// <summary>
            /// Init Vars
            /// </summary>
            public McInfo()
            {

                S_act = new Variable("S_act");
                S_act.Type = Type_enum.Float;
                S_act.Description = "Position à l’instant t";
                S_act.IO = Variable.IO_enum.Output;
                S_act.Unit = Units.GetUnitFromName("°");

                dS_act = new Variable("dS_act");
                dS_act.Type = Type_enum.Float;
                dS_act.Description = "Vitesse à l’instant t";
                dS_act.IO = Variable.IO_enum.Output;
                dS_act.Unit = Units.GetUnitFromName("°/s");

                d2S_act = new Variable("d2S_act");
                d2S_act.Type = Type_enum.Float;
                d2S_act.Description = "Accélération à l’instant t";
                d2S_act.IO = Variable.IO_enum.Output;
                d2S_act.Unit = Units.GetUnitFromName("°/s²");


                C_act = new Variable("C_act");
                C_act.Type = Type_enum.Float;
                C_act.Description = "Couple/effort à l’instant t";
                C_act.IO = Variable.IO_enum.Output;
                C_act.Unit = Units.GetUnitFromName("N*m");


                dC_act = new Variable("dC_act");
                dC_act.Type = Type_enum.Float;
                dC_act.Description = "Vitesse d’évolution du couple/effort à l’instant t";
                dC_act.IO = Variable.IO_enum.Output;


                IsHomed = new Variable("IsHomed");
                IsHomed.Type = Type_enum.Bool;
                IsHomed.Description = "Capteur home actif";
                IsHomed.IO = Variable.IO_enum.Output;

                LimitSwitchPos = new Variable("LimitSwitchPos");
                LimitSwitchPos.Type = Type_enum.Bool;
                LimitSwitchPos.Description = "Capteur de fin de course dans le sens positif actif";
                LimitSwitchPos.IO = Variable.IO_enum.Output;

                LimitSwitchNeg = new Variable("LimitSwitchNeg");
                LimitSwitchNeg.Type = Type_enum.Bool;
                LimitSwitchNeg.Description = "Capteur de fin de course dans le sens negatif actif";
                LimitSwitchNeg.IO = Variable.IO_enum.Output;

            }
        }

        [Serializable]
        public enum AxeControl_enum
        {
            Position,
            Velocity,
            Manual
        }

        /// <summary>
        /// Info de l'axe à l'instant t
        /// </summary>
        [Serializable]
        public partial class McCmd
        {
            [field: SerializeField]
            public AxeControl_enum ControlType { get; set; }
            [field: SerializeField]
            public Variable S_Cmd { get; set; }
            [field: SerializeField]
            public Variable dS_Cmd { get; set; }
            [field: SerializeField]
            public Variable BpHome { get; set; }
            [field: SerializeField]
            public Variable BpPos { get; set; }
            [field: SerializeField]
            public Variable BpNeg { get; set; }

            public McCmd()
            {
                S_Cmd = new Variable("Position Command");
                S_Cmd.Type = Type_enum.Float;
                S_Cmd.Description = "Commande Position";
                S_Cmd.IO = Variable.IO_enum.Input;
                S_Cmd.Unit = Units.GetUnitFromName("°");


                dS_Cmd = new Variable("Velocity Command");
                dS_Cmd.Type = Type_enum.Float;
                dS_Cmd.Description = "Commande Vitesse";
                dS_Cmd.IO = Variable.IO_enum.Input;
                dS_Cmd.Unit = Units.GetUnitFromName("°/s");

                BpHome = new Variable("BpHome");
                BpHome.Type = Type_enum.Bool;
                BpHome.Description = "Commande vers position Home";
                BpHome.IO = Variable.IO_enum.Input;

                BpPos = new Variable("BpPos");
                BpPos.Type = Type_enum.Bool;
                BpPos.Description = "Commande manuelle positive";
                BpPos.IO = Variable.IO_enum.Input;

                BpNeg = new Variable("BpNeg");
                BpNeg.Type = Type_enum.Bool;
                BpNeg.Description = "Commande manuelle négative";
                BpNeg.IO = Variable.IO_enum.Input;


            }


        }
    }
}
