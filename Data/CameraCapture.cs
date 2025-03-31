using System;
using System.Collections;
using System.IO;
using System.Threading.Tasks;
using _project.Scripts.Core;
using _project.Scripts.UI;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace _project.Scripts.Data
{
    public class CameraCapture : MonoBehaviour
    {
        [SerializeField] private Camera screenshotCamera;
        [SerializeField] private FPSController fpsController;
        [SerializeField] private MenuManager menuManager;
        [SerializeField] private GameObject camText;
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private ScriptedRobotManager robotManager;

        public bool debugging;

        private bool _holdingRight;
        private bool _firstCapture;
        private InputAction _leftClickAction;
        private InputAction _rightClickAction;
        private string _screenshotsDir;
        private InputAction _cameraCapAction;

        private void Awake()
        {
#if !UNITY_EDITOR
            debugging = false;
#endif 
            _rightClickAction = inputActions.FindAction("RightClick");
            _leftClickAction = inputActions.FindAction("LeftClick");
            _cameraCapAction = inputActions.FindAction("CameraCapture");

            if (debugging) Debug.Log("Input actions initialized.");

            _rightClickAction.performed += OnRightClickPerformed;
            _rightClickAction.canceled += OnRightClickCanceled;
            _leftClickAction.performed += OnLeftClickPerformed;
            _cameraCapAction.performed += OnCameraCapturePerformed;

            // Initialize the screenshots directory
            _screenshotsDir = Path.Combine(Application.persistentDataPath, "screenshots");
            if (!Directory.Exists(_screenshotsDir)) Directory.CreateDirectory(_screenshotsDir);
        }

        private void OnEnable()
        {
            _rightClickAction.Enable();
            _leftClickAction.Enable();
            _cameraCapAction.Enable();
        }

        private void OnDisable()
        {
            _rightClickAction.performed -= OnRightClickPerformed;
            _rightClickAction.canceled -= OnRightClickCanceled;
            _leftClickAction.performed -= OnLeftClickPerformed;
            _cameraCapAction.performed -= OnCameraCapturePerformed;

            _rightClickAction.Disable();
            _leftClickAction.Disable();
            _cameraCapAction.Disable();
        }

        private void OnRightClickPerformed(InputAction.CallbackContext context)
        {
            _holdingRight = true;
        }

        private void OnRightClickCanceled(InputAction.CallbackContext context)
        {
            _holdingRight = false;
        }

        private void OnLeftClickPerformed(InputAction.CallbackContext context)
        {
            if (_holdingRight)
                StartCoroutine(CaptureScreenshotWithLock());
        }

        private void OnCameraCapturePerformed(InputAction.CallbackContext context)
        {
            if (menuManager.isPaused) return;
            StartCoroutine(CaptureScreenshotWithLock());
        }

        private IEnumerator CaptureScreenshotWithLock()
        {
            if (fpsController)
                fpsController.lookLocked = true;

            if(!_firstCapture) {robotManager.SetFlag(ScriptFlags.TookPicture); _firstCapture = true; }
        
            var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            var filename = $"Screenshot_{timestamp}.png";
            var path = Path.Combine(_screenshotsDir, filename);

            // ReSharper disable once Unity.PerformanceCriticalCodeInvocation
            yield return CaptureScreenshot(path);

            if (fpsController)
                fpsController.lookLocked = false;

            if (!camText) yield break;
            // ReSharper disable once Unity.PerformanceCriticalCodeInvocation
            var textComponent = camText.GetComponent<TextMeshProUGUI>();
            if (!textComponent) yield break;
            textComponent.text = "Screenshot Captured!";
            camText.SetActive(true);
        }

        private IEnumerator CaptureScreenshot(string path)
        {
            var width = Screen.width;
            var height = Screen.height;

            var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            var screenshot = new Texture2D(width, height, TextureFormat.ARGB32, false);

            screenshotCamera.targetTexture = rt;
            screenshotCamera.Render();

            RenderTexture.active = rt;
            screenshot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            screenshot.Apply();

            screenshotCamera.targetTexture = null;
            RenderTexture.active = null;
            rt.Release();
            Destroy(rt);

            // ReSharper disable once Unity.PerformanceCriticalCodeInvocation
            SaveImageAsync(screenshot, path);

            yield return null;
        }

        private async void SaveImageAsync(Texture2D texture, string path)
        {
            try
            {
                const int retryCount = 3; // Number of retry attempts
                const int delayBetweenRetries = 100; // Delay in milliseconds between retries
                var success = false;

                try
                {
                    var bytes = texture.EncodeToPNG();

                    for (var attempt = 0; attempt < retryCount; attempt++)
                        try
                        {
                            // Ensure unique filename
                            if (File.Exists(path))
                            {
                                var timestamp =
                                    DateTime.Now.ToString("yyyyMMddHHmmssfff"); // Add milliseconds for uniqueness
                                path = Path.Combine(Path.GetDirectoryName(path) ?? string.Empty,
                                    $"Screenshot_{timestamp}.png");
                            }

                            // Attempt to save the file
                            await using var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None,
                                4096,
                                true);
                            await fs.WriteAsync(bytes, 0, bytes.Length);

                            success = true;
                            // ReSharper disable once Unity.PerformanceCriticalCodeInvocation
                            if (debugging) Debug.Log($"Screenshot saved successfully at: {path}");
                            break; // Exit loop if successful
                        }
                        catch (IOException ioEx) when (ioEx.Message.Contains("Sharing violation"))
                        {
                            // ReSharper disable once Unity.PerformanceCriticalCodeInvocation
                            Debug.LogWarning($"Sharing violation on path {path}. Retrying in {delayBetweenRetries}ms...");
                            await Task.Delay(delayBetweenRetries); // Wait before retrying
                        }

                    if (!success)
                        // ReSharper disable once Unity.PerformanceCriticalCodeInvocation
                        Debug.LogError($"Failed to save screenshot after {retryCount} attempts due to sharing violation.");
                }
                catch (Exception ex)
                {
                    // ReSharper disable once Unity.PerformanceCriticalCodeInvocation
                    Debug.LogError($"Failed to save screenshot asynchronously: {ex.Message}");
                }
                finally
                {
                    Destroy(texture);
                }
            }
            catch (Exception e)
            {
                // ReSharper disable once Unity.PerformanceCriticalCodeInvocation
                Debug.LogError($"Failed to save screenshot at {path}: {e.Message}");
            }
        }
    }
}