using Newtonsoft.Json.Converters;
using Newtonsoft.Json;
using System;
using UnityEngine;
using DT.Model;


namespace DT.Service
{
    public class MessageService : IService
    {
        // Default 
        public bool EnableDebugFallback = true;
        public bool EnableLogFileFallback = true;

        public string logPath { get; private set; }


        public MessageService() 
        { 
            logPath = "Logs/" + DateTime.Now.ToString("dd_MM_yyyy") + "_" + GuidGenerator.FetchID() + ".json";
        }

        public MessageService(string path)
        {
            logPath = path;
        }




        [Serializable]
        public enum MessageType
        {
            Info,
            Warn,
            Error,
            Progress
        }

        [Serializable]
        public struct Message
        {
            public string dateTime { get { return DateTime.Now.ToString("dd-MM-yyyy HH-mm-ss"); } private set { } }
            [JsonConverter(typeof(StringEnumConverter))]
            public MessageType type;
            public string message;
        }


        // Event on message reception
        // UI has to register to this
        public delegate void OnMsgDelegate(Message message);
        public event OnMsgDelegate OnNewMessage;


        public Message message
        {
            get { return message; }
            set
            {
                SetMessage(value);
            }
        }

        public void SetMessage(string message, MessageType type = MessageType.Info)
        {
            Message msg = new Message();
            msg.message = message;
            msg.type = type;
            SetMessage(msg);

        }

        public void SetMessage(Message message)
        {

            if (this.OnNewMessage != null)
            {
                this.OnNewMessage(message);
            }
            if (EnableDebugFallback)
            {
                // Debug 
                switch (message.type)
                {
                    case MessageType.Info:
                        Debug.Log(message.message);
                        break;
                    case MessageType.Warn:
                        Debug.LogWarning(message.message);
                        break;
                    case MessageType.Error:
                        Debug.LogError(message.message);
                        break;
                    case MessageType.Progress:
                        Debug.Log(message.message + "%");
                        break;
                    default:
                        break;
                }
            }
            if (EnableLogFileFallback)
            {
                message.WriteToJsonFile(logPath, true);
            }
        }


    }
}

