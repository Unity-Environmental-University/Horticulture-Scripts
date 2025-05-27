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
            if (!overlayCamera) overlayCamera = GameObject.FindWithTag($"noPostCamera");

            // If iPadOS, then let's adjust the view to be better.
            if (!SystemInfo.deviceModel.Contains("iPad")) return;
            cam.fieldOfView = 95;
            transform.rotation = Quaternion.Euler(13f, -180f, 0f);
        }
    }
}