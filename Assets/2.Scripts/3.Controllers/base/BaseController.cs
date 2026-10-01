using DT.Model;


namespace DT.Controller
{
    public class BaseController<M> where M : BaseModel
    {

        protected M Model;

        public virtual void Setup(M model)
        {
            Model = model;
        }

    }
}