using DT.Simulation;
using DT.Tools;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace DT.Model
{
	[Serializable]
	public class Variable
	{
		public enum IO_enum
		{
			Input, Output, Internal, InOut
		}

        [Serializable]
		public struct VariableSettings
		{
			public bool Active;
			public bool Override;
		}

        [field: SerializeField]

        public string Guid { get; private set; }
        [field: SerializeField]
        public string Name        { get; set; } // Variable name
        [field: SerializeField]
        public string Description { get; set; } // Commentaire utilisateur sur l'usage de cette variable
        [SerializeField]
        private Type_enum _Type;
        public Type_enum Type { get { return _Type; } set { _Type = SetType(value); } } // stockage du type dans la bdd
        [field: SerializeField]
        public Unit Unit          { get; set; }
        [field: SerializeField]
        public IO_enum IO         { get; set; }  // Variable d'entrée ou sortie ? input = false; output = true;
        [field: SerializeField]
        public VariableSettings Settings { get; set; } // Extra settings
        [field: SerializeField]
        public bool Connected            { get; set; } // Variable connectée sur OPCUA/UDP ou autre ?

		// On change event
        public delegate void OnVariableChangeDelegate(Variable obj);
        public event OnVariableChangeDelegate VariableChanged;
		// Only on bool/float/integer signal
		public event OnVariableChangeDelegate VariableRisingEdge;
        public event OnVariableChangeDelegate VariableFallingEdge;


        /// <summary>
        /// Serializable backing storage for Value (selected by Type). A plain `dynamic` field
        /// cannot be serialized by Unity, so the value used to be lost on every serialization
        /// round-trip; these concrete fields fix that. The public Value is exposed as a strongly
        /// typed <see cref="Val"/> (no `dynamic`), which still implicitly converts to/from the
        /// primitive types so every call site is unchanged.
        /// </summary>
        [SerializeField] private double _vNum;       // Float / Double / Int32
        [SerializeField] private bool _vBool;        // Bool
        [SerializeField] private string _vString;    // String
        [SerializeField] private Vector3 _vVector;   // Vector3f
        [SerializeField] private float[] _vFloatArr; // FloatArray
        [SerializeField] private int[] _vIntArr;     // Int32Array
        [SerializeField] private bool[] _vBoolArr;   // BoolArray
        [SerializeField] private byte[] _vByteArr;   // ByteArray

        private Val _ValueOverride; // Valeur d'override (runtime, non sérialisée)
        private float oldTime = 0;
        public float DerivativeValue { get; private set; }

        Type_enum SetType(Type_enum value)
        {
            // Reset the storage to the default of the new type.
            _vNum = 0.0;
            _vBool = false;
            _vString = value == Type_enum.String ? string.Empty : null;
            _vVector = Vector3.zero;
            _vFloatArr = null;
            _vIntArr = null;
            _vBoolArr = null;
            _vByteArr = null;
            return value;
        }

        // Reads the value back as a strongly-typed Val (tagged with this variable's Type).
        private Val ReadValue()
        {
            switch (Type)
            {
                case Type_enum.Float: return (float)_vNum;
                case Type_enum.Double: return _vNum;
                case Type_enum.Int32: return (int)_vNum;
                case Type_enum.Bool: return _vBool;
                case Type_enum.String: return _vString ?? string.Empty;
                case Type_enum.Vector3f: return _vVector;
                case Type_enum.FloatArray: return Val.FromObject(_vFloatArr ?? new float[0], Type_enum.FloatArray);
                case Type_enum.Int32Array: return Val.FromObject(_vIntArr ?? new int[0], Type_enum.Int32Array);
                case Type_enum.BoolArray: return Val.FromObject(_vBoolArr ?? new bool[0], Type_enum.BoolArray);
                case Type_enum.ByteArray: return Val.FromObject(_vByteArr ?? new byte[0], Type_enum.ByteArray);
                default: return Val.Default(Type_enum.Null);
            }
        }

        // Stores an (already type-coerced to Type) Val into the matching serializable field.
        private void WriteValue(Val v)
        {
            switch (Type)
            {
                case Type_enum.Float:
                case Type_enum.Double:
                    _vNum = v.AsDouble();
                    break;
                case Type_enum.Int32:
                    _vNum = Math.Round(v.AsDouble());
                    break;
                case Type_enum.Bool:
                    _vBool = v.AsBool();
                    break;
                case Type_enum.String:
                    _vString = v.AsString();
                    break;
                case Type_enum.Vector3f:
                    _vVector = v.AsVector3();
                    break;
                case Type_enum.FloatArray:
                    _vFloatArr = v.Boxed as float[];
                    break;
                case Type_enum.Int32Array:
                    _vIntArr = v.Boxed as int[];
                    break;
                case Type_enum.BoolArray:
                    _vBoolArr = v.Boxed as bool[];
                    break;
                case Type_enum.ByteArray:
                    _vByteArr = v.Boxed as byte[];
                    break;
                default:
                    break;
            }
        }


        // Strongly-typed value (see Val). No `dynamic`: Val implicitly converts to/from the
        // primitive types so every call site keeps working unchanged; the storage is the concrete
        // serializable fields above.
        public Val Value {
			get
			{
                // If value is overrided : return override
				if (Settings.Override)
				{
					return _ValueOverride;
				}
				return ReadValue();
			}
			set
			{
                // Check old value
                Val old = ReadValue();
                WriteValue(Val.Coerce(value, Type));
                Val current = ReadValue();
                // Trigger events on value change
				if (!old.IsNull)
				{
                    if (old != current)
                    {
                        VarChangedEvent(this);
                    }
                    switch (Type)
                    {
                        case Type_enum.Float:
                        case Type_enum.Double:
                        case Type_enum.Int32:
                            double o = old.AsDouble();
                            double n = current.AsDouble();
                            if (o > n)
                            {
                                VarFallingEvent(this);
                            }
                            if (o < n)
                            {
                                VarRisingEvent(this);
                            }
                            DerivativeValue = (float)((n - o) / (Scheduler.Instance.GetTime() - oldTime));
                            break;

                        case Type_enum.Bool:
                            bool ob = old.AsBool();
                            bool nb = current.AsBool();
                            if (ob != nb && ob == true )
                            {
                                VarFallingEvent(this);
                            }
                            if (ob != nb && ob == false)
                            {
                                VarRisingEvent(this);
                            }
                            break;
                        default:
                            break;
                    }
                    oldTime = Scheduler.Instance.GetTime();
                }

			}
		}

        /// <summary>
        /// ValueOverride setter/getter
        /// </summary>
        public Val ValueOverride { get { return _ValueOverride; } set { _ValueOverride = Val.Coerce(value, Type); } }


        protected void VarChangedEvent(Variable variable)
        {
            if (this.VariableChanged != null)
            {
                this.VariableChanged(variable);
            }
        }
        protected void VarRisingEvent(Variable variable)
        {
            if (this.VariableRisingEdge != null)
            {
                this.VariableRisingEdge(variable);
            }
        }
        protected void VarFallingEvent(Variable variable)
        {
            if (this.VariableFallingEdge != null)
            {
                this.VariableFallingEdge(variable);
            }
        }


        /// <summary>
        /// Instantiation
        /// </summary>
        public Variable(string name = null)
		{
			Name = name;
            Guid = GuidGenerator.FetchID();
            // Default values
            Type = Type_enum.Null;
            IO = IO_enum.Internal;
            DerivativeValue = 0;
            Unit = new Unit();


        }

        public static implicit operator float(Variable param)
        {
            if (param.Type != Type_enum.Float)
            {
                throw new InvalidOperationException("Wrong type");
            }
            return param.Value.AsFloat();
        }
        public static implicit operator double(Variable param)
        {
            if (param.Type != Type_enum.Double)
            {
                throw new InvalidOperationException("Wrong type");
            }
            return param.Value.AsDouble();
        }
        public static implicit operator int(Variable param)
        {
            if (param.Type != Type_enum.Int32)
            {
                throw new InvalidOperationException("Wrong type");
            }
            return param.Value.AsInt();
        }
        public static implicit operator string(Variable param)
        {
            if (param.Type != Type_enum.String)
            {
                throw new InvalidOperationException("Wrong type");
            }
            return param.Value.AsString();
        }
        public static implicit operator bool(Variable param)
        {
            if (param.Type != Type_enum.Bool)
            {
                throw new InvalidOperationException("Wrong type");
            }
            return param.Value.AsBool();
        }


        //Get one value in list
        public static Val GetValue(List<Variable> Variables, string name)
		{
			return Variables.Find(x => x.Name == name).Value;
		}
		// Set one value in list
		public static bool SetValue(List<Variable> Variables, string name, Val value)
		{
			return Variables.Find(x => x.Name == name).Value = value;
		}

	}
}
