using System;

namespace DT.Model
{
    /// <summary>
    /// Pré-requis pour exister dans une collection
    /// </summary>
    public interface IEntity
    {
        string   Id       { get; set; } // Id (GUID)
        DateTime Created  { get; set; } // Date de création
        DateTime Modified { get; set; } // Date de modification



    }
}
