using UnityEngine;
using UnityEngine.UI;

public class AudioMixerController : MonoBehaviour
{
    [Header("Sliders")] [SerializeField] private Slider _masterSlider;
    [SerializeField] private Slider _sfxSlider;
    [SerializeField] private Slider _musicSlider;

    private void Start()
    {
        if (_masterSlider != null)
        {
            _masterSlider.value = SettingsManager.GetMasterVolume();
            _masterSlider.onValueChanged.AddListener(OnMasterChanged);
        }

        if (_sfxSlider != null)
        {
            _sfxSlider.value = SettingsManager.GetSFXVolume();
            _sfxSlider.onValueChanged.AddListener(OnSFXChanged);
        }

        if (_musicSlider != null)
        {
            _musicSlider.value = SettingsManager.GetMusicVolume();
            _musicSlider.onValueChanged.AddListener(OnMusicChanged);
        }
    }

    private void OnMasterChanged(float value)
    {
        SettingsManager.SetMasterVolume(value);
        if (AudioManager.Instance != null)
            AudioManager.Instance.ApplyVolumeSettings();
    }

    private void OnSFXChanged(float value)
    {
        SettingsManager.SetSFXVolume(value);
        if (AudioManager.Instance != null)
            AudioManager.Instance.ApplyVolumeSettings();
    }

    private void OnMusicChanged(float value)
    {
        SettingsManager.SetMusicVolume(value);
        if (AudioManager.Instance != null)
            AudioManager.Instance.ApplyVolumeSettings();
    }
}