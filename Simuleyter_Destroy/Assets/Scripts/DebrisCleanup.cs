using UnityEngine;

public class DebrisCleanup : MonoBehaviour
{
    private float _lifetime = 5f;
    private bool _destroyOnCleanup = true;
    private float _elapsed = 0f;

    public void Setup(float lifetime, bool destroyOnCleanup)
    {
        _lifetime = lifetime;
        _destroyOnCleanup = destroyOnCleanup;
    }

    private void Update()
    {
        _elapsed += Time.deltaTime;

        if (_elapsed >= _lifetime)
        {
            if (_destroyOnCleanup)
                Destroy(gameObject);
            else
                gameObject.SetActive(false);

            enabled = false;
        }
    }
}
