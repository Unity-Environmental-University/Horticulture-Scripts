using System.Collections;
using _project.Scripts.Card_Core;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace _project.Scripts.UI
{
    public class PopUpController : MonoBehaviour
    {
        [SerializeField] private GameObject popUpPanel;
        [SerializeField] private Image displayedImage;
        [SerializeField] private TextMeshProUGUI popUpText;
        [SerializeField] private Button closeButton;

        // Testing
        private void Start() { StartCoroutine(DelayedStart()); }

        private IEnumerator DelayedStart()
        {
            yield return new WaitForSeconds(1f);
            ToggleUiInput();
            Click3D.click3DGloballyDisabled = true;
        }

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
            var inputModule = CardGameMaster.Instance.eventSystem.GetComponent<InputSystemUIInputModule>();
            inputModule.enabled = !inputModule.enabled;
        }

        public void ShowTutorialOne()
        {
            const string popText = "Some Words Here";
            ActivatePopUpPanel(null, popText);
        }
    }
}