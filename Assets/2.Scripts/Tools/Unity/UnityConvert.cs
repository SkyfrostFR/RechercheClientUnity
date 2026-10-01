using System;
using System.Collections.Generic;
using UnityEngine;

public static class UnityConvert
{
    // Facteur de conversion milimètre vers mètre
    public const float mmTom = 0.001f;
    // Facteur de conversion degrés vers radians.
    public const float DgToRd = Mathf.PI / 180.0f;
    public const float RdToDg =  180.0f / Mathf.PI ;


    public static float ToMM(this float value)
    {
        return value * 1000.0f;
    }

    public static Vector3[] ToSI(Vector3[] vect,float UnitToSI = mmTom)
    {
        for(int i=0; i<vect.Length; i++)
        {
            vect[i] *= UnitToSI;
        }
        return vect;
    }

    public static Vector2[] ToSI(Vector2[] vect, float UnitToSI = mmTom)
    {
        for (int i = 0; i < vect.Length; i++)
        {
            vect[i] *= UnitToSI;
        }
        return vect;
    }

    public static float[] TransformVector(float[] vect)
    {
        float[] result = new float[vect.Length];
        if(vect.Length >= 3)
        {
            Vector3 in1 = new Vector3(vect[0], vect[1], vect[2]);
            in1 = in1.ToDirectCoor();
            result[0] = in1.x;
            result[1] = in1.y;
            result[2] = in1.z;
        }
        if (vect.Length >= 6)
        {
            Vector3 in2 = new Vector3(vect[3], vect[4], vect[5]);
            result[3] = -in2.y;
            result[4] = -in2.x;
            result[5] = -in2.z;
        }
        return result;
    }


    public static float[,] TransformJacobian(this float[,] jacobianA)
    {
        int rowsA = jacobianA.GetLength(0);
        int colsA = jacobianA.GetLength(1);

        // Initialize the transformed Jacobian matrix
        float[,] jacobianB = new float[rowsA, colsA];

        // Apply the transformation to each column of the Jacobian
        for (int col = 0; col < colsA; col++)
        {
            // Extract the column vector from the original Jacobian
            float[] columnVectorA = new float[rowsA];
            for (int row = 0; row < rowsA; row++)
            {
                columnVectorA[row] = jacobianA[row, col];
            }

            // Initialize the transformed column vector
            float[] transformedColumnVector = TransformVector(columnVectorA);

            // Update the transformed Jacobian matrix with the new column vector
            for (int row = 0; row < rowsA; row++)
            {
                jacobianB[row, col] = transformedColumnVector[row];
            }
        }

        return jacobianB;
    }
    //---------------------------------------------------------------------------------------------------------------------
    //! \brief      Méthode calculant les coordonnées d'un point, d'un vecteur dans le repère indirect (main gauche)
    //!             à partir des coordonnées de ce point, de ce vecteur exprimées dans le repère direct (main droite).
    //! \param      DirectCoor : Coordonnées d'un point, d'un vecteur dans le repère direct (main droite). Les coordonnées
    //!             du point, du vecteur sont exprimées en unités utilisateur (Units).
    //! \param      UnitToSI : Facteur de conversion unités utilisateur vers le système d'unité de Unity, ici le mètre.
    //! \return     Retourne les coordonnées du point, du vecteur dans le repère indirect (main gauche). Les coordonnées
    //!             du point, du vecteur sont exprimées dans le système d'unité de Unity c'est-à-dire en mètres.
    public static Vector3 ToIndirectCoor(this Vector3 DirectCoor, float UnitToSI = 1)
    {
        Vector3 Res;
        Res.x = -DirectCoor.y * UnitToSI;
        Res.y = DirectCoor.z * UnitToSI;
        Res.z = DirectCoor.x * UnitToSI;
        return (Res);
    }

    public static Vector3[] ToIndirectCoor(this Vector3[] DirectCoor, float UnitToSI = 1)
    {
        List<Vector3> Res = new List<Vector3>();
        for (int i = 0; i < DirectCoor.Length; i++)
        {
            Res.Add(ToIndirectCoor(DirectCoor[i], UnitToSI));
        }
        return (Res.ToArray());
    }

    //---------------------------------------------------------------------------------------------------------------------
    public static Vector3 ToDirectCoor(this Vector3 inDirectCoor, float UnitToSI = 1)
    {
        Vector3 Res;
        // Calcul de la translation
        Res.x = inDirectCoor.z * UnitToSI;
        Res.y = -inDirectCoor.x * UnitToSI;
        Res.z = inDirectCoor.y * UnitToSI;
        return (Res);
    }

