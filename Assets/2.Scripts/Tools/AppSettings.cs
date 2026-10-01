using System;
using UnityEngine;


namespace DT
{
    public class AppSettings : Singleton<AppSettings>
    {
        [SerializeField]
        private int framerate = 60;
        [SerializeField]
        private int quality = 3;


        void Start()
        {
            framerate = PlayerPrefs.GetInt("framerate", framerate);
            quality = PlayerPrefs.GetInt("quality", quality);

            QualitySettings.SetQualityLevel(quality);
            Application.targetFrameRate = framerate;
        }


        public void ChangeQuality(int quality)
        {
            this.quality = quality;
            QualitySettings.SetQualityLevel(quality, true);
            PlayerPrefs.SetInt("quality", quality);
        }
        public void ChangeFramerate(float frequency)
        {
            framerate = Convert.ToInt32(frequency);
            Application.targetFrameRate = framerate;
            PlayerPrefs.SetInt("framerate", framerate);
        }


    }

}
