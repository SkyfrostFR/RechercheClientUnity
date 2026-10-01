using DT.Model;
using DT.Service;
using System;
using UnityEngine;

namespace DT
{

    [DefaultExecutionOrder(-1)]
    public class ApplicationManager : Singleton<ApplicationManager>
    {

        // Version number
        [SerializeField]
        public static string version = "2024.1";



        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Init()
        {
            // Service locator init instance
            ServiceLocator.Init();
            // Register new Services 
            ServiceLocator.Instance.Register(new MessageService());


        }


        // Init
        private void Start()
        {


            DatabaseManager.host = "127.0.0.1";
            /*
                    if (XRDevice.isPresent)
                    {
                        XRSettings.LoadDeviceByName("OpenVR");
                    }
                    else
                    {
                        XRSettings.LoadDeviceByName("None");
                    }
            */
            /*QualitySettings.asyncUploadTimeSlice = 1;
            QualitySettings.asyncUploadBufferSize = 16;
            QualitySettings.asyncUploadPersistentBuffer = true;*/
        }

    }
}

