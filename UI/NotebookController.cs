using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using _project.Scripts.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

namespace _project.Scripts.UI
{
    #region Classes

    [Serializable]
    public class FieldNoteCollection
    {
        // public string plant;
        public GameObject pageObject;
        public List<FieldNoteDay> days = new();

        public List<FieldNoteDay> AllDays()
        {
            var output = new List<FieldNoteDay>();
            output.AddRange(days);
            return output;
        }
    }

    [Serializable]
    public class FieldNoteDay
    {
        public string day;
        public List<InputFieldData> notes = new();
        public List<ActionToggles> actions = new();
        public List<ImageFieldData> images = new();

        public List<InputFieldData> AllNotes()
        {
            var output = new List<InputFieldData>();
            output.AddRange(notes);
            return output;
        }

        public List<ImageFieldData> AllImages()
        {
            var output = new List<ImageFieldData>();
            output.AddRange(images);
            return output;
        }
    }

    [Serializable]
    public class ActionToggles
    {
        public GameObject neemOil;
        public GameObject fungicide;
        public GameObject insecticide;
        public GameObject soapyWater;

        public List<GameObject> GetToggles()
        {
            return new List<GameObject> { neemOil, fungicide, insecticide, soapyWater };
        }
    }

    [Serializable]
    public class InputFieldData
    {
        public string keyword;
        public string fileName;
        public TextMeshProUGUI inputField;
    }

    [Serializable]
    public class ImageFieldData
    {
        public string keyword;
        public string attachedImagePath;
        public Button attachImageButton;
        public Image attachedImagePreview;
    }

    #endregion

    public class NotebookController : MonoBehaviour
    {
        private const string TemplateFileName = "HTML-TEMPLATE";
        private const string OutputFileName = "Notebook.html";

        [SerializeField] private InspectObject inspectObject;
        [SerializeField] private GalleryManager galleryManager;
        [SerializeField] private ScriptedRobotManager robotManager;
        [SerializeField] private GameObject pageBackButton;
        [SerializeField] private GameObject pageNextButton;
        [SerializeField] private bool debugging;

        public List<FieldNoteCollection> fieldNotesPages = new() { new FieldNoteCollection() };

        private ImageFieldData _currentImageField;
        private int _currentPageIndex;
        private string _notebooksPath;
        private Texture2D _currentCursor;
        private IEnumerable<ImageFieldData> _allImages;
        private IEnumerable<InputFieldData> _allNotes;

        //public IEnumerable<GameObject> ActionObjects;
        public List<TreatmentTable> treatmentTables = new();

        private void Awake()
        {
            _allImages = fieldNotesPages
                .SelectMany(page => page.days)
                .SelectMany(day => day.AllImages());

            _allNotes = fieldNotesPages
                .SelectMany(page => page.days)
                .SelectMany(day => day.AllNotes());
            _notebooksPath = Path.Combine(Application.persistentDataPath, "notes");

            /*ActionObjects = fieldNotesPages.SelectMany(page => page.days)
            .SelectMany(day => day.actions.SelectMany(action => action.GetToggles()));*/

            for (var i = 0; i < fieldNotesPages.Count; i++)
                if (fieldNotesPages[i].pageObject)
                    fieldNotesPages[i].pageObject.SetActive(i == _currentPageIndex);

            if (!Directory.Exists(_notebooksPath)) Directory.CreateDirectory(_notebooksPath);
            if (_currentPageIndex == 0) pageBackButton.SetActive(false);
        }

        private void Start()
        {
#if !UNITY_EDITOR
        debugging = false;
#endif
        }

        private void OnEnable()
        {
            Cursor.visible = true;
            if (inspectObject) inspectObject.ToggleSearch(false);
            foreach (var imageField in _allImages)
            {
                imageField.attachImageButton.onClick.AddListener(() => OpenGallery(imageField));
                if (!string.IsNullOrEmpty(imageField.attachedImagePath) && !File.Exists(imageField.attachedImagePath))
                    imageField.attachedImagePreview.sprite = null;
            }
        }

