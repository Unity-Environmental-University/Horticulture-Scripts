using System.Collections;
using TMPro;
using UnityEngine;

namespace _project.Scripts.UI
{
    public class TextFade : MonoBehaviour
    {
        public TextMeshProUGUI text;
        public float fadeDuration = 2f;

        private void OnEnable()
        {
            SetTextAlpha(1f);
            StartCoroutine(WaitAndFade());
        }

        private void SetTextAlpha(float alpha)
        {
            text.color = new Color(text.color.r, text.color.g, text.color.b, alpha);
        }

        private IEnumerator WaitAndFade()
        {
            var elapsedTime = 0f;
            var originalColor = text.color;

            while (elapsedTime < fadeDuration)
            {
                elapsedTime += Time.deltaTime;
                var alpha = Mathf.Lerp(1f, 0f, elapsedTime / fadeDuration);
                text.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
                yield return null;
            }

            SetTextAlpha(0f);

            gameObject.SetActive(false);
        }
    }
}