    //---------------------------------------------------------------------------------------------------------------------
    //! \brief      Méthode calculant dans le repère indirect (main gauche) le quaternion représentant une orientation
    //!             à partir des paramètres d'orientation de Euler (cardan) XYZ exprimées dans le repère direct (main droite).
    //! \param      EulerXYZ : Paramètre d'orientation d'Euler (cardan) XYZ dans le repère direct (main droite). Les angles
    //!             X, Y et Z sont exprimées en unités utilisateur (Units).
    //! \param      UnitToSI : Facteur de conversion unités utilisateur vers radian.
    //! \return     Retourne le quaternion représentant l'orientation dans le repère indirect (main gauche).
    public static Quaternion ToIndirectRot(this Vector3 EulerXYZ, float UnitToSI = 1)
    {
        // EulerXYZ holds URDF/ROS roll-pitch-yaw (degrees, as the importer applies Rad2Deg).
        // The rotation MUST be built in ROS' right-handed frame first
        // (R = Rz(yaw)·Ry(pitch)·Rx(roll)), then swapped into Unity's left-handed frame.
        // Feeding RPY straight into Quaternion.Euler is wrong: Unity composes in a
        // left-handed ZXY order, so any combined roll+pitch+yaw yielded the wrong orientation.
        return UnityConvert.ToIndirectRot(RosQuatFromRPY(EulerXYZ * UnitToSI));
    }

    //---------------------------------------------------------------------------------------------------------------------
    //! \brief      Construit le quaternion (repère direct ROS, main droite) à partir des angles
    //!             roll-pitch-yaw URDF (en degrés). Convention : R = Rz(yaw)·Ry(pitch)·Rx(roll).
    private static Quaternion RosQuatFromRPY(Vector3 rpyDeg)
    {
        float r = rpyDeg.x * DgToRd * 0.5f; // roll  (X)
        float p = rpyDeg.y * DgToRd * 0.5f; // pitch (Y)
        float y = rpyDeg.z * DgToRd * 0.5f; // yaw   (Z)

        float cr = Mathf.Cos(r), sr = Mathf.Sin(r);
        float cp = Mathf.Cos(p), sp = Mathf.Sin(p);
        float cy = Mathf.Cos(y), sy = Mathf.Sin(y);

        Quaternion q;
        q.w = cr * cp * cy + sr * sp * sy;
        q.x = sr * cp * cy - cr * sp * sy;
        q.y = cr * sp * cy + sr * cp * sy;
        q.z = cr * cp * sy - sr * sp * cy;
        return q;
    }
    public static Quaternion ToDirectRot(this Vector3 EulerXYZ, float UnitToSI = 1)
    {
        return (Quaternion.Euler(EulerXYZ * UnitToSI).ToDirectRot());
    }
    //---------------------------------------------------------------------------------------------------------------------
    //---------------------------------------------------------------------------------------------------------------------
    //! \brief      Méthode calculant dans le repère indirect (main gauche) le quaternion représentant une orientation
    //!             à partir du quaternion exprimées dans le repère direct (main droite).
    //! \return     Retourne le quaternion représentant l'orientation dans le repère indirect (main gauche).
    public static Quaternion ToIndirectRot(this Quaternion q)
    {
        // Change quaternion to indirect
        return new Quaternion(q.y, -q.z, -q.x, q.w);
    }
    //---------------------------------------------------------------------------------------------------------------------
    //---------------------------------------------------------------------------------------------------------------------
    //! \brief      Méthode calculant dans le repère direct (main droite) le quaternion représentant une orientation
    //!             à partir des paramètres d'orientation de Euler ZXY (localRotation.eulerAngles) exprimées dans le repère indirect.
    //! \param      EulerZXY : Paramètre d'orientation d'Euler ZXY (localRotation.eulerAngles) dans le repère indirect. Les angles
    //!             X, Y et Z sont exprimées en unités utilisateur (Units).
    //! \param      UnitToSI : Facteur de conversion unités utilisateur vers radian.
    //! \return     Retourne le quaternion représentant l'orientation dans le repère direct.
    public static Quaternion ToDirectRot(this Quaternion q)
    {
        // Change quaternion direct
        return new Quaternion(-q.z, q.x, -q.y, q.w);
    }

    //------------------------------------------------------------------------------------------------------------------


    // ------------------------------------- MESH CONVERTER ------------ C. Sept. 2020 ------------------------------------
    /// <summary>
    /// Flip uvs + add Unit conversion ! (avoid 2 loops)
    /// </summary>
    /// <param name="uvs"></param>
    /// <param name="UnitToSI"></param>
    /// <returns></returns>
    public static Vector2[] FlipUvsToSI(Vector2[] uvs, float UnitToSI =mmTom)
    {
        float x;
        for (int i = 0; i < uvs.Length; i++)
        {
            x = uvs[i].x;
            uvs[i].x = uvs[i].y * UnitToSI;
            uvs[i].y = x * UnitToSI;
        }
        return uvs;
    }

    /// <summary>
    /// OCCT uses counter clockwise winding for triangles / Unity uses clockwise winding
    /// Swap n2 / n3
    /// http://www.martin-ritter.com/2019/01/unity-mesh-generation-vertices-triangles-winding/
    /// </summary>
    /// <param name="triangles"></param>
    /// <returns></returns>
    public static int[] ToClockwiseWinding(int[] triangles)
    {
        int n2;
        for (int i = 0; i < triangles.Length/3; i++)
        {
            n2 = triangles[i * 3 + 1];
            triangles[i * 3 + 1] = triangles[i * 3 + 2];
            triangles[i * 3 + 2] = n2;
        }
        return triangles;
    }


    }
