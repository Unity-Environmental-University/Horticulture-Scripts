using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using _project.Scripts.Card_Core;
using _project.Scripts.Classes;
using _project.Scripts.Core;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace _project.Scripts.Data
{
    /// <summary>
    ///     Fetches plant and affliction description texts from a remote source,
    ///     caches them locally, and falls back to local cache or packaged assets when offline.
    /// </summary>
    public class SiteDataFetcher : MonoBehaviour
    {
        private const string URL = PrivateData.RawGithubContent;
        [SerializeField] private TextMeshProUGUI plantSummary;
        [SerializeField] private TextMeshProUGUI afflictionSummary;
        [SerializeField] private GameObject userInterface;
        [SerializeField] private GameObject uiSignage;
        [SerializeField] private Button exitInspectMode;
        private string affliction;
        private string plantType;

        private void Awake()
        {
            // Skip initial fetch when no plant type has been set (avoids 404 on empty selection)
            if (!string.IsNullOrEmpty(plantType))
                _ = GetPlantTextAsync(URL);
        }

        private void OnEnable()
        {
            if (userInterface != null)
                userInterface.SetActive(false);
            if (uiSignage != null)
                uiSignage.SetActive(false);
            if (exitInspectMode != null)
                exitInspectMode.onClick.AddListener(() =>
                {
                    var master = CardGameMaster.Instance;
                    if (master?.inspectedObj != null)
                        master.inspectedObj.ToggleInspect();
                });
            ToggleUiInput();
        }

        private void OnDisable()
        {
            if (userInterface != null)
                userInterface.SetActive(true);
            if (uiSignage != null)
                uiSignage.SetActive(true);
            if (exitInspectMode != null)
                exitInspectMode.onClick.RemoveAllListeners();
            ToggleUiInput();
        }

        private static void ToggleUiInput()
        {
            var module = CardGameMaster.Instance?.uiInputModule;
            if (module != null)
                module.enabled = !module.enabled;
        }

        public async void SetPlant(PlantType plantTypeEnum)
        {
            try
            {
                plantType = plantTypeEnum.ToString().Replace(" ", "-");
                await GetPlantTextAsync(URL);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        /// <summary>
        ///     Fetch and display description text for a plant affliction.
        /// </summary>
        public async void SetAffliction(PlantAfflictions.IAffliction afflictionObj)
        {
            try
            {
                if (afflictionObj == null)
                {
                    affliction = null;
                    if (afflictionSummary) afflictionSummary.text = string.Empty;
                    return;
                }

                affliction = afflictionObj.Name;
                await GetAfflictionTextAsync(URL);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        /// <summary>
        ///     Coroutine to fetch plant description text from the server.
        /// </summary>
        private async Task GetPlantTextAsync(string webURL)
        {
            var fileKey = UnityWebRequest.EscapeURL(plantType);
            // If a packaged text asset exists in Resources/Descriptions, use it first
            var plantResource = Resources.Load<TextAsset>($"Descriptions/{fileKey}");
            if (plantResource != null)
            {
                Debug.LogWarning($"Loaded plant text from Resources: Descriptions/{fileKey}.txt");
                if (plantSummary) plantSummary.text = plantResource.text;
                return;
            }

            var cacheDir = Path.Combine(Application.persistentDataPath, "Descriptions");
            var localCache = Path.Combine(cacheDir, fileKey + ".txt");
            var remoteUrl = webURL + fileKey + ".txt";
            using var www = UnityWebRequest.Get(remoteUrl);
            await www.SendWebRequest();

            if (www.result is UnityWebRequest.Result.ProtocolError or UnityWebRequest.Result.ConnectionError)
            {
                //Debug.LogWarning($"Remote plant text fetch failed: {www.error} [{remoteUrl}]");
                // Try persistent cache
                if (File.Exists(localCache))
                {
                    Debug.LogWarning($"Loaded plant text from persistent cache: {localCache}");
                    if (plantSummary) plantSummary.text = await File.ReadAllTextAsync(localCache);
                    return;
                }

                // Try packaged fallback
                var streamingFile = Path.Combine(Application.streamingAssetsPath, fileKey + ".txt");
                if (File.Exists(streamingFile))
                {
                    Debug.LogWarning($"Loaded plant text from StreamingAssets: {streamingFile}");
                    if (plantSummary) plantSummary.text = await File.ReadAllTextAsync(streamingFile);
                    return;
                }

                if (plantSummary)
                {
                    Debug.LogError($"Plant text not found locally: {streamingFile}");
                    plantSummary.text = "Error Finding Plant Text";
                }
            }
            else
            {
                Debug.LogWarning($"Loaded plant text from remote: {remoteUrl}");
                // Remotely succeeded: update UI and cache locally
                var text = www.downloadHandler.text;
                if (plantSummary) plantSummary.text = text;
                try
                {
                    Directory.CreateDirectory(cacheDir);
                    await File.WriteAllTextAsync(localCache, text);
                }
                catch (IOException ioe)
                {
                    Debug.LogWarning($"Failed to write plant text to local cache: {ioe.Message}");
                }
            }
        }

        /// <summary>
        ///     Coroutine to fetch affliction description text from the server.
        /// </summary>
        private async Task GetAfflictionTextAsync(string webURL)
        {
            var baseName = affliction.Replace(" ", string.Empty);
            var hyphenName = Regex.Replace(baseName, "(?<!^)([A-Z])", "-$1");
            var fileKey = UnityWebRequest.EscapeURL(hyphenName);
            // If a packaged text asset exists in Resources/Descriptions, use it first
            var affResource = Resources.Load<TextAsset>($"Descriptions/{fileKey}");
            if (affResource != null)
            {
                Debug.LogWarning($"Loaded affliction text from Resources: Descriptions/{fileKey}.txt");
                if (afflictionSummary) afflictionSummary.text = affResource.text;
                return;
            }

            var cacheDir = Path.Combine(Application.persistentDataPath, "Descriptions");
            var localCache = Path.Combine(cacheDir, fileKey + ".txt");
            var remoteUrl = webURL + fileKey + ".txt";
            using var www = UnityWebRequest.Get(remoteUrl);
            await www.SendWebRequest();

            if (www.result is UnityWebRequest.Result.ProtocolError or UnityWebRequest.Result.ConnectionError)
            {
                Debug.LogWarning($"Remote affliction text fetch failed: {www.error} [{remoteUrl}]");
                // Try the persistent cache
                if (File.Exists(localCache))
                {
                    Debug.LogWarning($"Loaded affliction text from persistent cache: {localCache}");
                    if (afflictionSummary) afflictionSummary.text = await File.ReadAllTextAsync(localCache);
                    return;
                }

                // Try packaged fallback
                var streamingFile = Path.Combine(Application.streamingAssetsPath, fileKey + ".txt");
                if (File.Exists(streamingFile))
                {
                    Debug.LogWarning($"Loaded affliction text from StreamingAssets: {streamingFile}");
                    if (afflictionSummary) afflictionSummary.text = await File.ReadAllTextAsync(streamingFile);
                    return;
                }

                if (afflictionSummary)
                {
                    Debug.LogError($"Affliction text not found locally: {streamingFile}");
                    afflictionSummary.text = "Error Finding Affliction Text";
                }
            }
            else
            {
                Debug.LogWarning($"Loaded affliction text from remote: {remoteUrl}");
                // Remotely succeeded: update UI and cache locally
                var text = www.downloadHandler.text;
                if (afflictionSummary) afflictionSummary.text = text;
                try
                {
                    Directory.CreateDirectory(cacheDir);
                    await File.WriteAllTextAsync(localCache, text);
                }
                catch (IOException ioe)
                {
                    Debug.LogWarning($"Failed to write affliction text to local cache: {ioe.Message}");
                }
            }
        }
    }
}
