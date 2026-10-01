using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.EventSystems;
using System.Linq;


namespace DT
{
    public static class UnityTools
    {
        


        public static void SetLayerRecursively(GameObject go, int layerNumber)
        {
            foreach (Transform trans in go.GetComponentsInChildren<Transform>(true))
            {
                trans.gameObject.layer = layerNumber;
            }
        }


        public static bool IsPointerOverGameObject(GameObject gameObject)
        {
            PointerEventData eventData = new PointerEventData(EventSystem.current);
            eventData.position = Input.mousePosition;
            List<RaycastResult> raysastResults = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, raysastResults);
            return raysastResults.Any(x => x.gameObject == gameObject);
        }

        public static List<GameObject> GetAllChildren(this GameObject Go)
        {
            List<GameObject> list = new List<GameObject>();
            for (int i = 0; i < Go.transform.childCount; i++)
            {
                list.Add(Go.transform.GetChild(i).gameObject);
            }
            return list;
        }
        public static List<GameObject> GetFirstChildren(this GameObject Go, int number)
        {
            List<GameObject> list = new List<GameObject>();
            for (int i = 0; i < number; i++)
            {
                list.Add(Go.transform.GetChild(i).gameObject);
            }
            return list;
        }
        public static List<GameObject> GetChildrenByName(this GameObject Go, string name)
        {
            List<GameObject> list = new List<GameObject>();
            Transform[] children = Go.GetComponentsInChildren<Transform>();
            for (int i = 0; i < children.Length; i++)
            {
                if (children[i].gameObject.name.Contains(name))
                {
                    list.Add(children[i].gameObject);
                }
            }
            return list;
        }
        public static List<GameObject> GetChildrenRecursive(this GameObject Go)
        {
            List<GameObject> list = new List<GameObject>();
            Transform[] children = Go.GetComponentsInChildren<Transform>();
            for (int i = 0; i < children.Length; i++)
            {
                list.Add(children[i].gameObject);
            }
            return list;
        }
        public static List<GameObject> GetChildrenRecursiveSorted(this GameObject Go)
        {
            List<GameObject> list = new List<GameObject>();
            List<GameObject> children = Go.GetAllChildren() ;
            for (int i = 0; i < children.Count; i++)
            {
                list.Add(children[i]);
                list.AddRange(children[i].GetChildrenRecursiveSorted());
            }
            return list;
        }

        public static T[] GetComponentsInChildrenByOrder<T>(this Component GO)
        {
            T[] temp = GO.GetComponentsInChildren<T>();
            T[] result = new T[temp.Length-1];
            int index = 0;
            List<GameObject> children = GO.gameObject.GetChildrenRecursiveSorted();

            for (int i = 0; i < children.Count; i++)
            {
                T childComponent = children[i].GetComponent<T>();
                if (childComponent != null)
                {
                    result[index] = childComponent;
                    index++;
                    if (index>=result.Length)
                    {
                        return result;
                    }
                }
            }
            return result;
        }

        public static T[] GetComponentsExact<T>(this GameObject aObj)
        {
            return aObj.GetComponents<T>().Where(a => a.GetType() == typeof(T)).ToArray();
        }
        public static T[] GetComponentsExactInChildren<T>(this Component aObj)
        {
            return aObj.GetComponentsInChildren<T>().Where(a => a.GetType() == typeof(T)).ToArray();
        }
        public static T GetComponentExact<T>(this GameObject aObj)
        {
            return aObj.GetComponents<T>().Where(a => a.GetType() == typeof(T)).FirstOrDefault();
        }
        public static T[] GetComponentsExact<T>(this Component aObj)
        {
            return aObj.GetComponents<T>().Where(a => a.GetType() == typeof(T)).ToArray();
        }
        public static T GetComponentExact<T>(this Component aObj)
        {
            return aObj.GetComponents<T>().Where(a => a.GetType() == typeof(T)).FirstOrDefault();
        }

        

        
        /// <summary>
        /// Distances between colliders using bounding box 
        /// </summary>
        /// <param name="A"></param>
        /// <param name="B"></param>
        /// <returns></returns>
        public static float GetDistance(Collider A, Collider B)
        {

            Bounds bounds0 = A.bounds;
            Bounds bounds1 = B.bounds;

            float sd0 = sdBounds(bounds0.center, bounds1, out Vector3 conjecture0);
            float sd1 = sdBounds(bounds1.center, bounds0, out Vector3 conjecture1);
            float dist = Vector3.Distance(conjecture0, conjecture1);



            return dist;
        }

        static float sdBounds(Vector3 point, Bounds bounds, out Vector3 contact)
        {
            Vector3 dir = point - bounds.center;
            float sd = sdBox(dir, bounds.extents);

            contact = point - dir.normalized * sd;
            // note: we dont need to know the real contact point in this case, this is pure conjecture

            return sd;
        }
        static float sdBox(Vector3 p, Vector3 b)
        {
            Vector3 q = new Vector3(Mathf.Abs(p.x), Mathf.Abs(p.y), Mathf.Abs(p.z)) - b;
            return Vector3.Magnitude(Vector3.Max(q, Vector3.zero)) + Mathf.Min(Mathf.Max(q.x, Mathf.Max(q.y, q.z)), 0f);
        }


        public static Vector3[] GetVertices(this Bounds bound)
        {
            Vector3[] vert = new Vector3[8];
            vert[0] = bound.min;
            vert[1] = bound.max;
            vert[2] = new Vector3(vert[0].x, vert[0].y, vert[1].z);
            vert[3] = new Vector3(vert[0].x, vert[1].y, vert[0].z);
            vert[4] = new Vector3(vert[1].x, vert[0].y, vert[0].z);
            vert[5] = new Vector3(vert[0].x, vert[1].y, vert[1].z);
            vert[6] = new Vector3(vert[1].x, vert[0].y, vert[1].z);
            vert[7] = new Vector3(vert[1].x, vert[1].y, vert[0].z);

            return vert;
        }
    }

}




