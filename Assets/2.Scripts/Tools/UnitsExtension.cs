using DT.Model;


namespace DT.Tools
{
    public static class UnitsExtension
    {

        // Implement a more generic way 
        public static float ConvertToSI(this float value, Unit unit)
        {
            return Units.ConvertToSI(value, unit);
        }
        public static float ConvertFromSI(this float value, Unit unit)
        {
            return Units.ConvertFromSI(value, unit);
        }

        public static float ConvertFromTo(this float value, Unit fromUnit, Unit toUnit)
        {
            return Units.ConvertFromSI(ConvertToSI(value, fromUnit), toUnit);
        }

        public static double ConvertToSI(this double value, Unit unit)
        {
            return Units.ConvertToSI(value, unit);
        }

        public static double ConvertFromTo(this double value, Unit fromUnit, Unit toUnit)
        {
            return Units.ConvertFromSI(Units.ConvertToSI(value, fromUnit), toUnit);
        }



        public static float ConvertToSI(this Parameter param)
        {
            if (param.Unit != null)
            {
                return ConvertToSI((float)param.Value, param.Unit);
            }
            else { return (float)param.Value; }
        }
        public static float ConvertToSI(this Variable var)
        {
            if (var.Unit != null)
            {
                return ConvertToSI((float)var.Value, var.Unit);
            }
            else { return (float)var.Value; }
        }

        public static float ConvertTo(this Variable var, Unit unit)
        {
            if (var.Unit != null)
            {
                return ConvertFromTo(var.Value, var.Unit, unit);

            }
            else { return var.Value; }
        }
        public static float ConvertTo(this Parameter var, Unit unit)
        {
            if (var.Unit != null)
            {
                return ConvertFromTo(var.Value, var.Unit, unit);

            }
            else { return var.Value; }
        }

    }
}

