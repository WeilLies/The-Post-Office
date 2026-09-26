using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class SettingsUI : MonoBehaviour
{
    [System.Serializable]
    private struct ArrowControl
    {
        public Button leftButton;
        public Button rightButton;
        public TextMeshProUGUI valueText;
    }

    [System.Serializable]
    private struct SliderControl
    {
        public Slider slider;
        public TextMeshProUGUI valueText;
    }

    [SerializeField] private ArrowControl language;
    [SerializeField] private SliderControl sensitivity;
    [SerializeField] private SliderControl masterVolume;
    [SerializeField] private SliderControl musicVolume;
    [SerializeField] private SliderControl sfxVolume;
    [SerializeField] private SliderControl uiVolume;
    [SerializeField] private ArrowControl screenResolution;
    [SerializeField] private ArrowControl screenMode;
    [SerializeField] private ArrowControl fpsLimit;
    [SerializeField] private ArrowControl vSync;
    [SerializeField] private ArrowControl textureQuality;
    [SerializeField] private ArrowControl shadowQuality;
    [SerializeField] private ArrowControl outputDevice;
    [SerializeField] private Button saveButton;
    [SerializeField] private Button resetButton;

    private ISettingsManager settingsManager;

    private void Start()
    {
        settingsManager = ServiceLocator.Get<ISettingsManager>();

        if (settingsManager == null)
        {
            Debug.LogError("SettingsManager is null.");
            return;
        }

        InitSliders();
        BindSliders();
        BindArrows();

        if (saveButton) saveButton.onClick.AddListener(OnSave);
        if (resetButton) resetButton.onClick.AddListener(OnReset);

        settingsManager.GetParameter<IndexedParameter>("Language").OnChanged += RefreshLanguage;
        settingsManager.GetParameter<SettingsParameter<float>>("Sensitivity").OnChanged += RefreshSensitivity;
        settingsManager.GetParameter<SettingsParameter<float>>("MasterVolume").OnChanged += RefreshMasterVolume;
        settingsManager.GetParameter<SettingsParameter<float>>("MusicVolume").OnChanged += RefreshMusicVolume;
        settingsManager.GetParameter<SettingsParameter<float>>("SFXVolume").OnChanged += RefreshSFXVolume;
        settingsManager.GetParameter<SettingsParameter<float>>("UIVolume").OnChanged += RefreshUIVolume;
        settingsManager.GetParameter<IndexedParameter>("ScreenResolution").OnChanged += RefreshScreenResolution;
        settingsManager.GetParameter<IndexedParameter>("ScreenMode").OnChanged += RefreshScreenMode;
        settingsManager.GetParameter<SettingsParameter<int>>("FpsStepIndex").OnChanged += RefreshFpsLimit;
        settingsManager.GetParameter<IndexedParameter>("VSync").OnChanged += RefreshVSync;
        settingsManager.GetParameter<IndexedParameter>("TextureQuality").OnChanged += RefreshTextureQuality;
        settingsManager.GetParameter<IndexedParameter>("ShadowQuality").OnChanged += RefreshShadowQuality;
        settingsManager.GetParameter<IndexedParameter>("OutputDevice").OnChanged += RefreshOutputDevice;

        RefreshAll();
    }

    private void OnEnable()
    {
        if (settingsManager != null)
            RefreshAll();
    }

    private void OnDestroy()
    {
        if (settingsManager == null) return;

        settingsManager.GetParameter<IndexedParameter>("Language").OnChanged -= RefreshLanguage;
        settingsManager.GetParameter<SettingsParameter<float>>("Sensitivity").OnChanged -= RefreshSensitivity;
        settingsManager.GetParameter<SettingsParameter<float>>("MasterVolume").OnChanged -= RefreshMasterVolume;
        settingsManager.GetParameter<SettingsParameter<float>>("MusicVolume").OnChanged -= RefreshMusicVolume;
        settingsManager.GetParameter<SettingsParameter<float>>("SFXVolume").OnChanged -= RefreshSFXVolume;
        settingsManager.GetParameter<SettingsParameter<float>>("UIVolume").OnChanged -= RefreshUIVolume;
        settingsManager.GetParameter<IndexedParameter>("ScreenResolution").OnChanged -= RefreshScreenResolution;
        settingsManager.GetParameter<IndexedParameter>("ScreenMode").OnChanged -= RefreshScreenMode;
        settingsManager.GetParameter<SettingsParameter<int>>("FpsStepIndex").OnChanged -= RefreshFpsLimit;
        settingsManager.GetParameter<IndexedParameter>("VSync").OnChanged -= RefreshVSync;
        settingsManager.GetParameter<IndexedParameter>("TextureQuality").OnChanged -= RefreshTextureQuality;
        settingsManager.GetParameter<IndexedParameter>("ShadowQuality").OnChanged -= RefreshShadowQuality;
        settingsManager.GetParameter<IndexedParameter>("OutputDevice").OnChanged -= RefreshOutputDevice;
    }

    private void InitSliders()
    {
        SetupSlider(sensitivity.slider, 0.05f, 1f, false);
        SetupSlider(masterVolume.slider, 0f, 2f, false);
        SetupSlider(musicVolume.slider, 0f, 2f, false);
        SetupSlider(sfxVolume.slider, 0f, 2f, false);
        SetupSlider(uiVolume.slider, 0f, 2f, false);
    }

    private void SetupSlider(Slider slider, float min, float max, bool wholeNumbers)
    {
        if (!slider) return;
        slider.minValue = min;
        slider.maxValue = max;
        slider.wholeNumbers = wholeNumbers;
    }

    private void BindSliders()
    {
        if (sensitivity.slider)
            sensitivity.slider.onValueChanged.AddListener(OnSensitivitySliderChanged);

        if (masterVolume.slider)
            masterVolume.slider.onValueChanged.AddListener(OnMasterVolumeSliderChanged);

        if (musicVolume.slider)
            musicVolume.slider.onValueChanged.AddListener(OnMusicVolumeSliderChanged);

        if (sfxVolume.slider)
            sfxVolume.slider.onValueChanged.AddListener(OnSFXVolumeSliderChanged);

        if (uiVolume.slider)
            uiVolume.slider.onValueChanged.AddListener(OnUIVolumeSliderChanged);
    }

    private void BindArrows()
    {
        BindArrow(language.leftButton, () => settingsManager.GetParameter<IndexedParameter>("Language").Step(-1));
        BindArrow(language.rightButton, () => settingsManager.GetParameter<IndexedParameter>("Language").Step(1));

        BindArrow(screenResolution.leftButton, () => settingsManager.GetParameter<IndexedParameter>("ScreenResolution").Step(-1));
        BindArrow(screenResolution.rightButton, () => settingsManager.GetParameter<IndexedParameter>("ScreenResolution").Step(1));

        BindArrow(screenMode.leftButton, () => settingsManager.GetParameter<IndexedParameter>("ScreenMode").Step(-1));
        BindArrow(screenMode.rightButton, () => settingsManager.GetParameter<IndexedParameter>("ScreenMode").Step(1));

        BindArrow(fpsLimit.leftButton, () => settingsManager.GetParameter<SettingsParameter<int>>("FpsStepIndex").Set(Mathf.Clamp(settingsManager.FpsStepIndex - 1, 0, settingsManager.FpsStepsCount - 1)));
        BindArrow(fpsLimit.rightButton, () => settingsManager.GetParameter<SettingsParameter<int>>("FpsStepIndex").Set(Mathf.Clamp(settingsManager.FpsStepIndex + 1, 0, settingsManager.FpsStepsCount - 1)));

        BindArrow(vSync.leftButton, () => settingsManager.GetParameter<IndexedParameter>("VSync").Step(-1));
        BindArrow(vSync.rightButton, () => settingsManager.GetParameter<IndexedParameter>("VSync").Step(1));

        BindArrow(textureQuality.leftButton, () => settingsManager.GetParameter<IndexedParameter>("TextureQuality").Step(-1));
        BindArrow(textureQuality.rightButton, () => settingsManager.GetParameter<IndexedParameter>("TextureQuality").Step(1));

        BindArrow(shadowQuality.leftButton, () => settingsManager.GetParameter<IndexedParameter>("ShadowQuality").Step(-1));
        BindArrow(shadowQuality.rightButton, () => settingsManager.GetParameter<IndexedParameter>("ShadowQuality").Step(1));

        BindArrow(outputDevice.leftButton, () => settingsManager.GetParameter<IndexedParameter>("OutputDevice").Step(-1));
        BindArrow(outputDevice.rightButton, () => settingsManager.GetParameter<IndexedParameter>("OutputDevice").Step(1));
    }

    private static void BindArrow(Button btn, System.Action action)
    {
        if (btn) btn.onClick.AddListener(() => action());
    }

    private void OnSensitivitySliderChanged(float value)
    {
        settingsManager.GetParameter<SettingsParameter<float>>("Sensitivity").Set(value);
    }

    private void OnMasterVolumeSliderChanged(float value)
    {
        settingsManager.GetParameter<SettingsParameter<float>>("MasterVolume").Set(value);
    }

    private void OnMusicVolumeSliderChanged(float value)
    {
        settingsManager.GetParameter<SettingsParameter<float>>("MusicVolume").Set(value);
    }

    private void OnSFXVolumeSliderChanged(float value)
    {
        settingsManager.GetParameter<SettingsParameter<float>>("SFXVolume").Set(value);
    }

    private void OnUIVolumeSliderChanged(float value)
    {
        settingsManager.GetParameter<SettingsParameter<float>>("UIVolume").Set(value);
    }

    private void OnSave()
    {
        settingsManager.Save();
    }

    private void OnReset()
    {
        settingsManager.ResetToDefaults();
    }

    private void RefreshAll()
    {
        RefreshLanguage();
        RefreshSensitivity();
        RefreshMasterVolume();
        RefreshMusicVolume();
        RefreshSFXVolume();
        RefreshUIVolume();
        RefreshScreenResolution();
        RefreshScreenMode();
        RefreshFpsLimit();
        RefreshVSync();
        RefreshTextureQuality();
        RefreshShadowQuality();
        RefreshOutputDevice();
    }

    private static void SetSliderValueWithoutNotify(Slider slider, float value, UnityAction<float> callback)
    {
        if (!slider) return;
        slider.onValueChanged.RemoveListener(callback);
        slider.value = value;
        slider.onValueChanged.AddListener(callback);
    }

    private static void SetText(TextMeshProUGUI label, string value)
    {
        if (label) label.text = value;
    }

    private static string ToPercent(float value) => Mathf.RoundToInt(value * 100f) + "%";

    private void RefreshLanguage()
    {
        SetText(language.valueText, settingsManager.GetParameter<IndexedParameter>("Language").Current);
    }

    private void RefreshSensitivity()
    {
        float val = settingsManager.GetParameter<SettingsParameter<float>>("Sensitivity").Value;
        SetSliderValueWithoutNotify(sensitivity.slider, val, OnSensitivitySliderChanged);
        SetText(sensitivity.valueText, val.ToString("F2"));
    }

    private void RefreshMasterVolume()
    {
        float val = settingsManager.GetParameter<SettingsParameter<float>>("MasterVolume").Value;
        SetSliderValueWithoutNotify(masterVolume.slider, val, OnMasterVolumeSliderChanged);
        SetText(masterVolume.valueText, ToPercent(val));
    }

    private void RefreshMusicVolume()
    {
        float val = settingsManager.GetParameter<SettingsParameter<float>>("MusicVolume").Value;
        SetSliderValueWithoutNotify(musicVolume.slider, val, OnMusicVolumeSliderChanged);
        SetText(musicVolume.valueText, ToPercent(val));
    }

    private void RefreshSFXVolume()
    {
        float val = settingsManager.GetParameter<SettingsParameter<float>>("SFXVolume").Value;
        SetSliderValueWithoutNotify(sfxVolume.slider, val, OnSFXVolumeSliderChanged);
        SetText(sfxVolume.valueText, ToPercent(val));
    }

    private void RefreshUIVolume()
    {
        float val = settingsManager.GetParameter<SettingsParameter<float>>("UIVolume").Value;
        SetSliderValueWithoutNotify(uiVolume.slider, val, OnUIVolumeSliderChanged);
        SetText(uiVolume.valueText, ToPercent(val));
    }

    private void RefreshScreenResolution()
    {
        SetText(screenResolution.valueText, settingsManager.GetParameter<IndexedParameter>("ScreenResolution").Current);
    }

    private void RefreshScreenMode()
    {
        SetText(screenMode.valueText, settingsManager.GetParameter<IndexedParameter>("ScreenMode").Current);
    }

    private void RefreshFpsLimit()
    {
        SetText(fpsLimit.valueText, settingsManager.FpsLimit.ToString("0") + "FPS");
    }

    private void RefreshVSync()
    {
        SetText(vSync.valueText, settingsManager.GetParameter<IndexedParameter>("VSync").Current);
    }

    private void RefreshTextureQuality()
    {
        SetText(textureQuality.valueText, settingsManager.GetParameter<IndexedParameter>("TextureQuality").Current);
    }

    private void RefreshShadowQuality()
    {
        SetText(shadowQuality.valueText, settingsManager.GetParameter<IndexedParameter>("ShadowQuality").Current);
    }

    private void RefreshOutputDevice()
    {
        SetText(outputDevice.valueText, settingsManager.GetParameter<IndexedParameter>("OutputDevice").Current);
    }
}
