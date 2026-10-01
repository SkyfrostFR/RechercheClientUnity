using DT.Model;
using System.Collections.Generic;


public static class ExtensionModel
{
    public static T FindIdInList<T>(this List<T> list, string ID) where T : Link
    {

        return list.Find(x => x.Id == ID);
    }
}

