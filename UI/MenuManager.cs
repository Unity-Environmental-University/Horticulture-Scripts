using System.Collections;
using _project.Scripts.Card_Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace _project.Scripts.UI
{
    public class MenuManager : MonoBehaviour
    {
        [SerializeField] private GameObject crosshair;
        [SerializeField] private GameObject menuCanvas;

        private AsyncOperation _preloadOperation;

        public GameObject pauseCanvas;
        public GameObject settingsUI;
        public GameObject buttonsUI;
        public TextMeshProUGUI versionText;
        public Button mainStartB;
        public bool isPaused;
        public float mobileUIScaleMult;


        public void Start()
        {
            if (SceneManager.GetActiveScene().name == "Main") menuCanvas = null;

            if (versionText) versionText.text = "v" + Application.version;

#if PLATFORM_IOS || PLATFORM_IPHONE || UNITY_ANDROID || UNITY_IOS
            {
                if (mainStartB) mainStartB.enabled = false;
                buttonsUI.transform.localScale *= mobileUIScaleMult;
            }
#endif
        }

        public void StartGame()
        {
            menuCanvas.SetActive(false);

            StartCoroutine(LoadScene("Main"));
        }

        public void StartCardGame()
        {
            menuCanvas.SetActive(false);
            StartCoroutine(LoadScene("CardGame"));
        }

        public void ReturnToMenu()
        {
            if (menuCanvas) menuCanvas.SetActive(false);
            if (CardGameMaster.Instance) CardGameMaster.Instance.SelfDestruct();
            StartCoroutine(LoadScene("Menu"));
        }

        private static IEnumerator LoadScene(string sceneToLoad)
        {
            var asyncLoad = SceneManager.LoadSceneAsync(sceneToLoad);
            while (asyncLoad is { isDone: false }) yield return null;
        }
    
        // ReSharper disable Unity.PerformanceAnalysis
        public void PauseGame()
        {
            isPaused = !isPaused;
            pauseCanvas.SetActive(isPaused);
            if (crosshair) crosshair.gameObject.SetActive(!isPaused);

            if (isPaused)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                EventSystem.current.SetSelectedGameObject(null);
                // this prevents a button still being selected upon re-opening the pause menu
            }
        }

        public void OpenSettings()
        {
            if (pauseCanvas) pauseCanvas.SetActive(false);
            if (menuCanvas) menuCanvas.SetActive(false);
            settingsUI.SetActive(true);
        }

        public void CloseSettingsMenu()
        {
            settingsUI.SetActive(false);
            if (pauseCanvas) pauseCanvas.SetActive(true);
            if (menuCanvas) menuCanvas.SetActive(true);
        }

        public void QuitGame()
        {
            Application.Quit();
        }
    }
}