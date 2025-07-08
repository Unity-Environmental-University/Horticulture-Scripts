using UnityEngine;

namespace _project.Scripts.UI
{
    public class CameraTweaks : MonoBehaviour
    {
        public GameObject overlayCamera;
        private Camera cam;

        private void Start()
        {
            // Get the Camera
            cam = GetComponent<Camera>();
            if (!overlayCamera) overlayCamera = GameObject.FindWithTag("noPostCamera");

            if (Application.isMobilePlatform)
                cam.fieldOfView =
                    68; // Catch-all for Android devices to adjust the camera FOV to fit in all UI elements
            if (SystemInfo.deviceModel
                .Contains("iPad")) // If iPadOS, we adjust the camera FOV to fit in all UI elements
            {
                cam.fieldOfView = 95;
                transform.rotation = Quaternion.Euler(13f, -180f, 0f);
            }
            else if
                (SystemInfo.deviceModel
                 .Contains("iPhone")) // If iPhone//iOS, we adjust the camera FOV to fit in all UI elements
            {
                cam.fieldOfView = 68;
            }
        }
    }
}