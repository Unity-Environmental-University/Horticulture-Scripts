using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using _project.Scripts.Core;
using _project.Scripts.Data;
using UnityEngine;
using UnityEngine.UI;

namespace _project.Scripts.UI
{
    public class GalleryManager : MonoBehaviour
    {
        public GameObject imagePrefab;
        public Transform content;
        public GameObject largeImagePanel;
        public Image largeImage;
        public Sprite placeholderSprite;
        public bool openedFromNotebook;
        public bool debugging;

        [SerializeField] private CameraCapture cameraCapture;
        [SerializeField] private InspectObject inspectObject;
        [SerializeField] private ScriptedRobotManager robotManager;
        [SerializeField] private GameObject bufferPanel;
        [SerializeField] private GameObject closeButton;
    
        private const float TimeBudgetPerFrame = 0.01f; // 10 milliseconds per frame
        private readonly Dictionary<string, Texture2D> _fullResCache = new(); // Cache for full-res images
        private readonly List<GameObject> _imagePool = new();
        private readonly List<string> _screenshotPaths = new();
        private readonly Dictionary<string, Texture2D> _thumbnailCache = new(); // Cache for thumbnails
    
        private string _lastLargeScreenshotPath;
        private int _lastScreenshotCount;
        private string _screenshotsDir;
        private bool _firstOpen;

        private void Awake()
        {
            _screenshotsDir = Path.Combine(Application.persistentDataPath, "screenshots");
            if (!Directory.Exists(_screenshotsDir)) Directory.CreateDirectory(_screenshotsDir);
        }

        private void Start()
        {
#if !UNITY_EDITOR
        debugging = false;
#endif
        }

        private void Update()
        {
            // Periodically check for new images
            if (CheckForNewImages())
                // Refresh the gallery if new images are added
                // ReSharper disable once Unity.PerformanceCriticalCodeInvocation
                StartCoroutine(DisplayImagesCoroutine());
        }

        private void OnEnable()
        {
            if (!_firstOpen){ robotManager.SetFlag(ScriptFlags.OpenedGallery); _firstOpen = true;}
            Cursor.visible = true;
            inspectObject?.ToggleSearch(false);
            bufferPanel.SetActive(true);
            StartCoroutine(DisplayImagesCoroutine());
        }

        private void OnDisable()
        {
            if (!openedFromNotebook)
            {
                Cursor.visible = false;
                inspectObject?.ToggleSearch(true);
            }

            bufferPanel.SetActive(false);
            openedFromNotebook = false;
        }

        public event Action<string> OnImageSelected;

        private bool CheckForNewImages()
        {
            var files = Directory.GetFiles(_screenshotsDir, "*.png");

            // Filter out thumbnails
            var validFiles = files.Where(file => !file.EndsWith("_thumbnail.png")).ToList();

            // If new images are detected, update the screenshot paths
            if (validFiles.Count == _lastScreenshotCount) return false;
            _screenshotPaths.Clear();
            _screenshotPaths.AddRange(validFiles);
            _lastScreenshotCount = validFiles.Count;
            return true;
        }


        private IEnumerator DisplayImagesCoroutine()
        {
            // Ensure the pool has enough GameObjects
            for (var i = _imagePool.Count; i < _screenshotPaths.Count; i++)
            {
                var newImage = Instantiate(imagePrefab, content);
                _imagePool.Add(newImage);
                newImage.SetActive(false); // Initially inactive
            }

            var loadedCount = 0;
            var startTime = Time.realtimeSinceStartup;
            const int batchLimit = 5; // Limit the number of images processed per frame

            for (var i = 0; i < _imagePool.Count; i++)
                if (i < _screenshotPaths.Count)
                {
                    var imageGo = _imagePool[i];
                    imageGo.SetActive(true);
                    // ReSharper disable twice Unity.PerformanceCriticalCodeInvocation
                    var imageComponent = imageGo.GetComponent<Image>();
                    var buttonComponent = imageGo.GetComponent<Button>();

                    // Assign placeholder sprite
                    imageComponent.sprite = placeholderSprite;

                    // Load and assign the thumbnail image using Coroutine with cache support
                    yield return StartCoroutine(LoadThumbnailCoroutine(_screenshotPaths[i], imageComponent));

                    // Update button listener to display the full-resolution image when clicked
                    buttonComponent.onClick.RemoveAllListeners();
                    var i1 = i;
                    if (openedFromNotebook)
                        // ReSharper disable once Unity.PerformanceCriticalCodeInvocation
                        buttonComponent.onClick.AddListener(() => SelectImage(_screenshotPaths[i1]));
                    else
                        buttonComponent.onClick.AddListener(() => ShowLargeImage(_screenshotPaths[i1]));

                    loadedCount++;

                    // Check the time budget for this frame and batch limit
                    if (loadedCount < batchLimit &&
                        !(Time.realtimeSinceStartup - startTime >= TimeBudgetPerFrame)) continue;
                    startTime = Time.realtimeSinceStartup;
                    loadedCount = 0;
                    yield return null; // Wait until the next frame
                }
                else if (i < _imagePool.Count)
                {
                    _imagePool[i].SetActive(false);
                }
        }

