using _project.Scripts.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace _project.Scripts.Data
{
    public class UserQualitySettings : MonoBehaviour
    {
        public UIDocument uiDocument;

        [SerializeField] private MenuManager menuManager;

        private Button _doneButton;
        private RadioButton _highQualityRadioButton;
        private RadioButton _lowQualityRadioButton;
        private RadioButton _mediumQualityRadioButton;
        private RadioButtonGroup _qualityRadioButtonGroup;
        private VisualElement _rootElement;
        private UIDocument _settingsUIDocument;
        private Slider _volumeSlider;

        private void OnEnable()
        {
            // Obtain the root VisualElement from the UIDocument
            _rootElement = uiDocument.rootVisualElement;

            // Query the root for all items
            _qualityRadioButtonGroup = _rootElement.Q<RadioButtonGroup>("QualityRadioButtonGroup");
            _lowQualityRadioButton = _qualityRadioButtonGroup.Q<RadioButton>("LowQualityRadioButton");
            _mediumQualityRadioButton = _qualityRadioButtonGroup.Q<RadioButton>("MediumQualityRadioButton");
            _highQualityRadioButton = _qualityRadioButtonGroup.Q<RadioButton>("HighQualityRadioButton");
            _doneButton = _rootElement.Q<Button>("DoneButton");
            _volumeSlider = _rootElement.Q<Slider>("VolumeSlider");

            // Register a callback for the done button to set the quality and hide the settings document
            _doneButton.clicked += () =>
            {
                //SetQuality(_qualityRadioButtonGroup.value);
                menuManager.CloseSettingsMenu();
            };

            // Check to see if it's all there
            if (_qualityRadioButtonGroup == null) Debug.LogError("No RadioButton Group found");
            if (_doneButton == null) Debug.LogError("No Button found");
            if (_volumeSlider == null) Debug.LogError("No Button found");

            var radioButtons = new (string Name, object Value)[]
            {
                ("HighQualityRadioButton", _highQualityRadioButton),
                ("MediumQualityRadioButton", _mediumQualityRadioButton),
                ("LowQualityRadioButton", _lowQualityRadioButton)
            };
            foreach (var rb in radioButtons)
                if (rb.Value == null)
                    Debug.LogError($"{rb.Name} was not found");


            // Set the initially checked RadioButton based on the current quality level
            if (_qualityRadioButtonGroup == null) return;
            _qualityRadioButtonGroup.value = QualitySettings.GetQualityLevel();

            // Register a callback to handle changes in the selected quality level
            _qualityRadioButtonGroup.RegisterValueChangedCallback(evt => SetQuality(evt.newValue));

            if (_volumeSlider == null) return;
            {
                _volumeSlider.value = AudioListener.volume;

                _volumeSlider.RegisterValueChangedCallback(evt => SetVolume(evt.newValue));
            }
        }

        private static void SetVolume(float newVolume)
        {
            AudioListener.volume = newVolume;
            Debug.Log($"Volume set to {newVolume}");
        }

        // Method to change the quality setting when a new RadioButton is selected
        private static void SetQuality(int newQualityLevel)
        {
            QualitySettings.SetQualityLevel(newQualityLevel);
            Debug.Log($"Quality level set to {newQualityLevel}" + " " +
                      QualitySettings.names[QualitySettings.GetQualityLevel()]);
        }
    }
}