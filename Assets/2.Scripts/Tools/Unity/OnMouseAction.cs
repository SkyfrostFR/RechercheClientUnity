using System;
using UnityEngine;
using UnityEngine.EventSystems;
using DT.Tools;

public class OnMouseAction : MonoBehaviour
{


    /*
     * Simple class that allows action if mouse is over children.
     *
     *
     */

    public bool isHovering { get; private set; }
    private bool previousHovering = false;

    MeshRenderer[] existingRenderers;



    private void Start()
    {
        existingRenderers = this.GetComponentsInChildren<MeshRenderer>(true);
    }


    private void Update()
    {
        CheckHovering();



        if (previousHovering != isHovering)
        {
            /*gameObject.Highlighter(isHovering);

            if (isHovering)
            {
                MainScreenManager.Instance.statusBar.SetLeftInfo(transform.parent.name + " - "+ gameObject.name);
            }
            else
            {
                if (MainScreenManager.Instance.statusBar.GetLeftInfo() == gameObject.name)
                {
                    MainScreenManager.Instance.statusBar.SetLeftInfo("");
                }
            }*/

        }

        previousHovering = isHovering;


    }


    void CheckHovering()
    {
        if (EventSystem.current.IsPointerOverGameObject() && existingRenderers != null)
        {
            RaycastHit hit = new RaycastHit();
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out hit))
            {
                if (Array.Exists(existingRenderers, element => (element != null && element.gameObject == hit.collider.transform.gameObject)))
                {
                    isHovering = true;
                    return;
                }
            }
        }
        isHovering = false;
        return;
    }



}
