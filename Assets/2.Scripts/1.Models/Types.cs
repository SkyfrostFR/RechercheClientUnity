using System.Globalization;
using UnityEngine;

namespace DT.Model
{
	public enum Type_enum
	{
		String,
		Float,
		Double,
		Bool,
		Int32,
		Vector3f,
		ByteArray,
		BoolArray,
		Int32Array,
		FloatArray,
		//Struct = 9,
		//Title = 10
		Null = 255
	}
	[System.Serializable]

	static class Types
    {
		private static Type_enum GetTypeCode(System.Type objectType)
        {

            if (objectType == typeof(string))
            {
				return Type_enum.String;
            }
            if (objectType == typeof(float))
            {
				return Type_enum.Float;
            }
            if (objectType == typeof(double))
            {
                return Type_enum.Double;
            }
            if (objectType == typeof(bool))
            {
				return Type_enum.Bool;
            }
            if (objectType == typeof(int))
            {
				return Type_enum.Int32;
            }
            if (objectType == typeof(Vector3))
            {
				return Type_enum.Vector3f;
            }
			if (objectType == typeof(byte[]))
			{
				return Type_enum.ByteArray;
			}
			if (objectType == typeof(bool[]))
			{
				return Type_enum.BoolArray;
			}
			if (objectType == typeof(int[]))
			{
				return Type_enum.Int32Array;
			}
			if (objectType == typeof(float))
			{
				return Type_enum.Vector3f;
			}


			return Type_enum.Null;
        }


        public static dynamic DefaultValue(Type_enum type)
        {
            dynamic _Value;
            // Default value 
            switch (type)
            {
                case Type_enum.String:
                    _Value = string.Empty;
                    break;
                case Type_enum.Float:
                    _Value = 0.0f;
                    break;
                case Type_enum.Double:
                    _Value = 0.0;
                    break;
                case Type_enum.Bool:
                    _Value = false;
                    break;
                case Type_enum.Int32:
                    _Value = 0;
                    break;
                case Type_enum.Vector3f:
                    _Value = Vector3.zero;
                    break;
                case Type_enum.ByteArray:
                    _Value = new byte[0];
                    break;
                case Type_enum.BoolArray:
                    _Value = new bool[0];
                    break;
                case Type_enum.Int32Array:
                    _Value = new int[0];
                    break;
                case Type_enum.FloatArray:
                    _Value = new float[0];
                    break;
                case Type_enum.Null:
                    _Value = null;
                    break;
                default:
                    _Value = null;
                    break;
            }

            return _Value;
        }


        public static dynamic CheckType(object obj, Type_enum type)
		{
            if (obj == null)
                return null;
			
			Type_enum objTypeCode = GetTypeCode(obj.GetType());

            if (objTypeCode == type)
			{
				// Ok : Same Type
				// return value
                switch(type){
                    case Type_enum.String:
					    return (string)obj;
                    case Type_enum.Float:
                        return (float)obj;

                    case Type_enum.Double:
                        return (double)obj;

                    case Type_enum.Int32:
                        return (int)obj;

                    case Type_enum.Bool:
                        return (bool)obj;
                    default:
                        return obj;
                }
			}

			// If not the same type : try to cast to the right type !
			// Usefull for database/file read/write
            switch (objTypeCode)
            {
				case Type_enum.String:
					return "" + obj;

				case Type_enum.Float:
					float.TryParse(obj.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out float parsedFloat);
					return parsedFloat;

                case Type_enum.Double:
                    double.TryParse(obj.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out double parseddouble);
                    return parseddouble;

                case Type_enum.Int32:
					int.TryParse(obj.ToString(), out int parsedInt);
					return parsedInt;

				case Type_enum.Bool:
					bool.TryParse(obj.ToString(), out bool parsedBool);
					return parsedBool;


				// TODO : Manage Arrays

				default:
					return null;
            }

		}
	}





}
