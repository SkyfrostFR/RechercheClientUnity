using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;


/// <summary>
/// Some C# extensions for data exchange
/// </summary>
public static class Extensions
{

    /// <summary>
    /// Generic method to convert structure to binary for UDP communication or binary filewriting
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="structure"></param>
    /// <returns></returns>
    public static byte[] GetBytes<T>(this T structure)
    {
        var arr = new List<byte>();

        FieldInfo[] fields = typeof(T).GetFields(BindingFlags.Public | BindingFlags.Instance);
        for (int i = 0; i < fields.Length; i++)
        {
            if (fields[i].FieldType.IsArray)
            {
                Array array = fields[i].GetValue(structure) as Array;
                for (int j = 0; j < array.Length; j++)
                {
                    if (array.GetType().GetElementType() == typeof(bool))
                    {
                        arr.AddRange(BitConverter.GetBytes((bool)array.GetValue(j)));
                        continue;
                    }
                    if (array.GetType().GetElementType() == typeof(double))
                    {
                        arr.AddRange(BitConverter.GetBytes((double)array.GetValue(j)));
                        continue;
                    }
                    if (array.GetType().GetElementType() == typeof(int))
                    {
                        arr.AddRange(BitConverter.GetBytes((int)array.GetValue(j)));
                        continue;
                    }
                    if (array.GetType().GetElementType() == typeof(float))
                    {
                        arr.AddRange(BitConverter.GetBytes((float)array.GetValue(j)));
                        continue;
                    }
                }
            }
            else
            {
                if (fields[i].FieldType == typeof(bool))
                {
                    arr.AddRange(BitConverter.GetBytes((bool)fields[i].GetValue(structure)));
                    continue;
                }
                if (fields[i].FieldType == typeof(double))
                {
                    arr.AddRange(BitConverter.GetBytes((double)fields[i].GetValue(structure)));
                    continue;
                }
                if (fields[i].FieldType == typeof(int))
                {
                    arr.AddRange(BitConverter.GetBytes((int)fields[i].GetValue(structure)));
                    continue;
                }
                if (fields[i].FieldType == typeof(float))
                {
                    arr.AddRange(BitConverter.GetBytes((float)fields[i].GetValue(structure)));
                    continue;
                }
            }

        }



        return arr.ToArray();
    }

    /// <summary>
    /// Extract part of an array 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="array"></param>
    /// <param name="offset"></param>
    /// <param name="length"></param>
    /// <returns></returns>
    public static T[] SubArray<T>(this T[] array, int offset, int length)
    {
        T[] result = new T[length];
        Array.Copy(array, offset, result, 0, length);
        return result;
    }

