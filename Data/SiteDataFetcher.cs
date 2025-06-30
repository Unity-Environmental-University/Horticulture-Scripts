using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

namespace _project.Scripts.Data
{
    public class SiteDataFetcher : MonoBehaviour
    {
        public string url = PrivateData.WebURL;
        public string plantType;
        public string affliction;
        public TextMeshProUGUI textMeshProGui;

        private void Awake()
        {
            StartCoroutine(GetText(url));
        }

        public void SetPlant(Type pType) => plantType = pType.Name;

       // public void SetAffliction(PlantAfflictions.IAffliction plantController)

        private IEnumerator GetText(string webURL)
        {
            using var www = UnityWebRequest.Get(webURL);
            yield return www.SendWebRequest();

            if (www.result is UnityWebRequest.Result.ProtocolError or UnityWebRequest.Result.ConnectionError)
            {
                Debug.LogError(www.error);
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