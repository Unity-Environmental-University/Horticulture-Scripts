using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace _project.Scripts.UI
{
    public class PanelFade : MonoBehaviour
    {
        public GameObject panel;
        public Image panelColor;
        public float fadeSpeed = 0.5f;
        public float fadeHoldTime = 0.5f;
        public bool isScreenOff;

        public event Action OnScreenOff;

        public void StartFade() => StartCoroutine(FadePanel());
    
        private IEnumerator FadePanel()
        {
            var elapsedTime = 0f;
            panel.SetActive(true);

            while (elapsedTime < fadeSpeed)
            {
                elapsedTime += Time.deltaTime;
                var alpha = Mathf.Lerp(0f, 1f, elapsedTime / fadeSpeed);
                panelColor.color = new Color(0, 0, 0, alpha);
                isScreenOff = !isScreenOff;
                yield return null;
            }
        
            //This is the full black space, we invoke an event to be set here
            OnScreenOff?.Invoke();

            yield return new WaitForSeconds(fadeHoldTime);

            elapsedTime = 0f;

            while (elapsedTime < fadeSpeed)
            {
                elapsedTime += Time.deltaTime;
                var alpha = Mathf.Lerp(1f, 0f, elapsedTime / fadeSpeed);
                panelColor.color = new Color(0, 0, 0, alpha);
                isScreenOff = !isScreenOff;
                yield return null;
            }

            panel.SetActive(false);
        }
    }
}