    /// <summary>
    /// Generic method to convert binary to structure for UDP communication or binary file writing
    ///
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="str"></param>
    /// <param name="arr"></param>
    /// <returns></returns>
    public static T FromBytes<T>(this T str, byte[] arr)
    {
        if (arr == null)
        {

            return str;
        }

        int index = 0;
        FieldInfo[] fields = typeof(T).GetFields(BindingFlags.Public | BindingFlags.Instance);
        for (int i = 0; i < fields.Length; i++)
        {
            if (fields[i].FieldType.IsArray)
            {

                int size = 0;
                Array array = fields[i].GetValue(str) as Array;
                Type type = array.GetType().GetElementType();
                if (type == typeof(bool))
                {
                    size = 1;
                    var d = new List<bool>();
                    for (int j = 0; j < array.Length; j++)
                    {

                        byte[] sub = arr.SubArray(index, size);
                        array.SetValue(BitConverter.ToBoolean(sub, 0), j);
                        index += size;
                    }
                    fields[i].SetValue(str, array);
                }
                if (type == typeof(double))
                {
                    size = sizeof(double);
                    var d = new List<double>();
                    for (int j = 0; j < array.Length; j++)
                    {
                        byte[] sub = arr.SubArray(index, size);
                        array.SetValue(BitConverter.ToDouble(sub, 0), j);
                        index += size;
                    }
                    fields[i].SetValue(str, array);
                }
                if (type == typeof(int))
                {
                    size = sizeof(int);
                    var d = new List<int>();
                    for (int j = 0; j < array.Length; j++)
                    {

                        byte[] sub = arr.SubArray(index, size);
                        array.SetValue(BitConverter.ToInt32(sub, 0), j);
                        index += size;
                    }
                    fields[i].SetValue(str, array);
                }
                if (type == typeof(float))
                {
                    size = sizeof(float);
                    var d = new List<float>();
                    for (int j = 0; j < array.Length; j++)
                    {

                        byte[] sub = arr.SubArray(index, size);
                        array.SetValue(BitConverter.ToDouble(sub, 0), j);
                        index += size;
                    }
                    fields[i].SetValue(str, array);
                }
            }
            else
            {
                int size = 0;
                byte[] d;
                if (fields[i].FieldType == typeof(bool))
                {
                    size = 1;
                    d = arr.SubArray(index, size);
                    fields[i].SetValue(str, BitConverter.ToBoolean(d, 0));
                }
                else if (fields[i].FieldType == typeof(int))
                {
                    size = Marshal.SizeOf(fields[i].FieldType);
                    d = arr.SubArray(index, size);
                    fields[i].SetValue(str, BitConverter.ToInt32(d, 0));
                }
                else if (fields[i].FieldType == typeof(double))
                {
                    size = Marshal.SizeOf(fields[i].FieldType);
                    d = arr.SubArray(index, size);
                    fields[i].SetValue(str, BitConverter.ToDouble(d, 0));
                }

                index += size;

            }
        }
        return str;
    }

    //Depreciated 
    /* public static byte[] GetBytes(this object obj)
     {
         if (obj == null)
             return null;

         BinaryFormatter bf = new BinaryFormatter();
         MemoryStream ms = new MemoryStream();
         bf.Serialize(ms, obj);

         return ms.ToArray();
     }*/


    /// <summary>
    ///  Generic method to convert bin to Object
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="obj"></param>
    /// <param name="arr"></param>
    /// <returns></returns>
    public static T ObjectFromBytes<T>(this T obj, byte[] arr) where T : class
    {
        MemoryStream memStream = new MemoryStream(arr);
        BinaryFormatter binForm = new BinaryFormatter();
        memStream.Position = 0;
        obj = (T)binForm.Deserialize(memStream);

        return obj;
    }


    /// <summary>
    /// Simple wrapper
    /// </summary>
    /// <param name="filePath"></param>
    /// <returns></returns>
    public static bool FileExist(this string filePath)
    {
        return File.Exists(filePath);
    }

    /// <summary>
    /// Serialize any object (struct, class, ...) to a json file
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="objectToWrite"></param>
    /// <param name="filePath"></param>
    /// <param name="append">Append data at the end of the file if it exists</param>
    public static void WriteToJsonFile<T>(this T objectToWrite, string filePath, bool append = false) where T : new()
    {
        TextWriter writer = null;
        try
        {
            // Try to creat dir if doesn't exists
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            var contentsToWriteToFile = JsonConvert.SerializeObject(objectToWrite);
            writer = new StreamWriter(filePath, append);
            writer.Write(contentsToWriteToFile);
        }
        finally
        {
            if (writer != null)
                writer.Close();
        }
    }

    /// <summary>
    /// Deserialize a json file to a generic object 
    /// </summary>
    /// <typeparam name="T">The type of the object to deserialize to</typeparam>
    /// <param name="filePath">Path to the json file</param>
    /// <returns>Deserialized object of type T</returns>
    public static T ReadFromJsonFile<T>(this T objectToRead, string filePath) where T : new()
    {
        TextReader reader = null;
        try
        {
            reader = new StreamReader(filePath);
            var fileContents = reader.ReadToEnd();
            return JsonConvert.DeserializeObject<T>(fileContents);
        }
        finally
        {
            if (reader != null)
                reader.Close();
        }
    }

}


