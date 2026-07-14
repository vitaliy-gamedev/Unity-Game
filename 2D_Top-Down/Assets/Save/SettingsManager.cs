using UnityEngine;

public static class SettingsManager
{
    private const string MasterVolumeKey = "MasterVolume";
    private const string SFXVolumeKey = "SFXVolume";
    private const string MusicVolumeKey = "MusicVolume";
    private const string FullscreenKey = "Fullscreen";
    private const string ResolutionWidthKey = "ResolutionWidth";
    private const string ResolutionHeightKey = "ResolutionHeight";

    private const float DefaultVolume = 0.75f;
    private const int DefaultWidth = 1920;
    private const int DefaultHeight = 1080;

    public static void LoadAndApplySettings()
    {
        ApplyFullscreen(GetFullscreen());
        ApplyResolution(GetResolutionWidth(), GetResolutionHeight());
    }

    public static float GetMasterVolume()
    {
        return PlayerPrefs.GetFloat(MasterVolumeKey, DefaultVolume);
    }

    public static void SetMasterVolume(float value)
    {
        PlayerPrefs.SetFloat(MasterVolumeKey, Mathf.Clamp01(value));
        PlayerPrefs.Save();
    }

    public static float GetSFXVolume()
    {
        return PlayerPrefs.GetFloat(SFXVolumeKey, DefaultVolume);
    }

    public static void SetSFXVolume(float value)
    {
        PlayerPrefs.SetFloat(SFXVolumeKey, Mathf.Clamp01(value));
        PlayerPrefs.Save();
    }

    public static float GetMusicVolume()
    {
        return PlayerPrefs.GetFloat(MusicVolumeKey, DefaultVolume);
    }

    public static void SetMusicVolume(float value)
    {
        PlayerPrefs.SetFloat(MusicVolumeKey, Mathf.Clamp01(value));
        PlayerPrefs.Save();
    }

    public static bool GetFullscreen()
    {
        return PlayerPrefs.GetInt(FullscreenKey, 1) == 1;
    }

    public static void SetFullscreen(bool value)
    {
        PlayerPrefs.SetInt(FullscreenKey, value ? 1 : 0);
        PlayerPrefs.Save();
        ApplyFullscreen(value);
    }

    private static void ApplyFullscreen(bool fullscreen)
    {
        Screen.fullScreen = fullscreen;
    }

    public static int GetResolutionWidth()
    {
        return PlayerPrefs.GetInt(ResolutionWidthKey, DefaultWidth);
    }

    public static int GetResolutionHeight()
    {
        return PlayerPrefs.GetInt(ResolutionHeightKey, DefaultHeight);
    }

    public static void SetResolution(int width, int height)
    {
        PlayerPrefs.SetInt(ResolutionWidthKey, width);
        PlayerPrefs.SetInt(ResolutionHeightKey, height);
        PlayerPrefs.Save();
        ApplyResolution(width, height);
    }

    private static void ApplyResolution(int width, int height)
    {
        if (width > 0 && height > 0)
        {
            Screen.SetResolution(width, height, Screen.fullScreen);
        }
    }
}
