using UnityEngine;

public static class SettingsKeys
{
    public const string Language = "Language";
    public const string Sensitivity = "Sensitivity";
    public const string FullscreenMode = "Fullscreen";
    public const string ScreenResolution = "Resolution";
    public const string FPSLock = "Fps Lock";
    public const string VSync = "V-Sync";
    public const string TextureQuality = "Texture";
    public const string ShadowQuality = "Shadow";
    public const string MasterVolume = "Master";
    public const string MusicVolume = "Music";
    public const string SFXVolume = "SFX";
    public const string UIVolume = "UI";
    public const string OutputDevice = "OutputDevice";

    public enum AllSettingsName
    {
        Language,
        Sensitivity, 
        FullscreenMode,
        ScreenResolution,
        FPSLock,
        VSync,
        TextureQuality,
        ShadowQuality,
        MasterVolume,
        MusicVolume,
        SFXVolume, 
        UIVolume,
        OutputDevice
    }
}
