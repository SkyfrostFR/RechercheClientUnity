using EigenCore.Core.Dense;
using System;
using System.Linq;
using UnityEngine;

public static class MathsExtensions
{

    // Epsilon : used for numerical comparisson = contact threshold 
    public static float epsilon = 0.0005f;

    /// <summary>
    /// Concat array helper
    ///
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    public static T[] Concat<T>(this T[] x, T[] y)
    {
        int oldLen = x.Length;
        Array.Resize<T>(ref x, x.Length + y.Length);
        Array.Copy(y, 0, x, oldLen, y.Length);
        return x;
    }

    public static float[] GetColumn(this float[,] matrix, int columnNumber)
    {
        return Enumerable.Range(0, matrix.GetLength(0))
                .Select(x => matrix[x, columnNumber])
                .ToArray();
    }

    public static float[] GetRow(this float[,] matrix, int rowNumber)
    {
        return Enumerable.Range(0, matrix.GetLength(1))
                .Select(x => matrix[rowNumber, x])
                .ToArray();
    }
    public static void SetRow(this float[,] matrix, int rowNumber, float[] row)
    {
        for (int i = 0; i < row.Length; i++)
        {
            matrix[rowNumber, i] = row[i];

        }
    }



    public static float Mod(this float x, float y)
    {

        if (y == 0.0f)
            return x;

        float m = x - y * Mathf.Floor(x / y);

        if (y > 0)
        {
            if (m >= y)
                return 0;
            if (m < 0)
            {
                if (y + m == y)
                    return 0;
                else
                    return y + m;
            }
        }
        else
        {
            if (m <= y)
                return 0;
            if (m > 0)
            {
                if (y + m == y)
                    return 0;
                else
                    return y + m;
            }
        }
        return m;
    }


    public static float[] toFloat(this double[] values)
    {
        float[] result = new float[values.Length];
        for (int i = 0; i < values.Length; i++)
        {
            result[i] = (float)values[i];
        }
        return result;
    }

    public static MatrixXD PseudoDampInv(this MatrixXD A, float damping)
    {
        MatrixXD damp = MatrixXD.Identity(A.Rows) * (damping * damping);
        MatrixXD res = A.Transpose() * ((A * A.Transpose()) + damp).Inverse();
        return res;
    }

    public static double[] toDouble(this float[] values)
    {
        double[] result = new double[values.Length];
        for (int i = 0; i < values.Length; i++)
        {
            result[i] = (double)values[i];
        }
        return result;
    }
    public static double[,] toDouble(this float[,] values)
    {
        double[,] result = new double[values.GetLength(0), values.GetLength(1)];
        for (int i = 0; i < values.GetLength(0); i++)
        {
            for (int j = 0; j < values.GetLength(1); j++)
            {
                result[i, j] = (double)values[i, j];

            }
        }
        return result;
    }

    public static T[] Add<T>(this T[] array, T value)
    {
        Array.Resize(ref array, array.Length + 1);
        array[array.Length - 1] = value;
        return array;
    }

    public static T[] Flatten<T>(this T[,] matrix)
    {
        int rows = matrix.GetLength(0);
        int cols = matrix.GetLength(1);

        // Initialize the flattened array
        T[] flattenedArray = new T[rows * cols];

        // Flatten the matrix to column-major order
        int index = 0;
        for (int j = 0; j < cols; j++)
        {
            for (int i = 0; i < rows; i++)
            {
                flattenedArray[index++] = matrix[i, j];
            }
        }
        return flattenedArray;
    }
}

