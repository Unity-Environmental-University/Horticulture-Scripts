using UnityEngine;

namespace _project.Scripts.UI
{
    [RequireComponent(typeof(Camera))]
    public class CameraTweaks : MonoBehaviour
    {
        private Camera cam;
        private const float TargetAspectRatio = 16f / 9f;
        private const float MobileFov = 68f;
        private const string OverlayCameraTag = "noPostCamera";

        [Tooltip("Toggles mobile camera FOV adjustment")] [SerializeField]
        private bool adjustMobileFov = true;
        
        // Cached screen dimensions for change detection
        private int lastScreenHeight;
        private int lastScreenWidth;

        [Tooltip("Camera used for rendering elements without post-processing effects. Auto-found if not assigned.")]
        public GameObject overlayCamera;

        private void Start()
        {
            cam = GetComponent<Camera>();

            // Cache initial screen dimensions
            lastScreenWidth = Screen.width;
            lastScreenHeight = Screen.height;

            ApplyAspectRatioCorrection();

            if (!overlayCamera)
                overlayCamera = GameObject.FindWithTag(OverlayCameraTag);

            ApplyPlatformFovAdjustments();
        }

        private void Update()
        {
            // Only recalculate when screen dimensions actually change
            // This handles window resizing on desktop and orientation changes on mobile
            if (Screen.width == lastScreenWidth && Screen.height == lastScreenHeight) return;
            lastScreenWidth = Screen.width;
            lastScreenHeight = Screen.height;
            ApplyAspectRatioCorrection();
        }

        /// <summary>
        ///     Applies platform-specific FOV adjustments to ensure UI elements fit on screen.
        ///     Mobile devices have varying aspect ratios that can crop UI designed for 16:9.
        /// </summary>
        private void ApplyPlatformFovAdjustments()
        {
            if (!Application.isMobilePlatform || !adjustMobileFov) return;
            cam.fieldOfView = MobileFov;
        }

        /// <summary>
        ///     Applies letterboxing (black bars top/bottom) or pillarboxing (black bars left/right)
        ///     to maintain the target aspect ratio regardless of actual screen dimensions.
        /// </summary>
        private void ApplyAspectRatioCorrection()
        {
            var windowAspect = (float)Screen.width / Screen.height;
            var scaleHeight = windowAspect / TargetAspectRatio;

            var rect = cam.rect;

            if (scaleHeight < 1.0f)
            {
                // Screen is narrower than 16:9 - apply letterboxing (horizontal bars)
                // Scale viewport height down and center vertically
                rect.width = 1.0f;
                rect.height = scaleHeight;
                rect.x = 0;
                rect.y = (1.0f - scaleHeight) / 2.0f;
            }
            else
            {
                // Screen is wider than 16:9 - apply pillarboxing (vertical bars)
                // Scale viewport width down and center horizontally
                var scaleWidth = 1.0f / scaleHeight;
                rect.width = scaleWidth;
                rect.height = 1.0f;
                rect.x = (1.0f - scaleWidth) / 2.0f;
                rect.y = 0;
            }

            cam.rect = rect;
        }
    }
}