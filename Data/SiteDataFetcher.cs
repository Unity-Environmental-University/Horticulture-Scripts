using System.Collections;
using System.IO;
using System.Text.RegularExpressions;
using _project.Scripts.Classes;
using _project.Scripts.Core;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

namespace _project.Scripts.Data
{
    /// <summary>
    /// Fetches plant and affliction description texts from a remote source,
    /// caches them locally, and falls back to local cache or packaged assets when offline.
    /// </summary>
    public class SiteDataFetcher : MonoBehaviour
    {
        private const string URL = PrivateData.RawGithubContent;
        [SerializeField] private TextMeshProUGUI plantSummary;
        [SerializeField] private TextMeshProUGUI afflictionSummary;
        private string affliction;
        private string plantType;

        private void Awake()
        {
            // Skip initial fetch when no plant type has been set (avoids 404 on empty selection)
            if (!string.IsNullOrEmpty(plantType))
                StartCoroutine(GetPlantText(URL));
        }

        public void SetPlant(PlantType plantTypeEnum)
        {
            plantType = plantTypeEnum.ToString().Replace(" ", "-");
            StartCoroutine(GetPlantText(URL));
        }

        /// <summary>
        ///     Fetch and display description text for a plant affliction.
        /// </summary>
        public void SetAffliction(PlantAfflictions.IAffliction afflictionObj)
        {
            if (afflictionObj == null)
            {
                affliction = null;
                if (afflictionSummary) afflictionSummary.text = string.Empty;
                return;
            }
            affliction = afflictionObj.Name;
            StartCoroutine(GetAfflictionText(URL));
        }

        /// <summary>
        ///     Coroutine to fetch plant description text from the server.
        /// </summary>
        private IEnumerator GetPlantText(string webURL)
        {
            var fileKey = UnityWebRequest.EscapeURL(plantType);
            var remoteUrl = webURL + fileKey + ".txt";
            using var www = UnityWebRequest.Get(remoteUrl);
            yield return www.SendWebRequest();

            if (www.result is UnityWebRequest.Result.ProtocolError or UnityWebRequest.Result.ConnectionError)
            {
                Debug.LogWarning($"Remote plant text fetch failed: {www.error} [{remoteUrl}]");
                // try persistent local copy
                var localPath = Path.Combine(Application.persistentDataPath, fileKey + ".txt");
                if (File.Exists(localPath))
                {
                    var url = "file://" + localPath;
                    using var localReq = UnityWebRequest.Get(url);
                    yield return localReq.SendWebRequest();
                    if (localReq.result == UnityWebRequest.Result.Success)
                    {
                        plantSummary.text = localReq.downloadHandler.text;
                        yield break;
                    }
                }
                // try packaged fallback in StreamingAssets
                var streamingPath = Path.Combine(Application.streamingAssetsPath, fileKey + ".txt");
                using var streamReq = UnityWebRequest.Get("file://" + streamingPath);
                yield return streamReq.SendWebRequest();
                if (streamReq.result == UnityWebRequest.Result.Success)
                {
                    plantSummary.text = streamReq.downloadHandler.text;
                }
                else if (plantSummary)
                {
                    Debug.LogError($"Plant text not found locally: {streamingPath}");
                    plantSummary.text = "Error Finding Plant Text";
                }
            }
            else if (plantSummary)
            {
                // remote succeeded: update UI and cache locally
                var text = www.downloadHandler.text;
                plantSummary.text = text;
                try
                {
                    var localPath = Path.Combine(Application.persistentDataPath, fileKey + ".txt");
                    File.WriteAllText(localPath, text);
                }
                catch (IOException ioe)
                {
                    Debug.LogWarning($"Failed to write plant text to local cache: {ioe.Message}");
                }
            }
        }

        // ReSharper disable Unity.PerformanceAnalysis
        /// <summary>
        ///     Coroutine to fetch affliction description text from the server.
        /// </summary>
        private IEnumerator GetAfflictionText(string webURL)
        {
            var baseName = affliction.Replace(" ", string.Empty);
            var hyphenName = Regex.Replace(baseName, "(?<!^)([A-Z])", "-$1");
            var fileKey = UnityWebRequest.EscapeURL(hyphenName);
            var remoteUrl = webURL + fileKey + ".txt";
            using var www = UnityWebRequest.Get(remoteUrl);
            yield return www.SendWebRequest();

            if (www.result is UnityWebRequest.Result.ProtocolError or UnityWebRequest.Result.ConnectionError)
            {
                Debug.LogWarning($"Remote affliction text fetch failed: {www.error} [{remoteUrl}]");
                // try persistent local copy
                var localPath = Path.Combine(Application.persistentDataPath, fileKey + ".txt");
                if (File.Exists(localPath))
                {
                    var url = "file://" + localPath;
                    using var localReq = UnityWebRequest.Get(url);
                    yield return localReq.SendWebRequest();
                    if (localReq.result == UnityWebRequest.Result.Success)
                    {
                        afflictionSummary.text = localReq.downloadHandler.text;
                        yield break;
                    }
                }
                // try packaged fallback in StreamingAssets
                var streamingPath = Path.Combine(Application.streamingAssetsPath, fileKey + ".txt");
                using var streamReq = UnityWebRequest.Get("file://" + streamingPath);
                yield return streamReq.SendWebRequest();
                if (streamReq.result == UnityWebRequest.Result.Success)
                {
                    afflictionSummary.text = streamReq.downloadHandler.text;
                }
                else if (afflictionSummary)
                {
                    Debug.LogError($"Affliction text not found locally: {streamingPath}");
                    afflictionSummary.text = "Error Finding Affliction Text";
                }
            }
            else if (afflictionSummary)
            {
                // remote succeeded: update UI and cache locally
                var text = www.downloadHandler.text;
                afflictionSummary.text = text;
                try
                {
                    var localPath = Path.Combine(Application.persistentDataPath, fileKey + ".txt");
                    File.WriteAllText(localPath, text);
                }
                catch (IOException ioe)
                {
                    Debug.LogWarning($"Failed to write affliction text to local cache: {ioe.Message}");
                }
            }
        }
    }
}
