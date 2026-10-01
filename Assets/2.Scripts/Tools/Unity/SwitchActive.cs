using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwitchActive : MonoBehaviour
{
    public void Switch()
    {
        if (gameObject.activeSelf)
        {
            gameObject.SetActive(false);
        }
        else
        {
            gameObject.SetActive(true);
        }
    }
}
