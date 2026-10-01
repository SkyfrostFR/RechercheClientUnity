using Newtonsoft.Json;
using UnityEngine;


namespace DT.Model
{
    public partial class LinkModel
    {
        [JsonIgnore]
        public GameObject gameObject { get; set; } // GameObject associated to LinkModel
        [JsonIgnore]
        public Texture2D Image { get; set; } // Texture 2D associated with Image of linkModel
    }
}

