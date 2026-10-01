using System;

namespace DT
{
    public class Matrix
    {

        public static float[,] MatrixIdentity(int n)
        {
            // return an n x n Identity matrix
            float[,] result = new float[n,n];
            for (int i = 0; i < n; ++i)
                result[i,i] = 1.0f;

            return result;
        }

        public static float[,] MatrixProduct(float[,] matrixA, float[,] matrixB)
        {
            int aRows = matrixA.GetLength(0); int aCols = matrixA.GetLength(1);
            int bRows = matrixB.GetLength(0); int bCols = matrixB.GetLength(1);
            if (aCols != bRows)
                throw new Exception("Non-conformable matrices in MatrixProduct");

            float[,] result = new float[aRows, bCols];

            for (int i = 0; i < aRows; ++i) // each row of A
                for (int j = 0; j < bCols; ++j) // each col of B
                    for (int k = 0; k < aCols; ++k) // could use k less-than bRows
                        result[i,j] += matrixA[i,k] * matrixB[k,j];

            return result;
        }

        public static float[] MatrixProduct(float[,] matrixA, float[] vectB)
        {
            int aRows = matrixA.GetLength(0); int aCols = matrixA.GetLength(1);
            int bRows = vectB.Length;
            if (aCols != bRows)
                throw new Exception("Non-conformable matrices in MatrixProduct");

            float[] result = new float[bRows];

            for (int i = 0; i < aCols; ++i) // each row of A
                for (int j = 0; j < bRows; ++j) // each col of B
                        result[i] += matrixA[i, j] * vectB[j];
            return result;
        }
        public static float[,] MatrixProduct(float[,] matrixA, float B)
        {
            int aRows = matrixA.GetLength(0); int aCols = matrixA.GetLength(1);


            for (int i = 0; i < aRows; ++i) // each row of A
                for (int j = 0; j < aCols; ++j) // each col of B
                    matrixA[i,j] *= B;
            return matrixA;
        }

        public static float[,] Transpose(float[,] matrix)
        {
            // rows
            int m = matrix.GetLength(0);
            //columns
            int n = matrix.GetLength(1);


            float[,] result = new float[m, n];


            for (int i = 0; i < m; i++)
                for (int j = 0; j < n; j++)
                    result[j,i] = matrix[i,j];

            return result;
        }

        public static float[] Substract(float[] vectA, float[] vectB)
        {
            for (int i = 0; i < vectA.Length; ++i) // each row of A
              vectA[i] -= vectB[i];
            return vectA;
        }
        public static float[] Add(float[] vectA, float[] vectB)
        {
            for (int i = 0; i < vectA.Length; ++i) // each row of A
                vectA[i] += vectB[i];
            return vectA;
        }
        public static float[,] Add(float[,] vectA, float[,] vectB)
        {
            for (int i = 0; i < vectA.GetLength(0); ++i) // each row of A
                for (int j = 0; j < vectA.GetLength(1); ++j)
                    vectA[i,j] += vectB[i,j];
            return vectA;
        }

        public static float[,] DampInv(float[,] A, float damping)
        {
            float[,] damp = MatrixProduct(MatrixIdentity(A.GetLength(0)), (damping * damping));
            float[,] AT = Transpose(A);
            float[,] AAT = MatrixProduct(A, AT);
            float[,] inv = MatrixInverse(Add(AAT, damp));
            return MatrixProduct(AT,  inv);
        }

        public static float[,] MatrixInverse(float[,] matrix)
        {
            int n = matrix.GetLength(0);
            float[,] result = MatrixDuplicate(matrix);

            int[] perm;
            int toggle;
            float[,] lum = MatrixDecompose(matrix, out perm,
              out toggle);
            if (lum == null)
                throw new Exception("Unable to compute inverse");

            float[] b = new float[n];
            for (int i = 0; i < n; ++i)
            {
                for (int j = 0; j < n; ++j)
                {
                    if (i == perm[j])
                        b[j] = 1.0f;
                    else
                        b[j] = 0.0f;
                }
                float[] x = HelperSolve(lum, b);

                for (int j = 0; j < n; ++j)
                    result[j,i] = x[j];
            }
            return result;
        }

        public static float[,] MatrixDuplicate(float[,] matrix)
        {
            // allocates/creates a duplicate of a matrix.
            float[,] result = new float[matrix.GetLength(0), matrix.GetLength(1)];
            for (int i = 0; i < matrix.GetLength(0); ++i) // copy the values
                for (int j = 0; j < matrix.GetLength(1); ++j)
                    result[i,j] = matrix[i,j];
            return result;
        }

