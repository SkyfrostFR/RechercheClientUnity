using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System;

public class NormalDistribution
{
    private static readonly Random random = new Random();
    private static bool hasSpare = false;
    private static double spare;

    public static double DblNormal(double mean = 0, double sigma = 1)
    {
        if (hasSpare)
        {
            hasSpare = false;
            return spare * sigma + mean;
        }

        double u, v, s;
        do
        {
            u = random.NextDouble() * 2 - 1; // Uniformly distributed in [-1, 1)
            v = random.NextDouble() * 2 - 1; // Uniformly distributed in [-1, 1)
            s = u * u + v * v;
        } while (s >= 1 || s == 0);

        s = Math.Sqrt(-2.0 * Math.Log(s) / s);
        spare = v * s;
        hasSpare = true;
        return u * s * sigma + mean;
    }
}
