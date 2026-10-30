using UnityEngine;

namespace GyroPilot.UI
{
    public static class SettingsManager
    {
        private const string PREF_QUALITY = "GyroPilot_QualityLevel";
        private const string PREF_VOLUME = "GyroPilot_MasterVolume";
        private const string PREF_INVERT_STEER = "GyroPilot_InvertSteer";

        private static bool isInitialized = false;

        public static void Initialize()
        {
            if (isInitialized) return;

            // Cargar calidad gráfica guardada
            int defaultQuality = QualitySettings.GetQualityLevel();
            int savedQuality = PlayerPrefs.GetInt(PREF_QUALITY, defaultQuality);
            savedQuality = Mathf.Clamp(savedQuality, 0, QualitySettings.count - 1);
            QualitySettings.SetQualityLevel(savedQuality, true);

            // Cargar volumen de sonido
            float savedVolume = PlayerPrefs.GetFloat(PREF_VOLUME, 1.0f);
            AudioListener.volume = Mathf.Clamp01(savedVolume);

            isInitialized = true;
        }

        public static int GetQualityLevel()
        {
            return QualitySettings.GetQualityLevel();
        }

        public static void SetQualityLevel(int level)
        {
            level = Mathf.Clamp(level, 0, QualitySettings.count - 1);
            QualitySettings.SetQualityLevel(level, true);
            PlayerPrefs.SetInt(PREF_QUALITY, level);
            PlayerPrefs.Save();
        }

        public static float GetMasterVolume()
        {
            return AudioListener.volume;
        }

        public static void SetMasterVolume(float volume)
        {
            volume = Mathf.Clamp01(volume);
            AudioListener.volume = volume;
            PlayerPrefs.SetFloat(PREF_VOLUME, volume);
            PlayerPrefs.Save();
        }

        public static bool GetInvertSteering(bool defaultVal = true)
        {
            return PlayerPrefs.GetInt(PREF_INVERT_STEER, defaultVal ? 1 : 0) == 1;
        }

        public static void SetInvertSteering(bool invert)
        {
            PlayerPrefs.SetInt(PREF_INVERT_STEER, invert ? 1 : 0);
            PlayerPrefs.Save();
        }
    }
}