        static float[] HelperSolve(float[,] luMatrix, float[] b)
        {
            // before calling this helper, permute b using the perm array
            // from MatrixDecompose that generated luMatrix
            int n = luMatrix.GetLength(0);
            float[] x = new float[n];
            b.CopyTo(x, 0);

            for (int i = 1; i < n; ++i)
            {
                float sum = x[i];
                for (int j = 0; j < i; ++j)
                    sum -= luMatrix[i,j] * x[j];
                x[i] = sum;
            }

            x[n - 1] /= luMatrix[n - 1,n - 1];
            for (int i = n - 2; i >= 0; --i)
            {
                float sum = x[i];
                for (int j = i + 1; j < n; ++j)
                    sum -= luMatrix[i,j] * x[j];
                x[i] = sum / luMatrix[i,i];
            }

            return x;
        }

        public static float[,] MatrixDecompose(float[,] matrix, out int[] perm, out int toggle)
        {
            // Doolittle LUP decomposition with partial pivoting.
            // rerturns: result is L (with 1s on diagonal) and U;
            // perm holds row permutations; toggle is +1 or -1 (even or odd)
            int rows = matrix.GetLength(0);
            int cols = matrix.GetLength(1); // assume square
            if (rows != cols)
                throw new Exception("Attempt to decompose a non-square m");

            int n = rows; // convenience

            float[,] result = MatrixDuplicate(matrix);

            perm = new int[n]; // set up row permutation result
            for (int i = 0; i < n; ++i) { perm[i] = i; }

            toggle = 1; // toggle tracks row swaps.
                        // +1 -greater-than even, -1 -greater-than odd. used by MatrixDeterminant

            for (int j = 0; j < n - 1; ++j) // each column
            {
                float colMax = Math.Abs(result[j,j]); // find largest val in col
                int pRow = j;
                //for (int i = j + 1; i less-than n; ++i)
                //{
                //  if (result[i][j] greater-than colMax)
                //  {
                //    colMax = result[i][j];
                //    pRow = i;
                //  }
                //}

                // reader Matt V needed this:
                for (int i = j + 1; i < n; ++i)
                {
                    if (Math.Abs(result[i,j]) > colMax)
                    {
                        colMax = Math.Abs(result[i,j]);
                        pRow = i;
                    }
                }
                // Not sure if this approach is needed always, or not.

                if (pRow != j) // if largest value not on pivot, swap rows
                {
                    float[] rowPtr = result.GetRow(pRow);
                    result.SetRow(pRow, result.GetRow(j));
                    result.SetRow(j, rowPtr);

                    int tmp = perm[pRow]; // and swap perm info
                    perm[pRow] = perm[j];
                    perm[j] = tmp;

                    toggle = -toggle; // adjust the row-swap toggle
                }

                // --------------------------------------------------
                // This part added later (not in original)
                // and replaces the 'return null' below.
                // if there is a 0 on the diagonal, find a good row
                // from i = j+1 down that doesn't have
                // a 0 in column j, and swap that good row with row j
                // --------------------------------------------------

                if (result[j,j] == 0.0f)
                {
                    // find a good row to swap
                    int goodRow = -1;
                    for (int row = j + 1; row < n; ++row)
                    {
                        if (result[row,j] != 0.0f)
                            goodRow = row;
                    }

                    if (goodRow == -1)
                        return result;

                    // swap rows so 0.0 no longer on diagonal
                    float[] rowPtr = result.GetRow(goodRow);
                    result.SetRow(goodRow, result.GetRow(j));
                    result.SetRow(j,rowPtr);

                    int tmp = perm[goodRow]; // and swap perm info
                    perm[goodRow] = perm[j];
                    perm[j] = tmp;

                    toggle = -toggle; // adjust the row-swap toggle
                }
                // --------------------------------------------------
                // if diagonal after swap is zero . .
                //if (Math.Abs(result[j][j]) less-than 1.0E-20)
                //  return null; // consider a throw

                for (int i = j + 1; i < n; ++i)
                {
                    result[i,j] /= result[j,j];
                    for (int k = j + 1; k < n; ++k)
                    {
                        result[i,k] -= result[i,j] * result[j,k];
                    }
                }


            } // main j column loop

            return result;
        }




    }
}