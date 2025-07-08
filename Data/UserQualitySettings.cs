using _project.Scripts.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace _project.Scripts.Data
{
    public class UserQualitySettings : MonoBehaviour
    {
        public UIDocument uiDocument;

        [SerializeField] private MenuManager menuManager;
        private DropdownField _displayModeDropdown;

        private Button _doneButton;
        private RadioButton _highQualityRadioButton;
        private RadioButton _lowQualityRadioButton;
        private RadioButton _mediumQualityRadioButton;
        private RadioButton _mobileQualityRadioButton;
        private RadioButtonGroup _qualityRadioButtonGroup;
        private DropdownField _resolutionDropdown;
        private VisualElement _rootElement;
        private UIDocument _settingsUIDocument;
        private Slider _volumeSlider;
        
        private void OnEnable()
        {
            // Get the root VisualElement from the UIDocument
            _rootElement = uiDocument.rootVisualElement;

            // Query the root for all items
            _qualityRadioButtonGroup = _rootElement.Q<RadioButtonGroup>("QualityRadioButtonGroup");
            _mobileQualityRadioButton = _qualityRadioButtonGroup.Q<RadioButton>("MobileQualityRadioButton");
            _lowQualityRadioButton = _qualityRadioButtonGroup.Q<RadioButton>("LowQualityRadioButton");
            _mediumQualityRadioButton = _qualityRadioButtonGroup.Q<RadioButton>("MediumQualityRadioButton");
            _highQualityRadioButton = _qualityRadioButtonGroup.Q<RadioButton>("HighQualityRadioButton");
            _doneButton = _rootElement.Q<Button>("DoneButton");
            _volumeSlider = _rootElement.Q<Slider>("VolumeSlider");
            _resolutionDropdown = _rootElement.Q<DropdownField>("ResolutionDropDown");
            _displayModeDropdown = _rootElement.Q<DropdownField>("DisplayModeDropDown");

            // Register a callback for the done button to set the quality and hide the settings document
            _doneButton.clicked += () => { menuManager.CloseSettingsMenu(); };

            // Check to see if it's all there
            if (_qualityRadioButtonGroup == null) Debug.LogError("No RadioButton Group found");
            if (_doneButton == null) Debug.LogError("No Button found");
            if (_volumeSlider == null) Debug.LogError("No Button found");
            if (_resolutionDropdown == null) Debug.LogError("No Resolution dropdown found");
            if (_displayModeDropdown == null) Debug.LogError("No DisplayMode dropdown found");

            var radioButtons = new (string Name, object Value)[]
            {
                ("HighQualityRadioButton", _highQualityRadioButton),
                ("MediumQualityRadioButton", _mediumQualityRadioButton),
                ("LowQualityRadioButton", _lowQualityRadioButton),
                ("MobileQualityRadioButton", _mobileQualityRadioButton)
            };
            foreach (var rb in radioButtons)
                if (rb.Value == null)
                    Debug.LogError($"{rb.Name} was not found");

#if PLATFORM_IOS || PLATFORM_IPHONE || UNITY_ANDROID || UNITY_IOS
            {
                // Adjust UI to reflect the platform
                if (_mobileQualityRadioButton != null) _mobileQualityRadioButton.style.display = DisplayStyle.Flex;
                if (_resolutionDropdown != null) _resolutionDropdown.style.display = DisplayStyle.None;
                if (_displayModeDropdown != null) _displayModeDropdown.style.display = DisplayStyle.None;
                
                // If we're on mobile, set Mobile quality
                const int mobileLevelIndex = 4;
                SetQuality(mobileLevelIndex);
                if (_qualityRadioButtonGroup != null) _qualityRadioButtonGroup.value = mobileLevelIndex;
            }
#else
            {
                _mobileQualityRadioButton.style.display = DisplayStyle.None;
            }
#endif

            // Initialize display mode dropdown to current fullscreen mode and register callback
            if (_displayModeDropdown != null)
            {
                var currentMode = Screen.fullScreenMode switch
                {
                    FullScreenMode.FullScreenWindow => "Windowed FullScreen",
                    FullScreenMode.ExclusiveFullScreen => "FullScreen Exclusive",
                    _ => "Windowed"
                };
                if (_displayModeDropdown.choices.Contains(currentMode))
                    _displayModeDropdown.value = currentMode;
                else if (_displayModeDropdown.choices.Count > 0)
                    _displayModeDropdown.value = _displayModeDropdown.choices[0];
                _displayModeDropdown.RegisterValueChangedCallback(evt => SetDisplayMode(evt.newValue));
            }

            // Initialize resolution dropdown to current screen size and register callback
            if (_resolutionDropdown != null)
            {
                var currentRes = $"{Screen.width}x{Screen.height}";
                if (_resolutionDropdown.choices.Contains(currentRes))
                    _resolutionDropdown.value = currentRes;
                else if (_resolutionDropdown.choices.Count > 0)
                    _resolutionDropdown.value = _resolutionDropdown.choices[0];
                _resolutionDropdown.RegisterValueChangedCallback(evt => SetResolution(evt.newValue));
            }

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

        // Method to change screen resolution when the dropdown value changes
        private static void SetResolution(string resolution)
        {
            var parts = resolution.Split('x');
            if (parts.Length != 2 || !int.TryParse(parts[0], out var w) || !int.TryParse(parts[1], out var h))
            {
                Debug.LogError($"Invalid resolution format: {resolution}");
                return;
            }

            Screen.SetResolution(w, h, Screen.fullScreen);
            Debug.Log($"Resolution set to {w}x{h}");
        }

        // Method to change display mode when the dropdown value changes
        private static void SetDisplayMode(string displayMode)
        {
            var mode = displayMode switch
            {
                "Windowed FullScreen" => FullScreenMode.FullScreenWindow,
                "FullScreen Exclusive" => FullScreenMode.ExclusiveFullScreen,
                _ => FullScreenMode.Windowed
            };
            Screen.fullScreenMode = mode;
            Debug.Log($"Display mode set to {displayMode}");
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