        private void OnDisable()
        {
            Cursor.visible = false;
            if (inspectObject) inspectObject.ToggleSearch(true);

            if (!robotManager.CheckFlag(ScriptFlags.OpenedNotebook)) robotManager.SetFlag(ScriptFlags.OpenedNotebook);

            foreach (var imageField in _allImages)
                imageField.attachImageButton.onClick.RemoveListener(() => OpenGallery(imageField));
        }

        public void Save()
        {
            var htmlTemplate = Resources.Load<TextAsset>(TemplateFileName);

            if (htmlTemplate)
            {
                var outputFilePath = Path.Combine(_notebooksPath, OutputFileName);
                if (debugging) Debug.Log($"Output file path: {outputFilePath}");

                // Trim and Debug template content
                var templateText = htmlTemplate.text.Trim();
                if (debugging) Debug.Log($"Template Content:\n{templateText}");

                // Create a dictionary to hold keyword replacements
                var replacements = new Dictionary<string, string>();

                foreach (var note in _allNotes)
                {
                    var filePath = Path.Combine(_notebooksPath, note.fileName);
                    var inputText = note.inputField?.text?.Trim() ?? "";

                    if (string.IsNullOrEmpty(inputText)) continue;

                    try
                    {
                        File.WriteAllText(filePath, inputText);
                        replacements[note.keyword] = inputText;
                        if (debugging) Debug.Log($"Input Text for {note.keyword}:\n{inputText}");
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Failed to write {note.keyword} to file {filePath}: {e.Message}");
                    }
                }

                foreach (var imageField in _allImages)
                    if (!string.IsNullOrEmpty(imageField.attachedImagePath))
                    {
                        var imageTag = $"file://{imageField.attachedImagePath.Replace("\\", "/")}";
                        replacements[imageField.keyword] = imageTag;
                        if (debugging) Debug.Log($"Image path for {imageField.keyword}: {imageTag}");
                    }
                    else
                    {
                        replacements[imageField.keyword] = ""; // Leave empty if no image attached
                    }

                // Perform replacements
                var modifiedHtml = templateText;

                var allKeywordsFound = true;

                foreach (var kvp in replacements)
                    if (modifiedHtml.Contains(kvp.Key))
                    {
                        modifiedHtml = modifiedHtml.Replace(kvp.Key, kvp.Value);
                    }
                    else
                    {
                        if (debugging) Debug.LogError($"Keyword {kvp.Key} not found in the template.");
                        allKeywordsFound = false;
                    }

                if (allKeywordsFound)
                {
                    if (debugging) Debug.Log("All replacements successful. Saving modified HTML...");

                    // Save the modified HTML to the output file
                    File.WriteAllText(outputFilePath, modifiedHtml);
                    if (debugging) Debug.Log($"Modified HTML saved at: {outputFilePath}");

                    // Open the modified HTML file
                    OpenFile(outputFilePath);
                }
                else
                {
                    if (debugging) Debug.LogError("One or more keywords were not found in the template.");
                }
            }
            else
            {
                if (debugging) Debug.LogError("The Template file could not be found.");
            }
        }

        public void SetCursor(Texture2D icon) => Cursor.SetCursor(_currentCursor = icon, Vector2.zero, CursorMode.Auto);
    
        public bool IsCursorSet() => _currentCursor;
    
        public void ClearCursor() => Cursor.SetCursor(_currentCursor = null, Vector2.zero, CursorMode.Auto);

        private void OpenGallery(ImageFieldData imageField)
        {
            _currentImageField = imageField;
            galleryManager.openedFromNotebook = true;
            galleryManager.gameObject.SetActive(true);
            galleryManager.OnImageSelected += AttachSelectedImage;
        }

