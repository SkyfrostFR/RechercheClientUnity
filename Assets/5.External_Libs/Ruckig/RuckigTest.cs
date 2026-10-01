using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Windows;

public class RuckigTest : MonoBehaviour
{

   public Ruckig otg = new Ruckig(3, 0.01);

    // Start is called before the first frame update
    void Start()
    {
        otg.Input.current_position = new double[] { 0.0, 0.0, 0.5};
        otg.Input.current_velocity = new double[] { 0.0, -2.2, -0.5};
        otg.Input.current_acceleration = new double[] { 0.0, 2.5, -0.5};

        otg.Input.target_position = new double[] { 5.0, -2.0, -3.5};
        otg.Input.target_velocity = new double[] { 0.0, -0.5, -2.0};
        otg.Input.target_acceleration = new double[] { 0.0, 0.0, 0.5};

        otg.Input.max_velocity = new double[] { 3.0, 1.0, 3.0};
        otg.Input.max_acceleration = new double[] { 3.0, 2.0, 1.0};
        otg.Input.max_jerk = new double[] { 4.0, 3.0, 2.0};

        
    }

    public bool test = false;

    // Update is called once per frame
    void Update()
    {
        if (test)
        {
            if (otg.Update() == Ruckig.Result.Working)
            {
                Debug.Log("Time : " + otg.Output.time + "  -  " + string.Join("; ", otg.Output.new_position));
               
                otg.OutputToInput();
            }
            else
            {
                test = false;
            }
        }

        
    }


}
