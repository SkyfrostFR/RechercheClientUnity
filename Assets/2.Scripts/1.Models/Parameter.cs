using DT.Tools;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace DT.Model
{

	/// <summary>
	/// Parameter Model
	/// </summary>
	[Serializable]
	public partial class Parameter
	{
        [field: SerializeField]
        public string Guid { get; private set; }
        [field: SerializeField]
        public string Name { get; set; } // Paramètre name
        [field: SerializeField]
        public string Description { get; set; } // Commentaire utilisateur sur l'usage de ce Paramètre
        [SerializeField]
        private Type_enum _Type;
        public Type_enum Type { get { return _Type; } set { _Type = SetType(value); } } // stockage du type dans la bdd

        [field: SerializeField]
        public Unit Unit { get; set; } // Unit

        // Strongly-typed value (see Val). No `dynamic` anywhere: `Val` implicitly converts to/from
        // the primitive types so call sites are unchanged, while the actual storage is the set of
        // concrete, Unity-serializable fields below — a plain `dynamic` field cannot be serialized
        // by Unity, which is why parameter values (joint limits, max speed, home position…) used to
        // be lost on the edit→Play round-trip.
        public Val Value { get { return ReadValue(); } set { WriteValue(Val.Coerce(value, Type)); } }



		/// <summary>
		/// Serializable backing storage for Value (selected by Type).
		/// </summary>
		[SerializeField] private double _vNum;       // Float / Double / Int32
		[SerializeField] private bool _vBool;        // Bool
		[SerializeField] private string _vString;    // String
		[SerializeField] private Vector3 _vVector;   // Vector3f
		[SerializeField] private float[] _vFloatArr; // FloatArray
		[SerializeField] private int[] _vIntArr;     // Int32Array
		[SerializeField] private bool[] _vBoolArr;   // BoolArray
		[SerializeField] private byte[] _vByteArr;   // ByteArray

		/// <summary>
		/// Instantiation
		/// </summary>

		public Parameter(string name = "")
		{
			Name = name;
            Guid = GuidGenerator.FetchID();
            Type = Type_enum.Null;
            Unit = new Unit();
        }


        Type_enum SetType(Type_enum value)
        {
            // Reset the storage to the default of the new type (mirrors the old behaviour where
            // changing Type reset _Value to DefaultValue(type)).
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

        // Reads the value back as a strongly-typed Val (tagged with this parameter's Type) so
        // callers keep behaving exactly like before (comparisons, arithmetic, implicit casts…).
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


        public static implicit operator float(Parameter param)
        {
            if (param.Type != Type_enum.Float)
            {
                throw new InvalidOperationException("Wrong type");
            }
            return param.Value.AsFloat();
        }
        public static implicit operator double(Parameter param)
        {
            if (param.Type != Type_enum.Double)
            {
                throw new InvalidOperationException("Wrong type");
            }
            return param.Value.AsDouble();
        }
        public static implicit operator int(Parameter param)
        {
            if (param.Type != Type_enum.Int32)
            {
                throw new InvalidOperationException("Wrong type");
            }
            return param.Value.AsInt();
        }
        public static implicit operator string(Parameter param)
        {
            if (param.Type != Type_enum.String)
            {
                throw new InvalidOperationException("Wrong type");
            }
            return param.Value.AsString();
        }
        public static implicit operator bool(Parameter param)
        {
            if (param.Type != Type_enum.Bool)
            {
                throw new InvalidOperationException("Wrong type");
            }
            return param.Value.AsBool();
        }


        // Tools
        public static Val GetValue(List<Parameter> Parameters, string name)
		{
			return Parameters.Find(x => x.Name == name).Value;
		}

		public static bool SetValue(List<Parameter> Parameters, string name, Val value)
		{
			return Parameters.Find(x => x.Name == name).Value = value;
		}




	}

}
