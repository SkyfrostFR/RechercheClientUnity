using System;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace DT.Model
{
    /// <summary>
    /// Strongly-typed, value-type replacement for the old <c>dynamic</c> value of
    /// <see cref="Parameter"/> / <see cref="Variable"/>.
    ///
    /// It is a tiny tagged union (a <see cref="Type_enum"/> plus the matching storage) that
    /// implicitly converts <b>to and from</b> the primitive CLR types, so every call site keeps
    /// the exact same ergonomics as before but is now fully type-checked at compile time:
    /// <code>
    ///     float f   = var.Value;          // Val -> float
    ///     var.Value = 123.4f;             // float -> Val
    ///     if (a.Value &lt; b.Value) { … }    // Val -> float, then float &lt; float
    ///     bool eq   = a.Value == b.Value;  // operator ==(Val, Val)
    /// </code>
    ///
    /// Because the type tag lives in the value (and in the owning Parameter/Variable) instead of
    /// in a generic type argument, a <c>List&lt;Variable&gt;</c> can still hold variables of
    /// different types.
    ///
    /// The equality operators are declared explicitly so the implicit conversions never make a
    /// comparison ambiguous (an exact-match operator always beats a conversion).
    /// </summary>
    [JsonConverter(typeof(ValJsonConverter))]
    public readonly struct Val
    {
        public Type_enum Type { get; }

        private readonly double _num;   // Float / Double / Int32
        private readonly bool _bool;    // Bool
        private readonly object _obj;   // String, Vector3 (boxed), arrays

        public bool IsNull => Type == Type_enum.Null;

        private Val(Type_enum type, double num = 0.0, bool b = false, object obj = null)
        {
            Type = type;
            _num = num;
            _bool = b;
            _obj = obj;
        }

        // --------------------------------------------------------------------- factories

        /// <summary>Default (empty) value for a given type — mirrors Types.DefaultValue.</summary>
        public static Val Default(Type_enum type)
        {
            switch (type)
            {
                case Type_enum.String: return new Val(type, obj: string.Empty);
                case Type_enum.Vector3f: return new Val(type, obj: Vector3.zero);
                case Type_enum.ByteArray: return new Val(type, obj: Array.Empty<byte>());
                case Type_enum.BoolArray: return new Val(type, obj: Array.Empty<bool>());
                case Type_enum.Int32Array: return new Val(type, obj: Array.Empty<int>());
                case Type_enum.FloatArray: return new Val(type, obj: Array.Empty<float>());
                default: return new Val(type);
            }
        }

        /// <summary>Coerce an existing Val to the declared <paramref name="target"/> type.</summary>
        public static Val Coerce(Val v, Type_enum target)
        {
            return v.Type == target ? v : FromObject(v.Boxed, target);
        }

        /// <summary>Build a Val of <paramref name="target"/> type from any boxed value.</summary>
        public static Val FromObject(object value, Type_enum target)
        {
            if (value == null) return Default(target);
            switch (target)
            {
                case Type_enum.Float:
                case Type_enum.Double:
                    return new Val(target, num: ToDouble(value));
                case Type_enum.Int32:
                    return new Val(target, num: Math.Round(ToDouble(value)));
                case Type_enum.Bool:
                    return new Val(target, b: ToBool(value));
                case Type_enum.String:
                    return new Val(target, obj: value as string ?? Convert.ToString(value, CultureInfo.InvariantCulture));
                case Type_enum.Vector3f:
                    return new Val(target, obj: value is Vector3 vec ? vec : Vector3.zero);
                case Type_enum.FloatArray:
                case Type_enum.Int32Array:
                case Type_enum.BoolArray:
                case Type_enum.ByteArray:
                    return new Val(target, obj: value);
                default:
                    return Default(target);
            }
        }

        // --------------------------------------------------------------------- accessors

        /// <summary>The concrete, boxed CLR value (for JSON, ToString, generic interop).</summary>
        public object Boxed
        {
            get
            {
                switch (Type)
                {
                    case Type_enum.Float: return (float)_num;
                    case Type_enum.Double: return _num;
                    case Type_enum.Int32: return (int)_num;
                    case Type_enum.Bool: return _bool;
                    case Type_enum.String: return (string)(_obj ?? string.Empty);
                    case Type_enum.Vector3f: return _obj is Vector3 v ? v : Vector3.zero;
                    case Type_enum.FloatArray: return _obj ?? Array.Empty<float>();
                    case Type_enum.Int32Array: return _obj ?? Array.Empty<int>();
                    case Type_enum.BoolArray: return _obj ?? Array.Empty<bool>();
                    case Type_enum.ByteArray: return _obj ?? Array.Empty<byte>();
                    default: return null;
                }
            }
        }

        public double AsDouble()
        {
            switch (Type)
            {
                case Type_enum.Bool: return _bool ? 1.0 : 0.0;
                case Type_enum.String:
                    return double.TryParse(_obj as string, NumberStyles.Any, CultureInfo.InvariantCulture, out double d) ? d : 0.0;
                default: return _num;
            }
        }

        public float AsFloat() => (float)AsDouble();
        public int AsInt() => (int)Math.Round(AsDouble());

        public bool AsBool()
        {
            switch (Type)
            {
                case Type_enum.Bool: return _bool;
                case Type_enum.Float:
                case Type_enum.Double:
                case Type_enum.Int32: return _num != 0.0;
                case Type_enum.String: return bool.TryParse(_obj as string, out bool b) && b;
                default: return false;
            }
        }

        public string AsString()
            => Type == Type_enum.String ? (string)(_obj ?? string.Empty)
                                        : Convert.ToString(Boxed, CultureInfo.InvariantCulture);

        public Vector3 AsVector3() => _obj is Vector3 v ? v : Vector3.zero;

        // ------------------------------------------------------- implicit conversions (IN)
        // primitive -> Val : enables `var.Value = 123.4f`, `= true`, `= "txt"`, …

        public static implicit operator Val(float v) => new Val(Type_enum.Float, num: v);
        public static implicit operator Val(double v) => new Val(Type_enum.Double, num: v);
        public static implicit operator Val(int v) => new Val(Type_enum.Int32, num: v);
        public static implicit operator Val(bool v) => new Val(Type_enum.Bool, b: v);
        public static implicit operator Val(string v) => new Val(Type_enum.String, obj: v ?? string.Empty);
        public static implicit operator Val(Vector3 v) => new Val(Type_enum.Vector3f, obj: v);

        // ------------------------------------------------------ implicit conversions (OUT)
        // Val -> every CLR type that backs a Type_enum, so all of these compile with no cast:
        //     string s  = var.Value;   float f   = var.Value;   double d = var.Value;
        //     bool   b  = var.Value;   int   i   = var.Value;   Vector3 p = var.Value;
        //     float[] a = var.Value;   int[] ia  = var.Value;   bool[] ba = var.Value;   byte[] by = var.Value;
        //
        // Having several numeric out-conversions would, on its own, make `a.Value < b.Value`
        // resolve to the "most specific" target (int) and silently truncate. The explicit
        // relational / equality operators below prevent that: a user-defined operator shadows the
        // predefined ones whenever a Val is an operand, so every Val comparison stays numeric.
        // Method overloads such as ConvertToSI(float)/(double) remain unambiguous because float is
        // a better conversion target than double.

        public static implicit operator string(Val v) => v.AsString();
        public static implicit operator float(Val v) => v.AsFloat();
        public static implicit operator double(Val v) => v.AsDouble();
        public static implicit operator bool(Val v) => v.AsBool();
        public static implicit operator int(Val v) => v.AsInt();
        public static implicit operator Vector3(Val v) => v.AsVector3();
        public static implicit operator byte[](Val v) => v.Boxed as byte[];
        public static implicit operator bool[](Val v) => v.Boxed as bool[];
        public static implicit operator int[](Val v) => v.Boxed as int[];
        public static implicit operator float[](Val v) => v.Boxed as float[];

        // ----------------------------------------------------------------------- equality
        // Declared explicitly so the implicit conversions above never create an ambiguous
        // comparison (an exact-match operator wins over any user-defined conversion).

        public static bool operator ==(Val a, Val b) => a.ValueEquals(b);
        public static bool operator !=(Val a, Val b) => !a.ValueEquals(b);
        public static bool operator ==(Val a, float b) => a.AsFloat() == b;
        public static bool operator !=(Val a, float b) => a.AsFloat() != b;
        public static bool operator ==(float a, Val b) => a == b.AsFloat();
        public static bool operator !=(float a, Val b) => a != b.AsFloat();
        public static bool operator ==(Val a, bool b) => a.AsBool() == b;
        public static bool operator !=(Val a, bool b) => a.AsBool() != b;
        public static bool operator ==(bool a, Val b) => a == b.AsBool();
        public static bool operator !=(bool a, Val b) => a != b.AsBool();

        // --------------------------------------------------------------------- relational
        // Defined explicitly so `a.Value < b.Value` is always a numeric (double) comparison
        // instead of being resolved to int truncation through the implicit out-conversions.
        public static bool operator <(Val a, Val b) => a.AsDouble() < b.AsDouble();
        public static bool operator >(Val a, Val b) => a.AsDouble() > b.AsDouble();
        public static bool operator <=(Val a, Val b) => a.AsDouble() <= b.AsDouble();
        public static bool operator >=(Val a, Val b) => a.AsDouble() >= b.AsDouble();

        private bool ValueEquals(Val o)
        {
            switch (Type)
            {
                case Type_enum.Float:
                case Type_enum.Double:
                case Type_enum.Int32: return _num == o.AsDouble();
                case Type_enum.Bool: return _bool == o.AsBool();
                default: return Equals(Boxed, o.Boxed);
            }
        }

        public override bool Equals(object obj) => obj is Val v && ValueEquals(v);
        public override int GetHashCode() => Boxed?.GetHashCode() ?? 0;

        public override string ToString()
        {
            object b = Boxed;
            return b is IFormattable f ? f.ToString(null, CultureInfo.InvariantCulture) : (b?.ToString() ?? "null");
        }

        // ----------------------------------------------------------------------- helpers

        private static double ToDouble(object o)
            => o is bool b ? (b ? 1.0 : 0.0) : Convert.ToDouble(o, CultureInfo.InvariantCulture);

        private static bool ToBool(object o)
        {
            if (o is bool b) return b;
            if (o is string s) return bool.TryParse(s, out bool r) && r;
            try { return Convert.ToDouble(o, CultureInfo.InvariantCulture) != 0.0; }
            catch { return false; }
        }
    }

    /// <summary>
    /// Serializes a <see cref="Val"/> to JSON as its plain scalar value (so the Newtonsoft
    /// import/export format stays identical to the old <c>dynamic</c> one). On read the scalar is
    /// wrapped in a Val whose tag is inferred from the JSON token; the owning Parameter/Variable
    /// setter then coerces it to the declared <see cref="Type_enum"/>.
    /// </summary>
    public class ValJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType) => objectType == typeof(Val);

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, ((Val)value).Boxed);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            switch (reader.TokenType)
            {
                case JsonToken.Null:
                    return Val.Default(Type_enum.Null);
                case JsonToken.Integer:
                    return Val.FromObject(reader.Value, Type_enum.Int32);
                case JsonToken.Float:
                    return Val.FromObject(reader.Value, Type_enum.Double);
                case JsonToken.Boolean:
                    return Val.FromObject(reader.Value, Type_enum.Bool);
                case JsonToken.String:
                    return Val.FromObject(reader.Value, Type_enum.String);
                default:
                    // Arrays / objects (Vector3, *Array …): keep the raw token boxed; the
                    // Parameter/Variable setter coerces it by the declared type.
                    JToken token = JToken.ReadFrom(reader);
                    return Val.FromObject(token, Type_enum.Null);
            }
        }
    }
}
