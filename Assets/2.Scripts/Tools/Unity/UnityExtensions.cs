using UnityEngine;



public static class Vector3DExtensions
{
    public static double[] toDouble(this Vector3 vect)
    {
        return new double[] { vect.x, vect.y, vect.z };
    }
    
    public static Vector3 toVector3(this double[] array)
    {
        if (array.Length != 3)
        {
            Debug.Log("Must be 3 elements array");
            return Vector3.zero;
        }
        else
        {
            return new Vector3(array.toFloat()[0], array.toFloat()[1], array.toFloat()[2]);
        }
    }


    

    public static void DebugLog<T>(this T[,] array)
    {
        string msg = "";

        for (int j = 0; j < array.GetLength(0); j++)
        {
            for (int i = 0; i < array.GetLength(1); i++)
            {
                msg += array[j,i].ToString() + "; ";
            }
            msg += "\n";
        }
        Debug.Log(msg);

    }
    public static void DebugLog(this ArticulationJacobian array, string name = "")
    {
        string msg = "";
        if (name != "")
        {
            msg += name + " = \n";
        }

        for (int j = 0; j < array.rows; j++)
        {
            for (int i = 0; i < array.columns; i++)
            {
                msg += array[j, i].ToString() + "; ";
            }
            msg += "\n";
        }
        Debug.Log(msg);

    }
    public static void DebugLog<T>(this T[] array)
    {
        string msg = "";

        for (int j = 0; j < array.GetLength(0); j++)
        {
            msg += array[j].ToString() + "; ";
        }
        Debug.Log(msg);

    }

}

public static class RectTransformExtensions
{
    public static void SetLeft(this RectTransform rt, float left)
    {
        rt.offsetMin = new Vector2(left, rt.offsetMin.y);
    }
    public static void SetRight(this RectTransform rt, float right)
    {
        rt.offsetMax = new Vector2(-right, rt.offsetMax.y);
    }
    public static void SetTop(this RectTransform rt, float top)
    {
        rt.offsetMax = new Vector2(rt.offsetMax.x, -top);
    }
    public static void SetBottom(this RectTransform rt, float bottom)
    {
        rt.offsetMin = new Vector2(rt.offsetMin.x, bottom);
    }
    public static float GetLeft(this RectTransform rt)
    {
        return rt.offsetMin.x;
    }
    public static float GetRight(this RectTransform rt)
    {
        return -rt.offsetMax.x;
    }
    public static float GetTop(this RectTransform rt){
        return -rt.offsetMax.y;
    }
    public static void SetWidth(this RectTransform rt, float width)
    {
        rt.sizeDelta = new Vector2(width, rt.sizeDelta.y);
    }
    public static void SetHeight(this RectTransform rt, float height)
    {
        rt.sizeDelta = new Vector2(rt.sizeDelta.x, height);
    }
    public static float GetWidth(this RectTransform rt)
    {
        var w = rt.rect.width;
        return w;
    }
    public static float GetHeight(this RectTransform rt)
    {
        var h = (rt.anchorMax.y - rt.anchorMin.y) * Screen.height + rt.sizeDelta.y;
        return h;
    }
}

public static class GameObjectsExtensions
{
    public static void DestroyImmediateIfExists<T>(this Transform transform) where T : Component
    {
        T component = transform.GetComponent<T>();
        if (component != null)
        {
            UnityEngine.Object.DestroyImmediate(component);
        }
    }

    public static T AddComponentIfNotExists<T>(this Transform transform) where T : Component
    {
        T component = transform.GetComponent<T>();
        if (component == null)
        {
            component = transform.gameObject.AddComponent<T>();
        }
        return component;
    }

    public static void SetParentAndAlign(this Transform transform, Transform parent, bool keepLocalTransform = true)
    {
        Vector3 localPosition = transform.localPosition;
        Quaternion localRotation = transform.localRotation;
        transform.parent = parent;
        if (keepLocalTransform)
        {
            transform.position = transform.parent.position + localPosition;
            transform.rotation = transform.parent.rotation * localRotation;
        }
        else
        {
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
        }
    }

    public static bool HasExactlyOneChild(this Transform transform)
    {
        return transform.childCount == 1;
    }

    public static void MoveChildTransformToParent(this Transform parent, bool transferRotation = true)
    {
        //Detach child in order to get a transform independent from parent
        Transform childTransform = parent.GetChild(0);
        parent.DetachChildren();

        //Copy transform from child to parent
        parent.position = childTransform.position;
        parent.localScale = childTransform.localScale;

        if (transferRotation)
        {
            parent.rotation = childTransform.rotation;
            childTransform.localRotation = Quaternion.identity;
        }

        // Reattach child
        childTransform.parent = parent;

        childTransform.localPosition = Vector3.zero;
        childTransform.localScale = Vector3.one;
        if (transferRotation)
        {
            childTransform.localRotation = Quaternion.identity;
        }
    }

}


