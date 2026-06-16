using UnityEngine;

public class ResourceProducer : MonoBehaviour
{
    [Tooltip("Який ресурс виробляє будівля")]
    [SerializeField] private ResourceType resourceType = ResourceType.Gold;

    [Tooltip("Скільки додавати за один тік")]
    [SerializeField] private int amountPerTick = 1;

    [Tooltip("Інтервал між тіками в секундах")]
    [SerializeField] private float secondsPerTick = 2f;

    private float _timer;

    private void Update()
    {
        if (ResourceManager.Instance == null) return;

        _timer += Time.deltaTime;
        while (_timer >= secondsPerTick)
        {
            _timer -= secondsPerTick;
            ResourceManager.Instance.Add(resourceType, amountPerTick);
        }
    }
}
