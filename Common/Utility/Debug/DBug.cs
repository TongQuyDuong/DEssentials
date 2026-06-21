using System;
using UnityEngine;

namespace Dessentials.Common.Utility
{
    public static class DBug
    {
        static DBug()
        {
            m_Enabled = PlayerPrefs.GetInt("EnabledLog", 0) == 1;
        }

        private static bool m_Enabled;

        public static bool Enabled
        {
#if UNITY_EDITOR
            get => true;
#else
            get => m_Enabled;
#endif
            set
            {
                if (m_Enabled == value)
                    return;
                m_Enabled = value;
                PlayerPrefs.SetInt("EnabledLog", value ? 1 : 0);
            }
        }

        public static void Log(string message)
        {
            if (m_Enabled)
                Debug.Log(message);
        }

        public static void LogWarning(string message)
        {
            if (m_Enabled)
                Debug.LogWarning(message);
        }

        public static void LogError(string message)
        {
            if (m_Enabled)
                Debug.LogError(message);
        }

        public static void LogException(Exception e)
        {
            if (m_Enabled)
                Debug.LogException(e);
        }
    }
}