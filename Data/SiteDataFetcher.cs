using System.Collections;
using System.Text.RegularExpressions;
using _project.Scripts.Classes;
using _project.Scripts.Core;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

namespace _project.Scripts.Data
{
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
            using var www = UnityWebRequest.Get(webURL + fileKey + ".txt");
            yield return www.SendWebRequest();

            if (www.result is UnityWebRequest.Result.ProtocolError or UnityWebRequest.Result.ConnectionError)
            {
                Debug.LogError(www.error);
                Debug.LogError(webURL + fileKey + ".txt");
                if (plantSummary) plantSummary.text = "Error Finding Plant Text";
            }
            else if (plantSummary)
            {
                plantSummary.text = www.downloadHandler.text;
            }
        }

        /// <summary>
        ///     Coroutine to fetch affliction description text from the server.
        /// </summary>
        private IEnumerator GetAfflictionText(string webURL)
        {
            var baseName = affliction.Replace(" ", string.Empty);
            var hyphenName = Regex.Replace(baseName, "(?<!^)([A-Z])", "-$1");
            var fileKey = UnityWebRequest.EscapeURL(hyphenName);
            using var www = UnityWebRequest.Get(webURL + fileKey + ".txt");
            yield return www.SendWebRequest();

            if (www.result is UnityWebRequest.Result.ProtocolError or UnityWebRequest.Result.ConnectionError)
            {
                Debug.LogError(www.error);
                Debug.LogError(webURL + fileKey + ".txt");
                if (afflictionSummary) afflictionSummary.text = "Error Finding Affliction Text";
            }
            else if (afflictionSummary)
            {
                afflictionSummary.text = www.downloadHandler.text;
            }
        }
    }
}