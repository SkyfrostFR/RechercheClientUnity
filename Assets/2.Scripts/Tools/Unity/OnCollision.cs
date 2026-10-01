using UnityEngine;




namespace DT.Tools
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Collider))]
    public class OnCollision : MonoBehaviour
    {

        public delegate void OnCollisionEvent(Collider collider);

        OnCollisionEvent callBackEnter;
        OnCollisionEvent callBackExit;


        private void Start()
        {
            gameObject.GetComponent<Collider>().isTrigger = true;

        }


        private void OnTriggerEnter(Collider other)
        {
            callBackEnter.Invoke(other);
        }

        private void OnTriggerExit(Collider other)
        {
            callBackExit.Invoke(other);
        }

        public void enterEvent(OnCollisionEvent onCollisionEvent)
        {

            callBackEnter = onCollisionEvent;
        }
        public void exitEvent(OnCollisionEvent onCollisionEvent)
        {

            callBackExit = onCollisionEvent;
        }
    }

}
