using DT.Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;


namespace DT
{
    public class DatabaseManager : Singleton<DatabaseManager>
    {
        public static string host { get; set; }

        [Obsolete]
        public static T Get<T>(string id) where T : BaseModel, new()
        {
            // Get name of Entity
            string modelType = typeof(T).Name;
            // Construct request
            UnityWebRequest request = UnityWebRequest.Get(host + "/api/" + modelType + "/" + id);
            // Send request
            request.SendWebRequest();
            // Notify user
            //MainScreenManager.Instance.statusBar.SetLeftInfo("Récupération de la base de données");
            // Wait for answer => TODO : better implementation
            while (!request.isDone && !request.isNetworkError)
            {
                // Set Progress data
                //MainScreenManager.Instance.statusBar.SetProgress(request.downloadProgress);

                System.Threading.Thread.Sleep(1);
            }
            //MainScreenManager.Instance.statusBar.SetLeftInfo("");

            if (request.isNetworkError)
            {
                return null;
            }
            // Get request data
            string result = request.downloadHandler.text;
            // Text to Object
            T data = JsonConvert.DeserializeObject<T>(result);

            return data;
        }

        [Obsolete]
        public static List<T> GetAll<T>() where T : BaseModel, new()
        {
            // Get name of Entity
            string modelType = typeof(T).Name;
            // Construct request
            UnityWebRequest request = UnityWebRequest.Get(host+ "/api/" + modelType + "/");
            // Send request
            request.SendWebRequest();
            // Notify user
            //MainScreenManager.Instance.statusBar.SetLeftInfo("Récupération de la base de données");
            // Wait for answer => TODO : better implementation
            while (!request.isDone && !request.isNetworkError)
            {
                // Set Progress data
                //MainScreenManager.Instance.statusBar.SetProgress(request.downloadProgress);

                System.Threading.Thread.Sleep(1);
            }
            //MainScreenManager.Instance.statusBar.SetLeftInfo("");

            if (request.isNetworkError)
            {
                return null;
            }
            // Get request data
            string result = request.downloadHandler.text;
            // Text to Object
            List<T> data = JsonConvert.DeserializeObject<List<T>>(result);

            return data;
        }

        [Obsolete]
        public static T Add<T>(T data) where T: BaseModel
        {
            // Update Modified date
            data.Created = DateTime.Now;
            data.Modified = DateTime.Now;
            // Get name of Entity
            string modelType = typeof(T).Name;
            // Construct request
            WWWForm form = new WWWForm();
            form.AddField("JSON", JsonConvert.SerializeObject(data));
            UnityWebRequest request = UnityWebRequest.Post(host + "/api/" + modelType + "/add/", form);

            // Send request
            request.SendWebRequest();

            // Notify user
            //MainScreenManager.Instance.statusBar.SetLeftInfo("Récupération de la base de données");
            // Wait for answer => TODO : better implementation
            while (!request.isDone && !request.isNetworkError)
            {
                // Set Progress data
                //MainScreenManager.Instance.statusBar.SetProgress(request.downloadProgress);

                System.Threading.Thread.Sleep(1);
            }
            //MainScreenManager.Instance.statusBar.SetLeftInfo("");
            return data;

        }

        [Obsolete]
        public static T Update<T>(T data) where T : BaseModel
        {
            // Update Modified date
            data.Modified = DateTime.Now;
            // Get name of Entity
            string modelType = typeof(T).Name;
            // Construct request
            WWWForm form = new WWWForm();
            form.AddField("JSON", JsonConvert.SerializeObject(data));
            UnityWebRequest request = UnityWebRequest.Post(host + "/api/" + modelType + "/update/", form);
            // Send request
            request.SendWebRequest();
            // Notify user
            //MainScreenManager.Instance.statusBar.SetLeftInfo("Mise à jour de la base de données");
            // Wait for answer => TODO : better implementation
            while (!request.isDone && !request.isNetworkError)
            {
                // Set Progress data
                //MainScreenManager.Instance.statusBar.SetProgress(request.downloadProgress);

                System.Threading.Thread.Sleep(1);
            }
            return data;

        }

        [Obsolete]
        public static string UploadImage(Texture2D image, string id = null)
        {
            // Convert 2D Texture to image
            byte[] data = image.EncodeToPNG();
            // Construct request
            WWWForm form = new WWWForm();
            form.AddBinaryData("IMG", data, id, "image/png");
            if (id == null)
            {
                id = GuidGenerator.FetchID();
            }
            form.AddField("ID", id);
            UnityWebRequest request = UnityWebRequest.Post(host + "/api/img/upload", form);
            // Send request
            request.SendWebRequest();
            // Wait for answer => TODO : better implementation
            while (!request.isDone && !request.isNetworkError)
            {
                System.Threading.Thread.Sleep(1);
            }
            // Get request data
            string result = request.downloadHandler.text;
            // return id  (md5 hash)
            return result;
        }

        [Obsolete]
        public static Texture2D GetImage(string id)
        {
            UnityWebRequest request = UnityWebRequest.Get(host + "/api/img/" + id + ".png");
            // Send request
            request.SendWebRequest();
            // Wait for answer => TODO : better implementation
            while (!request.isDone && !request.isNetworkError)
            {
                System.Threading.Thread.Sleep(1);
            }
            // Get result
            byte[] data = request.downloadHandler.data;
            // Return as texture
            Texture2D result = new Texture2D(2, 2); // don't care about size
            // Load png image
            result.LoadImage(data);

            return result;
        }


    }

    namespace Model
    {
        public partial class LinkModel
        {
            [Obsolete]
            public static LinkModel Get(string id)
            {
                LinkModel link = DatabaseManager.Get<LinkModel>(id);
                link.Image = DatabaseManager.GetImage(id);
                return link;
            }

            [Obsolete]
            public static List<LinkModel> GetAll()
            {
                List<LinkModel> links = DatabaseManager.GetAll<LinkModel>();

                // Get Image frome db
                for (int i = 0; i < links.Count; i++)
                {
                    links[i].Image = DatabaseManager.GetImage(links[i].Id);
                }
                return links;
            }

            [Obsolete]
            public LinkModel Add()
            {
                return DatabaseManager.Add<LinkModel>(this);
            }

            [Obsolete]
            public LinkModel Update()
            {
                return DatabaseManager.Update<LinkModel>(this);
            }
        }
    }

}



