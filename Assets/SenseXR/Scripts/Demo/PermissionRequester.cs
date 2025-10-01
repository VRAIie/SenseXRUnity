using UnityEngine;

public class PermissionRequester : MonoBehaviour
{
    void Start()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            // Determine Android API level at runtime
            int sdkInt = 0;
            using (var version = new AndroidJavaClass("android.os.Build$VERSION"))
            {
                sdkInt = version.GetStatic<int>("SDK_INT");
            }

            var permissionsToRequest = new List<string>();

            if (sdkInt >= 33)
            {
                Debug.Log($"[PermissionRequester] Android SDK_INT={sdkInt}. Note: WRITE_EXTERNAL_STORAGE is not a runtime permission on Android 13+ and any checks for it will return false/denied. Use READ_MEDIA_* and/or MANAGE_EXTERNAL_STORAGE where appropriate.");

                // Android 13+ uses the new media permissions. The old READ/WRITE_EXTERNAL_STORAGE are ignored.
                permissionsToRequest.Add("android.permission.READ_MEDIA_IMAGES");
                permissionsToRequest.Add("android.permission.READ_MEDIA_VIDEO");
                permissionsToRequest.Add("android.permission.READ_MEDIA_AUDIO");

                // Nearby Wi‑Fi devices is a runtime permission on Android 13+
                permissionsToRequest.Add("android.permission.NEARBY_WIFI_DEVICES");
            }
            else
            {
                // Pre-Android 13 devices still use the legacy external storage permissions
                permissionsToRequest.Add("android.permission.READ_EXTERNAL_STORAGE");
                permissionsToRequest.Add("android.permission.WRITE_EXTERNAL_STORAGE");

                // Many Wi‑Fi scan/use-cases need location on pre-13 Android
                // (Android 10-12 require ACCESS_FINE_LOCATION for Wi‑Fi scans and connections)
                permissionsToRequest.Add("android.permission.ACCESS_FINE_LOCATION");
            }

            foreach (var p in permissionsToRequest)
            {
                if (!Permission.HasUserAuthorizedPermission(p))
                {
                    Debug.Log($"[PermissionRequester] Requesting permission: {p}");
                    Permission.RequestUserPermission(p);
                }
                else
                {
                    Debug.Log($"[PermissionRequester] Already granted: {p}");
                }
            }

            // Special case: MANAGE_EXTERNAL_STORAGE (All files access) cannot be requested via standard runtime dialog.
            // If your app truly requires it on Android 11+ (API 30+), you must navigate the user to the system settings page.
            if (sdkInt >= 30)
            {
                bool hasAllFilesAccess = false;
                try
                {
                    using (var env = new AndroidJavaClass("android.os.Environment"))
                    {
                        hasAllFilesAccess = env.CallStatic<bool>("isExternalStorageManager");
                    }
                }
                catch { /* API < 30 won't reach here */ }

                if (!hasAllFilesAccess)
                {
                    Debug.Log("[PermissionRequester] MANAGE_EXTERNAL_STORAGE (All files access) currently NOT granted.");
                    Debug.Log("[PermissionRequester] Opening system Settings page to let the user grant All Files Access for this app.");
                    OpenAllFilesAccessSettings();
                }
                else
                {
                    Debug.Log("[PermissionRequester] MANAGE_EXTERNAL_STORAGE (All files access) already granted.");
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[PermissionRequester] Exception while requesting permissions: {e}");
        }
#endif
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    private void OpenAllFilesAccessSettings()
    {
        try
        {
            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            using (var intent = new AndroidJavaObject("android.content.Intent"))
            {
                intent.Call<AndroidJavaObject>("setAction", "android.settings.MANAGE_APP_ALL_FILES_ACCESS_PERMISSION");

                // Build the package: URI for this app
                using (var uriClass = new AndroidJavaClass("android.net.Uri"))
                using (var uri = uriClass.CallStatic<AndroidJavaObject>("parse", "package:" + Application.identifier))
                {
                    intent.Call<AndroidJavaObject>("setData", uri);
                }

                activity.Call("startActivity", intent);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[PermissionRequester] Could not open All Files Access settings: {e.Message}");
        }
    }
#endif
}