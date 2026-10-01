using UnityEngine;
using UnityEngine.EventSystems;

public class MouseDrag : MonoBehaviour
{

    private Vector3 screenPoint;
    private Vector3 offset;
    public GameObject Robot, OptimManager;


    //float GridSize = 0.05f;


/*    private void Update()
    {
        if (EventSystem.current.IsPointerOverGameObject())
        {
            RaycastHit hit = new RaycastHit();
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out hit))
                Debug.Log(hit.collider.transform.gameObject.name);
        }

    }*/



    void OnMouseDown()
    {
        /*screenPoint = Camera.main.WorldToScreenPoint(Robot.transform.position);
        offset = Robot.transform.position - Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, screenPoint.z));*/
    }

    void OnMouseDrag()
    {
        //Debug.Log("Drag");
        /*float distance_to_screen = Camera.main.WorldToScreenPoint(Robot.transform.position).z;
        Vector3 newPos = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, distance_to_screen));
        newPos.z = Robot.transform.position.z;
        // Snap on grid
        newPos.x = Mathf.RoundToInt(newPos.x / GridSize) * GridSize;
        newPos.y = Mathf.RoundToInt(newPos.y / GridSize) * GridSize;

        Robot.transform.position = newPos;*/
    }

    private void OnMouseUp()
    {


    }

}