        private void AttachSelectedImage(string path)
        {
            // Unsubscribe immediately to avoid duplicate calls
            galleryManager.OnImageSelected -= AttachSelectedImage;
            galleryManager.gameObject.SetActive(false);

            // Retry logic: Check if the file exists with a delay
            // ReSharper disable once Unity.PerformanceCriticalCodeInvocation
            StartCoroutine(AttachImageWithRetry(path));
        }

        private IEnumerator AttachImageWithRetry(string path)
        {
            const int maxRetries = 3;
            const float retryDelay = 0.5f; // 500 milliseconds
            var attempt = 0;

            // Try to find the file with a few retries if it doesn't exist immediately
            while (attempt < maxRetries && !File.Exists(path))
            {
                yield return new WaitForSeconds(retryDelay);
                attempt++;
            }

            if (File.Exists(path) && _currentImageField != null)
            {
                // File found, proceed to load and attach it
                _currentImageField.attachedImagePath = path; // Save the path for replacement
                var bytes = File.ReadAllBytes(path);
                var texture = new Texture2D(2, 2);
                texture.LoadImage(bytes);
                _currentImageField.attachedImagePreview.sprite = Sprite.Create(
                    texture,
                    new Rect(0, 0, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f)
                );
                _currentImageField.attachedImagePreview.gameObject.SetActive(true);
                // ReSharper disable once Unity.PerformanceCriticalCodeInvocation
                if (debugging) Debug.Log($"Image attached for keyword {_currentImageField.keyword}: {path}");
            }

            // ReSharper disable once Unity.PerformanceCriticalCodeInvocation
            if (File.Exists(path) && debugging) Debug.Log($"File found on retry: {path}");
            else
                // If the File is still not found after retries
                // ReSharper disable once Unity.PerformanceCriticalCodeInvocation
                Debug.LogError($"Image file not found after retries: {path}");

            // Reset the current image field after attaching the image
            _currentImageField = null;
        }

        public void NextPage()
        {
            Debug.LogFormat("Current Page Index: {0}, Total Pages: {1}", _currentPageIndex, _allNotes.Count());
            if (_currentPageIndex >= _allNotes.Count() - 1) return;
            if (IsCursorSet()) ClearCursor();

            // Deactivate the current Page
            if (fieldNotesPages[_currentPageIndex].pageObject)
                fieldNotesPages[_currentPageIndex].pageObject.SetActive(false);

            _currentPageIndex++;

            // Activate the Next Page
            if (fieldNotesPages[_currentPageIndex].pageObject)
                fieldNotesPages[_currentPageIndex].pageObject.SetActive(true);

            pageBackButton.SetActive(true);
            if (_currentPageIndex == fieldNotesPages.Count - 1)
                pageNextButton.SetActive(false);
        }

        public void PreviousPage()
        {
            if (_currentPageIndex <= 0) return;
            if (IsCursorSet()) ClearCursor();
            // Deactivate the current page
            if (fieldNotesPages[_currentPageIndex].pageObject)
                fieldNotesPages[_currentPageIndex].pageObject.SetActive(false);

            _currentPageIndex--;

            // Activate the Next Page
            if (fieldNotesPages[_currentPageIndex].pageObject)
                fieldNotesPages[_currentPageIndex].pageObject.SetActive(true);

            pageNextButton.SetActive(true);
            if (_currentPageIndex == 0)
                pageBackButton.SetActive(false);
        }

        private void OpenFile(string filePath)
        {
            if (debugging) Debug.Log($"Opening file: {filePath}");
            if (File.Exists(filePath))
            {
#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
            Process.Start("open", $"\"{filePath}\"");
#else
                Application.OpenURL($"file://{filePath}");
#endif
                if (debugging) Debug.Log("File opened successfully.");
            }
            else
            {
                if (debugging) Debug.LogError($"File not found: {filePath}");
            }
        }
    }
}