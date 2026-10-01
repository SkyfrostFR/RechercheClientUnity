
using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class Highlighter : MonoBehaviour
{

    public bool isHovering = false;
    private bool first = true;

    private Material highlightMat ;

    private MeshRenderer[] existingRenderers;



    private void Awake()
    {

        highlightMat = new Material(Resources.Load<Material>("Outline2"));
        existingRenderers = gameObject.GetComponentsInChildren<MeshRenderer>(); 

        GenerateMouseOverHighlight(gameObject);
    }

   


    private void Update()
    {
        UpdateHighlightRenderers();

        if (isHovering)
        {
            first = false;
            isHovering = true;

        }
        if ((isHovering == false ))
        {
            first = true;
            isHovering = false;
            
        }
    }



    public void SetOn()
    {
        isHovering = true;
    }

    public void Set(bool on)
    {
        isHovering = on;
    }
    public void SetOff()
    {
        isHovering = false;
    }


   


    bool ShouldIgnore(GameObject check)
    {
        if (check.layer != 0) // Ignore objects not in default layer
        {
            return true;
        }

        return false;
    }
    bool ShouldIgnoreHighlight(Component component)
    {
        return ShouldIgnore(component.gameObject);
    }


   void UpdateHighlightRenderers()
    {

        for (int rendererIndex = 0; rendererIndex < existingRenderers.Length; rendererIndex++)
        {
            MeshRenderer renderer = existingRenderers[rendererIndex];

            if (renderer != null && isHovering)
            {
                renderer.materials = renderer.materials.Add(highlightMat);
            }
            if (renderer != null && !isHovering && first && renderer.materials.Length >1)
            {

                Material[] mats = new Material[renderer.materials.Length - 1];
                for (int i = 0; i < mats.Length; i++)
                {
                    mats[i] = renderer.materials[i];
                }
                renderer.materials = mats;
            }
        }
    }


    public static void GenerateMouseOverHighlight(GameObject go)
    {
        MeshFilter[] existingFilters = go.GetComponentsInChildren<MeshFilter>(true);

        foreach (MeshFilter mesh in existingFilters)
        {
            // Remove previous MeshColliders
            foreach (MeshCollider col in mesh.GetComponents<MeshCollider>())
            {
                DestroyImmediate(col);
            }

            MeshCollider collider = mesh.gameObject.AddComponent<MeshCollider>();
            if (mesh.gameObject.GetComponent<OnMouseAction>() == null)
            {
                mesh.gameObject.AddComponent<OnMouseAction>();
            }
            collider.convex = true;
            collider.isTrigger = true;
        }
    }

}
