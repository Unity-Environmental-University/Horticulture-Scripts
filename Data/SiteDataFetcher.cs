using System.Collections;
using _project.Scripts.Core;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

namespace _project.Scripts.Data
{
    public class SiteDataFetcher : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI textMeshProGui;
        private const string URL = PrivateData.RawGithubContent;
        private string plantType;
        private string affliction;

        private void Awake()
        {
            StartCoroutine(GetText(URL));
        }

        public void SetPlant(PlantType plantTypeEnum)
        {
            plantType = plantTypeEnum.ToString();
            StartCoroutine(GetText(URL));
        }

       // public void SetAffliction(PlantAfflictions.IAffliction plantController)

        private IEnumerator GetText(string webURL)
        {
            using var www = UnityWebRequest.Get(webURL + plantType + ".txt");
            yield return www.SendWebRequest();

            if (www.result is UnityWebRequest.Result.ProtocolError or UnityWebRequest.Result.ConnectionError)
            {
                Debug.LogError(www.error);
                Debug.LogError(webURL + plantType + ".txt");
                textMeshProGui.text = "Error Finding Text";
            }
            else
            {
                var text = www.downloadHandler.text;
                if (textMeshProGui) textMeshProGui.text = text;
            }
        }
    }
}