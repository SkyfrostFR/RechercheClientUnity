using System;
using UnityEngine;

namespace DT.Model
{
    [Serializable]
    public class BaseModel : IEntity
    {
        [field: SerializeField]
        public string Id { get; set; }
        [field: SerializeField]
        public DateTime Created { get; set; }
        public DateTime Modified { get; set; }

        public BaseModel()
        {
            Id = GuidGenerator.FetchID();
            Created = DateTime.Now;
        }


    }

}

