using System.Collections;
using _project.Scripts.Card_Core;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// ReSharper disable UnusedMember.Global

namespace _project.Scripts.UI
{
    public class MenuManager : MonoBehaviour
    {
        public GameObject buttonsUI;
        public GameObject pauseCanvas;
        public GameObject settingsUI;
        public TextMeshProUGUI versionText;
        public Button mainStartB;
        public bool isPaused;
        public float mobileUIScaleMult;
        
        private AsyncOperation _preloadOperation;

        [CanBeNull] public Toggle tutorialToggle;
        [SerializeField] private GameObject crosshair;
        [SerializeField] private GameObject menuCanvas;

        private void Awake() { if (!PlayerPrefs.HasKey("Tutorial")) ToggleTutorial(); }

        private void Start()
        {
            if (SceneManager.GetActiveScene().name == "Main") menuCanvas = null;

            if (versionText) versionText.text = "v" + Application.version;

            if (PlayerPrefs.HasKey("Tutorial") && tutorialToggle != null)
            {
                tutorialToggle.isOn = PlayerPrefs.GetInt("Tutorial", 0) == 1;
            }

#if PLATFORM_IOS || PLATFORM_IPHONE || UNITY_ANDROID || UNITY_IOS
            {
                if (mainStartB) mainStartB.enabled = false;
                if (buttonsUI) buttonsUI.transform.localScale *= mobileUIScaleMult;
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
            }
        }

        public void ToggleTutorial()
        {
            if (!tutorialToggle) return;
            PlayerPrefs.SetInt("Tutorial", tutorialToggle.isOn ? 1 : 0);
            PlayerPrefs.Save();
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

        public void QuitGame() { Application.Quit(); }

        public void OpenCourseLink() {Application.
            OpenURL("https://learn.unity.edu/product/biol-330-integrated-pest-management/01tUH000008UtF9YAK");}
    }
}
