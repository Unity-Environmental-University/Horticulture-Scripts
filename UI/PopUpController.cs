using _project.Scripts.Card_Core;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ReSharper disable UnusedMember.Local

namespace _project.Scripts.UI
{
    public class PopUpController : MonoBehaviour
    {
        [SerializeField] private GameObject popUpPanel;
        [SerializeField] private Button closeButton;
        
        [Header("Image Priority Pop-Up")]
        [SerializeField] private GameObject imagePri;
        [SerializeField] private Image imagePriImage;
        [SerializeField] private TextMeshProUGUI imagePriText;
        [Header("Text Priority Pop-Up")]
        [SerializeField] private GameObject textPri;
        [SerializeField] private Image textPriImage;
        [SerializeField] private TextMeshProUGUI textPriText;
        
        public void ActivatePopUpPanel([CanBeNull] Sprite image, bool imageFocus, string text)
        {
            if (popUpPanel.activeInHierarchy) return;

            Time.timeScale = 0;
            Click3D.Click3DGloballyDisabled = true;
            popUpPanel.SetActive(true);
            imagePri.SetActive(imageFocus);
            textPri.SetActive(!imageFocus);
            UIInputManager.RequestEnable("PopUpController");

            if (imageFocus)
            {
                if (imagePriImage) imagePriImage.sprite = image;
                if (imagePriText) imagePriText.text = text;
            }
            else
            {
                if (textPriImage) textPriImage.sprite = image;
                if (textPriText) textPriText.text = text;
            }
        }

        public void ClosePopUpPanel()
        {
            Time.timeScale = 1;
            popUpPanel.SetActive(false);
            imagePri.SetActive(false);
            textPri.SetActive(false);
            ClearPanelElements();
            UIInputManager.RequestDisable("PopUpController");
            Click3D.Click3DGloballyDisabled = false;
        }

        private void ClearPanelElements()
        {
            if (imagePriImage) imagePriImage.sprite = null;
            if (imagePriText) imagePriText.text = null;
            if (textPriImage) textPriImage.sprite = null;
            if (textPriText) textPriText.text = null;
        }
    }
}
