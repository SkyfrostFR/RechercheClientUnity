using System;

namespace DT.Tools
{
    [Serializable]
    public class Unit
    {

        public string Id = "";
        public int OpcUAId = 0;
        public string Name = "Default";
        public string Description = "Default";
        public float ToSIConvertFactor = 1.0f;


        public Unit Clone()
        {
            return (Unit)this.MemberwiseClone();
        }

    }

    
}