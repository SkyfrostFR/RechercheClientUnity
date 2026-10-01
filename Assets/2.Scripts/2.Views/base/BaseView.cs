using DT.Controller;
using DT.Model;
using UnityEngine;

namespace DT.View
{
    public class BaseView<M, C> : MonoBehaviour
    where M : BaseModel
    where C : BaseController<M>, new()
    {
        public M Model;
        protected C Controller;

        // Use start for allowing model to be set before call
        public virtual void Start()
        {
            Controller = new C();
            Controller.Setup(Model);
        }
    }
}

