using UnityEngine;

public class ResourcePoint : MonoBehaviour
{
    [SerializeField] private int _maxResources = 100;
    private int _currentResources;
    private Renderer _renderer;

    private void OnEnable() => Unit.AllResources.Add(this);
    private void OnDisable() => Unit.AllResources.Remove(this);

    private void Awake()
    {
        _currentResources = _maxResources;
        _renderer = GetComponent<Renderer>();
    }

    public bool HasResources => _currentResources > 0;

    public int Gather(int amount)
    {
        if (_currentResources <= 0)
            return 0;

        var gathered = Mathf.Min(amount, _currentResources);
        _currentResources -= gathered;

        if (_currentResources <= 0 && _renderer != null)
        {
            _renderer.material.color = Color.gray;
        }

        return gathered;
    }

    public float ResourcePercent => (float)_currentResources / _maxResources;
}