using System;
using _project.Scripts.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace _project.Scripts.Data
{
    public class UserQualitySettings : MonoBehaviour
    {
        private const string QualityLevelPrefKey = "QualityLevel";
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
            _doneButton.clicked += OnDoneButtonClicked;

            // Check to see if it's all there
            if (_qualityRadioButtonGroup == null) Debug.LogError("No RadioButtonGroup found");
            if (_doneButton == null) Debug.LogError("No DoneButton found");
            if (_volumeSlider == null) Debug.LogError("No VolumeSlider found");
            if (_resolutionDropdown == null) Debug.LogError("No ResolutionDropdown found");
            if (_displayModeDropdown == null) Debug.LogError("No DisplayModeDropdown found");

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

            // Load saved quality preference (if any)
            // ReSharper disable once UnusedVariable
            var savedQuality = PlayerPrefs.GetInt(QualityLevelPrefKey, -1);

#if PLATFORM_IOS || PLATFORM_IPHONE || UNITY_ANDROID || UNITY_IOS
            {
                // Adjust UI to reflect the platform
                if (_mobileQualityRadioButton != null) _mobileQualityRadioButton.style.display = DisplayStyle.Flex;
                if (_highQualityRadioButton != null) _highQualityRadioButton.style.display = DisplayStyle.None;
                if (_resolutionDropdown != null) _resolutionDropdown.style.display = DisplayStyle.None;
                if (_displayModeDropdown != null) _displayModeDropdown.style.display = DisplayStyle.None;
                
                // If no saved preference, set default to Mobile quality
                var mobileQualityIndex = Array.IndexOf(QualitySettings.names, "Mobile");
                if (mobileQualityIndex < 0)
                    mobileQualityIndex = QualitySettings.names.Length - 1;
                if (savedQuality < 0)
                    SetQuality(mobileQualityIndex);
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
                _displayModeDropdown.RegisterValueChangedCallback(OnDisplayModeChanged);
            }

            // Initialize resolution dropdown to current screen size and register callback
            if (_resolutionDropdown != null)
            {
                var currentRes = $"{Screen.width}x{Screen.height}";
                if (_resolutionDropdown.choices.Contains(currentRes))
                    _resolutionDropdown.value = currentRes;
                else if (_resolutionDropdown.choices.Count > 0)
                    _resolutionDropdown.value = _resolutionDropdown.choices[0];
                _resolutionDropdown.RegisterValueChangedCallback(OnResolutionChanged);
            }

            // Set the initially checked RadioButton based on the current quality level
            if (_qualityRadioButtonGroup == null) return;
            _qualityRadioButtonGroup.value = QualitySettings.names[QualitySettings.GetQualityLevel()] switch
            {
                "High" => _qualityRadioButtonGroup.IndexOf(_highQualityRadioButton),
                "Medium" => _qualityRadioButtonGroup.IndexOf(_mediumQualityRadioButton),
                "Low" => _qualityRadioButtonGroup.IndexOf(_lowQualityRadioButton),
                "Mobile" => _qualityRadioButtonGroup.IndexOf(_mobileQualityRadioButton),
                _ => 0
            };


            // Register a callback to handle changes in the selected quality level
            _qualityRadioButtonGroup.RegisterValueChangedCallback(OnQualityChanged);

            if (_volumeSlider == null) return;
            {
                _volumeSlider.value = AudioListener.volume;

                _volumeSlider.RegisterValueChangedCallback(OnVolumeChanged);
            }
        }

        private void OnDisable()
        {
            _doneButton.clicked -= OnDoneButtonClicked;
            _displayModeDropdown?.UnregisterValueChangedCallback(OnDisplayModeChanged);
            _resolutionDropdown?.UnregisterValueChangedCallback(OnResolutionChanged);
            _qualityRadioButtonGroup?.UnregisterValueChangedCallback(OnQualityChanged);
            _volumeSlider?.UnregisterValueChangedCallback(OnVolumeChanged);
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

        // Method to change the display mode when the dropdown value changes
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
        private void SetQuality(int radioIndex)
        {
            var selectedName = _qualityRadioButtonGroup[radioIndex].name;

            var qualityIndex = selectedName switch
            {
                "HighQualityRadioButton" => Array.IndexOf(QualitySettings.names, "High"),
                "MediumQualityRadioButton" => Array.IndexOf(QualitySettings.names, "Medium"),
                "LowQualityRadioButton" => Array.IndexOf(QualitySettings.names, "Low"),
                "MobileQualityRadioButton" => Array.IndexOf(QualitySettings.names, "Mobile"),
                _ => -1
            };

            if (qualityIndex >= 0)
            {
                QualitySettings.SetQualityLevel(qualityIndex);
                PlayerPrefs.SetInt(QualityLevelPrefKey, qualityIndex);
                PlayerPrefs.Save();
                Debug.Log($"Quality level set to {qualityIndex} {QualitySettings.names[qualityIndex]}");
            }
            else
            {
                Debug.LogError($"Unrecognized quality button: {selectedName}");
            }
        }

        private void OnDoneButtonClicked()
        {
            menuManager.CloseSettingsMenu();
        }

        private static void OnDisplayModeChanged(ChangeEvent<string> evt)
        {
            SetDisplayMode(evt.newValue);
        }

        private static void OnResolutionChanged(ChangeEvent<string> evt)
        {
            SetResolution(evt.newValue);
        }

        private void OnQualityChanged(ChangeEvent<int> evt)
        {
            SetQuality(evt.newValue);
        }

        private static void OnVolumeChanged(ChangeEvent<float> evt)
        {
            SetVolume(evt.newValue);
        }
    }
}