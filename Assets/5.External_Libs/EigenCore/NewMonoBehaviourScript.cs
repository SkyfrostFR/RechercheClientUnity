using EigenCore.Core.Dense;
using System;
using UnityEngine;

public class NewMonoBehaviourScript : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        VectorXD q = new VectorXD(new double[2] { 10, 10 });
        VectorXD dx = new VectorXD(new double[3] { 0.3, 0.3, 0 });

        MatrixXD j = new MatrixXD(new double[6, 2]);
        j[0, 0] = 10;
        j[0, 1] = 20;
        j[1, 0] = 30;
        j[1, 1] = 40;

        j[5, 0] = 1;
        j[5, 1] = 1;

        MatrixXD j1 = MatrixXD.Identity(20);

        for (int i = 0; i < 10000; i++)
        {
            MatrixXD pinvJac = j.PseudoInverse();
            //VectorXD qDest = (pinvJac * (dx)) + q;
            MatrixXD test = j1.Inverse();

            Debug.Log(test);
            Debug.Log(i);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