        private IEnumerator LoadThumbnailCoroutine(string path, Image imageComponent)
        {
            var thumbnailPath = Path.Combine(Path.GetDirectoryName(path) ?? string.Empty,
                Path.GetFileNameWithoutExtension(path) + "_thumbnail.png");

            if (_thumbnailCache.TryGetValue(path, out var cachedThumbnail))
            {
                // Use cached thumbnail if available
                imageComponent.sprite = Sprite.Create(
                    cachedThumbnail,
                    new Rect(0, 0, cachedThumbnail.width, cachedThumbnail.height),
                    new Vector2(0.5f, 0.5f)
                );
            }
            else if (File.Exists(thumbnailPath))
            {
                // Load the thumbnail directly from the disk
                var bytes = File.ReadAllBytes(thumbnailPath);
                var texture = new Texture2D(2, 2);
                texture.LoadImage(bytes);

                _thumbnailCache[path] = texture; // Cache the thumbnail

                imageComponent.sprite = Sprite.Create(
                    texture,
                    new Rect(0, 0, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f)
                );
            }
            else
            {
                // Thumbnail does not exist, create it
                yield return StartCoroutine(CreateAndSaveThumbnail(path, thumbnailPath, imageComponent));
            }

            yield return null;
        }

        private IEnumerator CreateAndSaveThumbnail(string originalPath, string thumbnailPath, Image imageComponent)
        {
            // Load full resolution image
            var bytes = File.ReadAllBytes(originalPath);
            var fullResTexture = new Texture2D(2, 2);
            fullResTexture.LoadImage(bytes);

            // Create a low-resolution thumbnail
            var thumbnailTexture = ScaleTexture(fullResTexture, fullResTexture.width / 4, fullResTexture.height / 4);

            // Save the thumbnail to disk
            File.WriteAllBytes(thumbnailPath, thumbnailTexture.EncodeToPNG());

            // Cache the thumbnail
            _thumbnailCache[originalPath] = thumbnailTexture;

            imageComponent.sprite = Sprite.Create(
                thumbnailTexture,
                new Rect(0, 0, thumbnailTexture.width, thumbnailTexture.height),
                new Vector2(0.5f, 0.5f)
            );

            // Cache the full-resolution texture
            _fullResCache.TryAdd(originalPath, fullResTexture);

            yield return null;
        }

        private static Texture2D ScaleTexture(Texture2D source, int targetWidth, int targetHeight)
        {
            var result = new Texture2D(targetWidth, targetHeight, source.format, false);
            for (var y = 0; y < targetHeight; y++)
            for (var x = 0; x < targetWidth; x++)
            {
                var newColor = source.GetPixelBilinear((float)x / targetWidth, (float)y / targetHeight);
                result.SetPixel(x, y, newColor);
            }

            result.Apply();
            return result;
        }

        private void SelectImage(string path)
        {
            // Ensure the full-resolution image is shown for the selected image
            if (_fullResCache.TryGetValue(path, out var fullResTexture))
                largeImage.sprite = Sprite.Create(
                    fullResTexture,
                    new Rect(0, 0, fullResTexture.width, fullResTexture.height),
                    new Vector2(0.5f, 0.5f)
                );
            else
                StartCoroutine(LoadFullResolutionImageCoroutine(path));

            OnImageSelected?.Invoke(path);
            gameObject.SetActive(false);
            Cursor.visible = true;
        }

        private void ShowLargeImage(string path)
        {
            StartCoroutine(LoadFullResolutionImageCoroutine(path));
            largeImagePanel.SetActive(true);
            _lastLargeScreenshotPath = path;
        }

        private IEnumerator LoadFullResolutionImageCoroutine(string path)
        {
            if (_fullResCache.TryGetValue(path, out var cachedFullRes))
            {
                // Use cached full-resolution texture
                largeImage.sprite = Sprite.Create(
                    cachedFullRes,
                    new Rect(0, 0, cachedFullRes.width, cachedFullRes.height),
                    new Vector2(0.5f, 0.5f)
                );
            }
            else
            {
                // Load full-resolution image from disk
                var bytes = File.ReadAllBytes(path);
                var fullResTexture = new Texture2D(2, 2);
                fullResTexture.LoadImage(bytes);

                _fullResCache[path] = fullResTexture; // Cache the texture

                largeImage.sprite = Sprite.Create(
                    fullResTexture,
                    new Rect(0, 0, fullResTexture.width, fullResTexture.height),
                    new Vector2(0.5f, 0.5f)
                );
            }

            yield return null;
        }

        public void CloseLargeImage()
        {
            largeImage.sprite = null;
            largeImagePanel.SetActive(false);
            _lastLargeScreenshotPath = null;
        }

        public void DeleteImage()
        {
            if (string.IsNullOrEmpty(_lastLargeScreenshotPath) || !File.Exists(_lastLargeScreenshotPath))
            {
                if (debugging) Debug.LogError("No image selected for deletion, or image file does not exist.");
                return;
            }

            try
            {
                File.Delete(_lastLargeScreenshotPath);

                // Construct the thumbnail path
                var thumbnailPath = Path.Combine(
                    Path.GetDirectoryName(_lastLargeScreenshotPath) ?? string.Empty,
                    Path.GetFileNameWithoutExtension(_lastLargeScreenshotPath) + "_thumbnail.png");

                if (File.Exists(thumbnailPath)) File.Delete(thumbnailPath);

                _lastLargeScreenshotPath = null;
            }
            catch (Exception exception)
            {
                if (debugging) Debug.LogError($"Failed to delete image: {exception.Message}");
            }

            _lastLargeScreenshotPath = null;
            CloseLargeImage();
        }

        public void ExportImage()
        {
            if (string.IsNullOrEmpty(_lastLargeScreenshotPath) || !File.Exists(_lastLargeScreenshotPath))
            {
                if (debugging) Debug.LogError("No image selected for export, or image file does not exist.");
                return;
            }

            try
            {
            }
            catch (Exception exception)
            {
                if (debugging) Debug.LogError($"Failed to export image: {exception.Message}");
            }
        }
    }
}