using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Audio Source")]
    [SerializeField] private AudioSource audioSource;

    [Header("Sound Effects")]
    [SerializeField] private AudioClip matchSound;
    [SerializeField] private AudioClip swapSound;
    [SerializeField] private AudioClip failSound;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Play(AudioClip clip)
    {
        if (audioSource == null || clip == null)
            return;

        audioSource.PlayOneShot(clip);
    }

    public void PlayMatch()
    {
        Play(matchSound);
    }

    public void PlaySwap()
    {
        Play(swapSound);
    }

    public void PlayFail()
    {
        Play(failSound);
    }
}