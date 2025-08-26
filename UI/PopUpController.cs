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
        [SerializeField] private Image displayedImage;
        [SerializeField] private TextMeshProUGUI popUpText;
        [SerializeField] private Button closeButton;
        
        public void ActivatePopUpPanel([CanBeNull] Image image, string text)
        {
            if (popUpPanel.activeInHierarchy) return;

            Time.timeScale = 0;
            Click3D.Click3DGloballyDisabled = true;
            popUpPanel.SetActive(true);
            ToggleUiInput();

            if (image != null) displayedImage.sprite = image.sprite;
            popUpText.text = text;
        }

        public void ClosePopUpPanel()
        {
            Time.timeScale = 1;
            popUpPanel.SetActive(false);
            ClearPanelElements();
            ToggleUiInput();
            Click3D.Click3DGloballyDisabled = false;
        }

        private void ClearPanelElements()
        {
            displayedImage.sprite = null;
            popUpText.text = null;
        }

        // ReSharper disable once MemberCanBeMadeStatic.Local
        private void ToggleUiInput() { CardGameMaster.Instance.uiInputModule.enabled = !CardGameMaster.Instance.uiInputModule.enabled; }
    }
}