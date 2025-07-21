using _project.Scripts.Card_Core;
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
        
        private void ActivatePopUpPanel(Image image, string text)
        {
            if (popUpPanel.activeInHierarchy) return;

            Click3D.click3DGloballyDisabled = true;
            popUpPanel.SetActive(true);
            ToggleUiInput();

            if (image != null && text != null)
            {
                displayedImage.sprite = image.sprite;
                popUpText.text = text;
            }
            else
            {
                popUpText.text = "PLEASE PASS IN A STRING";
            }
        }

        public void ClosePopUpPanel()
        {
            popUpPanel.SetActive(false);
            ClearPanelElements();
            ToggleUiInput();
            Click3D.click3DGloballyDisabled = false;
        }

        private void ClearPanelElements()
        {
            displayedImage.sprite = null;
            popUpText.text = null;
        }

        // ReSharper disable once MemberCanBeMadeStatic.Local
        private void ToggleUiInput()
        {
            CardGameMaster.Instance.uiInputModule.enabled = !CardGameMaster.Instance.uiInputModule.enabled;
        }
    }
}