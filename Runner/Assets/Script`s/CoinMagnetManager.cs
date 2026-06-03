using UnityEngine;

public class CoinMagnetManager : MonoBehaviour
{
    public static CoinMagnetManager Instance;

    private bool isActive = false;
    private float timer;

    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        if (!isActive) return;

        timer -= Time.deltaTime;

        if (timer <= 0)
        {
            isActive = false;
        }
    }

    public void ActivateMagnet(float duration)
    {
        isActive = true;
        timer = duration;
    }

    public bool IsMagnetActive()
    {
        return isActive;
    }
}