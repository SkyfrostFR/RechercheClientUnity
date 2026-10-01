using System.Collections.Generic;
using DT.Model;

namespace DT
{
    public class VariableManager : Singleton<VariableManager>
    {
        public List<Variable> Variables = new List<Variable>();


        public void RegisterNewVar(Variable var)
        {
            Variables.Add(var);
        }

        //Get one value in list
        public dynamic GetVariable(string name)
        {
            return Variables.Find(x => x.Name == name);
        }

    }
}

