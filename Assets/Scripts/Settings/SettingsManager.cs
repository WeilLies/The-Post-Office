using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class SettingsManager : MonoBehaviour, ISettingsManager, IAsyncInitializable
{
    private static readonly string[] LanguageOptions = { "English", "Russian", "Spain" };
    private static readonly string[] ScreenModeOptions = { "Fullscreen", "Windowed", "Borderless" };
    private static readonly string[] QualityOptions = { "Low", "Medium", "High" };
    private static readonly string[] VSyncOptions = { "Disable", "Enable" };
    private static readonly int[] FpsSteps = { 30, 60, 90, 120, 144, 180, 240, 360, 480 };

    private IndexedParameter _language;
    private SettingsParameter<float> _sensitivity;
    private SettingsParameter<float> _masterVolume;
    private SettingsParameter<float> _musicVolume;
    private SettingsParameter<float> _sfxVolume;
    private SettingsParameter<float> _uiVolume;
    private IndexedParameter _screenResolution;
    private IndexedParameter _screenMode;
    private SettingsParameter<int> _fpsStepIndex;
    private IndexedParameter _vSync;
    private IndexedParameter _textureQuality;
    private IndexedParameter _shadowQuality;
    private IndexedParameter _outputDevice;

    private string SavePath => Path.Combine(Application.persistentDataPath, "settings.json");

    [Serializable]
    private class SettingsData
    {
        public int languageIndex = 0;
        public float sensitivity = 1f;
        public float masterVolume = 1f;
        public float musicVolume = 1f;
        public float sfxVolume = 1f;
        public float uiVolume = 1f;
        public string screenResolution = "1920x1080";
        public int screenModeIndex = 0;
        public int fpsLimit = 120;
        public int vSyncIndex = 0;
        public int textureQualityIndex = 2;
        public int shadowQualityIndex = 2;
        public int outputDeviceIndex = 0;
    }

    public List<Resolution> FilteredResolutions { get; } = new List<Resolution>();
    public List<string> AllDevices { get; } = new List<string>();
    public List<string> OutputDevices { get; } = new List<string>();

    public event Action OnParametersChanged;

    public string Language => _language.Current;
    public float Sensitivity => _sensitivity.Value;
    public float MasterVolume => _masterVolume.Value;
    public float MusicVolume => _musicVolume.Value;
    public float SFXVolume => _sfxVolume.Value;
    public float UIVolume => _uiVolume.Value;
    public string ScreenResolution => _screenResolution.Current;
    public string ScreenMode => _screenMode.Current;
    public float FpsLimit => FpsSteps[_fpsStepIndex.Value];
    public int VSync => _vSync.Index;
    public string TextureQuality => _textureQuality.Current;
    public string ShadowQuality => _shadowQuality.Current;
    public string OutputDevice => _outputDevice.Current;

    public int LanguageIndex => _language.Index;
    public int ScreenResolutionIndex => _screenResolution.Index;
    public int ScreenModeIndex => _screenMode.Index;
    public int LanguageCount => _language.Count;
    public int ScreenModeCount => _screenMode.Count;
    public int FpsStepIndex => _fpsStepIndex.Value;
    public int FpsStepsCount => FpsSteps.Length;
    public string[] FpsStepLabels => Array.ConvertAll(FpsSteps, x => x.ToString());

    private FMOD.System _coreSystem;

    private bool isLoaded;

    private void Awake()
    {
        _coreSystem = FMODUnity.RuntimeManager.CoreSystem;

        PopulateResolutionList();
        PopulateMicrophoneList();
        PopulateOutputDeviceList();

        InitParameters();
        LoadSettings();
        ApplyAllSettings();

        isLoaded = true;
    }

    public IEnumerator InitializeAsync()
    {
        yield return new WaitUntil(() => isLoaded);
    }

    private void InitParameters()
    {
        string[] resLabels = BuildResolutionLabels();

        int defaultResIndex = Array.FindIndex(resLabels, r => r == "1920x1080");
        if (defaultResIndex < 0) defaultResIndex = resLabels.Length - 1;

        string[] outputDeviceLabels = OutputDevices.Count > 0 ? OutputDevices.ToArray() : new[] { "Default" };

        _language = new IndexedParameter("Language", LanguageOptions);
        _sensitivity = new SettingsParameter<float>("Sensitivity", 0.5f, v => Mathf.Clamp(v, 0.05f, 1f));
        _masterVolume = new SettingsParameter<float>("MasterVolume", 1f, v => Mathf.Clamp(v, 0f, 2f), v => AudioListener.volume = v);
        _musicVolume = new SettingsParameter<float>("MusicVolume", 1f, v => Mathf.Clamp(v, 0f, 2f));
        _sfxVolume = new SettingsParameter<float>("SFXVolume", 1f, v => Mathf.Clamp(v, 0f, 2f));
        _uiVolume = new SettingsParameter<float>("UIVolume", 1f, v => Mathf.Clamp(v, 0f, 2f));
        _screenResolution = new IndexedParameter("ScreenResolution", resLabels, defaultResIndex, _ => ApplyScreenSettings());
        _screenMode = new IndexedParameter("ScreenMode", ScreenModeOptions, 0, _ => ApplyScreenSettings());
        _fpsStepIndex = new SettingsParameter<int>("FpsStepIndex", GetClosestFpsIndex(120), v => Mathf.Clamp(v, 0, FpsSteps.Length - 1), v => Application.targetFrameRate = FpsSteps[v]);
        _vSync = new IndexedParameter("VSync", VSyncOptions, 0, ApplyVSync);
        _textureQuality = new IndexedParameter("TextureQuality", QualityOptions, 2, ApplyTextureQuality);
        _shadowQuality = new IndexedParameter("ShadowQuality", QualityOptions, 2, ApplyShadowQuality);
        _outputDevice = new IndexedParameter("OutputDevice", outputDeviceLabels, 0, ApplyOutputDevice);

        BuildParameterRegistry();
        BuildLookup();
        SubscribeAll();
    }

    private void SubscribeAll()
    {
        void Notify() { OnParametersChanged?.Invoke(); }

        _language.OnChanged += Notify;
        _sensitivity.OnChanged += Notify;
        _masterVolume.OnChanged += Notify;
        _musicVolume.OnChanged += Notify;
        _sfxVolume.OnChanged += Notify;
        _uiVolume.OnChanged += Notify;
        _screenResolution.OnChanged += Notify;
        _screenMode.OnChanged += Notify;
        _fpsStepIndex.OnChanged += Notify;
        _vSync.OnChanged += Notify;
        _textureQuality.OnChanged += Notify;
        _shadowQuality.OnChanged += Notify;
        _outputDevice.OnChanged += Notify;
    }

    private string[] BuildResolutionLabels()
    {
        string[] labels = new string[FilteredResolutions.Count];
        for (int i = 0; i < FilteredResolutions.Count; i++)
            labels[i] = FilteredResolutions[i].width + "x" + FilteredResolutions[i].height;
        return labels;
    }

    private static int GetClosestFpsIndex(int target)
    {
        int closest = 0;
        int minDiff = int.MaxValue;
        for (int i = 0; i < FpsSteps.Length; i++)
        {
            int diff = Math.Abs(FpsSteps[i] - target);
            if (diff >= minDiff) continue;
            minDiff = diff;
            closest = i;
        }
        return closest;
    }

    private void LoadSettings()
    {
        SettingsData data = new SettingsData();

        if (File.Exists(SavePath))
        {
            try
            {
                string json = File.ReadAllText(SavePath);
                data = JsonUtility.FromJson<SettingsData>(json) ?? data;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SettingsManager] Failed to load settings: {e.Message}");
            }
        }

        int fpsIdx = Array.IndexOf(FpsSteps, data.fpsLimit);
        if (fpsIdx < 0) fpsIdx = GetClosestFpsIndex(data.fpsLimit);

        int resIdx = Array.FindIndex(BuildResolutionLabels(), r => r == data.screenResolution);

        _language.Set(data.languageIndex, silent: true);
        _sensitivity.Set(data.sensitivity, silent: true);
        _masterVolume.Set(data.masterVolume, silent: true);
        _musicVolume.Set(data.musicVolume, silent: true);
        _sfxVolume.Set(data.sfxVolume, silent: true);
        _uiVolume.Set(data.uiVolume, silent: true);
        _screenResolution.Set(Mathf.Max(0, resIdx), silent: true);
        _screenMode.Set(data.screenModeIndex, silent: true);
        _fpsStepIndex.Set(fpsIdx, silent: true);
        _vSync.Set(data.vSyncIndex, silent: true);
        _textureQuality.Set(data.textureQualityIndex, silent: true);
        _shadowQuality.Set(data.shadowQualityIndex, silent: true);
        _outputDevice.Set(data.outputDeviceIndex, silent: true);
    }

    public void Save()
    {
        SettingsData data = new SettingsData
        {
            languageIndex = _language.Index,
            sensitivity = _sensitivity.Value,
            masterVolume = _masterVolume.Value,
            musicVolume = _musicVolume.Value,
            sfxVolume = _sfxVolume.Value,
            uiVolume = _uiVolume.Value,
            screenResolution = _screenResolution.Current,
            screenModeIndex = _screenMode.Index,
            fpsLimit = FpsSteps[_fpsStepIndex.Value],
            vSyncIndex = _vSync.Index,
            textureQualityIndex = _textureQuality.Index,
            shadowQualityIndex = _shadowQuality.Index,
            outputDeviceIndex = _outputDevice.Index
        };

        try
        {
            File.WriteAllText(SavePath, JsonUtility.ToJson(data, true));
        }
        catch (Exception e)
        {
            Debug.LogError($"[SettingsManager] Failed to save settings: {e.Message}");
        }
    }

    public void PopulateMicrophoneList()
    {
        AllDevices.Clear();
        _coreSystem.getRecordNumDrivers(out int numDrivers, out _);
        if (numDrivers == 0) return;

        for (int i = 0; i < numDrivers; i++)
        {
            _coreSystem.getRecordDriverInfo(i, out string name, 256, out _, out _, out _, out _, out _);
            AllDevices.Add(name);
        }
    }

    public void PopulateOutputDeviceList()
    {
        OutputDevices.Clear();
        _coreSystem.getNumDrivers(out int numDrivers);
        if (numDrivers == 0) return;

        for (int i = 0; i < numDrivers; i++)
        {
            _coreSystem.getDriverInfo(i, out string name, 256, out _, out _, out _, out _);
            OutputDevices.Add(name);
        }
    }

    private void PopulateResolutionList()
    {
        FilteredResolutions.Clear();
        Resolution[] all = Screen.resolutions;
        HashSet<string> seen = new HashSet<string>();

        for (int i = all.Length - 1; i >= 0; i--)
        {
            string key = all[i].width + "x" + all[i].height;
            if (seen.Add(key))
                FilteredResolutions.Add(all[i]);
        }
    }

    public void SetLanguage(int index) => _language.Set(index);
    public void SetLanguageFromDropdown(int index) => _language.Set(index);
    public void SetSensitivityFromSlider(float value) => _sensitivity.Set(value);
    public void SetMasterVolumeFromSlider(float value) => _masterVolume.Set(value);
    public void SetMusicVolumeFromSlider(float value) => _musicVolume.Set(value);
    public void SetSFXVolumeFromSlider(float value) => _sfxVolume.Set(value);
    public void SetUIVolumeFromSlider(float value) => _uiVolume.Set(value);
    public void SetScreenResolution(int index) => _screenResolution.Set(index);
    public void SetScreenResolutionFromDropdown(int index) => _screenResolution.Set(index);
    public void SetScreenMode(int index) => _screenMode.Set(index);
    public void SetScreenModeFromDropdown(int index) => _screenMode.Set(index);

    public void SetFpsLimitFromSlider(float value)
    {
        _fpsStepIndex.Set(Mathf.RoundToInt(value));
    }

    public void StepFpsLimit(int direction)
    {
        int next = (_fpsStepIndex.Value + direction + FpsSteps.Length) % FpsSteps.Length;
        _fpsStepIndex.Set(next);
    }

    public void StepVSync(int direction) => _vSync.Step(direction);
    public void StepLanguage(int direction) => _language.Step(direction);
    public void StepScreenResolution(int direction) => _screenResolution.Step(direction);
    public void StepScreenMode(int direction) => _screenMode.Step(direction);
    public void StepTextureQuality(int direction) => _textureQuality.Step(direction);
    public void StepShadowQuality(int direction) => _shadowQuality.Step(direction);
    public void StepOutputDevice(int direction) => _outputDevice.Step(direction);

    private Dictionary<string, object> _parameterRegistry;
    private Dictionary<string, Func<object>> _parameterLookup;

    private void BuildParameterRegistry()
    {
        _parameterRegistry = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
        {
            { _language.Name,         _language         },
            { _sensitivity.Name,      _sensitivity      },
            { _masterVolume.Name,     _masterVolume     },
            { _musicVolume.Name,      _musicVolume      },
            { _sfxVolume.Name,        _sfxVolume        },
            { _uiVolume.Name,         _uiVolume         },
            { _screenResolution.Name, _screenResolution },
            { _screenMode.Name,       _screenMode       },
            { _fpsStepIndex.Name,     _fpsStepIndex     },
            { _vSync.Name,            _vSync            },
            { _textureQuality.Name,   _textureQuality   },
            { _shadowQuality.Name,    _shadowQuality    },
            { _outputDevice.Name,     _outputDevice     },
        };
    }

    private void BuildLookup()
    {
        _parameterLookup = new Dictionary<string, Func<object>>(StringComparer.OrdinalIgnoreCase)
        {
            { _language.Name,          () => _language.Current          },
            { _sensitivity.Name,       () => _sensitivity.Value         },
            { _masterVolume.Name,      () => _masterVolume.Value        },
            { _musicVolume.Name,       () => _musicVolume.Value         },
            { _sfxVolume.Name,         () => _sfxVolume.Value           },
            { _uiVolume.Name,          () => _uiVolume.Value            },
            { _screenResolution.Name,  () => _screenResolution.Current  },
            { _screenMode.Name,        () => _screenMode.Current        },
            { _fpsStepIndex.Name,      () => (object)FpsLimit           },
            { _vSync.Name,             () => _vSync.Current             },
            { _textureQuality.Name,    () => _textureQuality.Current    },
            { _shadowQuality.Name,     () => _shadowQuality.Current     },
            { _outputDevice.Name,      () => _outputDevice.Current      },
        };
    }

    private void ApplyScreenSettings()
    {
        if (FilteredResolutions.Count == 0) return;

        string[] parts = _screenResolution.Current.Split('x');
        if (parts.Length != 2) return;
        if (!int.TryParse(parts[0], out int w) || !int.TryParse(parts[1], out int h)) return;

        FullScreenMode mode = _screenMode.Index switch
        {
            1 => FullScreenMode.Windowed,
            2 => FullScreenMode.FullScreenWindow,
            _ => FullScreenMode.ExclusiveFullScreen
        };

        Screen.SetResolution(w, h, mode);
    }

    private void ApplyVSync(int index)
    {
        QualitySettings.vSyncCount = index;
    }

    private void ApplyTextureQuality(int index)
    {
        QualitySettings.globalTextureMipmapLimit = QualityOptions.Length - 1 - index;
    }

    private void ApplyShadowQuality(int index)
    {
        QualitySettings.shadowResolution = index switch
        {
            0 => UnityEngine.ShadowResolution.Low,
            1 => UnityEngine.ShadowResolution.Medium,
            _ => UnityEngine.ShadowResolution.High
        };
        QualitySettings.shadows = index == 0 ? UnityEngine.ShadowQuality.HardOnly : UnityEngine.ShadowQuality.All;
    }

    private void ApplyOutputDevice(int index)
    {
        if (OutputDevices.Count == 0) return;
        _coreSystem.setDriver(index);
    }

    private void ApplyAllSettings()
    {
        AudioListener.volume = _masterVolume.Value;
        QualitySettings.vSyncCount = _vSync.Index;
        Application.targetFrameRate = FpsSteps[_fpsStepIndex.Value];
        ApplyTextureQuality(_textureQuality.Index);
        ApplyShadowQuality(_shadowQuality.Index);
        ApplyScreenSettings();
        if (OutputDevices.Count > 0) _coreSystem.setDriver(_outputDevice.Index);
    }

    public void ResetToDefaults()
    {
        _language.Reset(silent: true);
        _sensitivity.Reset(silent: true);
        _masterVolume.Reset(silent: true);
        _musicVolume.Reset(silent: true);
        _sfxVolume.Reset(silent: true);
        _uiVolume.Reset(silent: true);
        _screenResolution.Reset(silent: true);
        _screenMode.Reset(silent: true);
        _fpsStepIndex.Reset(silent: true);
        _vSync.Reset(silent: true);
        _textureQuality.Reset(silent: true);
        _shadowQuality.Reset(silent: true);
        _outputDevice.Reset(silent: true);

        ApplyAllSettings();
        Save();

        _language.ForceNotify();
        _sensitivity.ForceNotify();
        _masterVolume.ForceNotify();
        _musicVolume.ForceNotify();
        _sfxVolume.ForceNotify();
        _uiVolume.ForceNotify();
        _screenResolution.ForceNotify();
        _screenMode.ForceNotify();
        _fpsStepIndex.ForceNotify();
        _vSync.ForceNotify();
        _textureQuality.ForceNotify();
        _shadowQuality.ForceNotify();
        _outputDevice.ForceNotify();

        OnParametersChanged?.Invoke();
    }

    public T GetParameter<T>(string name) where T : class
    {
        if (_parameterRegistry == null) BuildParameterRegistry();
        return _parameterRegistry.TryGetValue(name, out object param) ? param as T : null;
    }

    public T GetParametersValue<T>(string name)
    {
        if (_parameterLookup == null) BuildLookup();
        if (!_parameterLookup.TryGetValue(name, out Func<object> getter)) return default;
        object result = getter();
        if (result is T value) return value;
        try { return (T)Convert.ChangeType(result, typeof(T)); }
        catch { return default; }
    }
}
