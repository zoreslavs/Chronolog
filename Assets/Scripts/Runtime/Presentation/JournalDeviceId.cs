using UnityEngine;
using System;

namespace Chronolog.Presentation
{
    public static class JournalDeviceId
    {
        private const string EditorDeviceIdKey = "Chronolog.EditorDeviceId";

        public static string Get()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            using var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            using var contentResolver = activity.Call<AndroidJavaObject>("getContentResolver");
            using var settingsSecure = new AndroidJavaClass("android.provider.Settings$Secure");
            var deviceId = settingsSecure.CallStatic<string>("getString", contentResolver, "android_id");

            if (string.IsNullOrWhiteSpace(deviceId))
                throw new InvalidOperationException("Android device ID is unavailable.");

            return deviceId;
#else
            var deviceId = PlayerPrefs.GetString(EditorDeviceIdKey);
            if (!string.IsNullOrWhiteSpace(deviceId))
                return deviceId;

            deviceId = $"editor-{Guid.NewGuid():N}";
            PlayerPrefs.SetString(EditorDeviceIdKey, deviceId);
            PlayerPrefs.Save();
            return deviceId;
#endif
        }
    }
}