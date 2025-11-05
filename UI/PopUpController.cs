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

        [Header("Image Priority Pop-Up")] [SerializeField]
        private GameObject imagePri;

        [SerializeField] private Image imagePriImage;
        [SerializeField] private TextMeshProUGUI imagePriText;

        [Header("Text Priority Pop-Up")] [SerializeField]
        private GameObject textPri;

        [SerializeField] private Image textPriImage;
        [SerializeField] private TextMeshProUGUI textPriText;

        public void ActivatePopUpPanel([CanBeNull] Sprite image, bool imageFocus, string text)
        {
            if (!popUpPanel)
            {
                Debug.LogError("PopUpController requires a popUpPanel reference before it can be used.", this);
                return;
            }

            if (popUpPanel.activeInHierarchy) return;
            if (!EnsurePriorityRefs()) return;

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
            if (!popUpPanel)
            {
                Debug.LogError("PopUpController requires a popUpPanel reference before it can be used.", this);
                return;
            }

            Time.timeScale = 1;
            popUpPanel.SetActive(false);
            if (imagePri) imagePri.SetActive(false);
            if (textPri) textPri.SetActive(false);
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

        private bool EnsurePriorityRefs()
        {
            var isValid = true;

            if (!imagePri)
            {
                Debug.LogError("PopUpController requires the imagePri GameObject to be assigned in the inspector.",
                    this);
                isValid = false;
            }

            if (textPri) return isValid;
            Debug.LogError("PopUpController requires the textPri GameObject to be assigned in the inspector.", this);

            return false;
        }
    }
}