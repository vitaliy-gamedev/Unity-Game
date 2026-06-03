using UnityEngine;

public class Builder : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera _camera;
    [SerializeField] private float _buildDistance = 5f;

    [Header("Prefabs")]
    [SerializeField] private GameObject[] _buildPrefabs;

    private int _currentIndex = 0;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.N))
        {
            _currentIndex--;
            if (_currentIndex < 0) _currentIndex = _buildPrefabs.Length - 1;
        }

        if (Input.GetKeyDown(KeyCode.M))
        {
            _currentIndex++;
            if (_currentIndex >= _buildPrefabs.Length) _currentIndex = 0;
        }

        if (Input.GetMouseButtonDown(0))
        {
            PlaceObject();
        }
    }

    private void PlaceObject()
   
    {
        {
            if (_buildPrefabs.Length == 0) return;

            Vector3 pos = transform.position + _camera.transform.forward * 3f;
            Instantiate(_buildPrefabs[_currentIndex], pos, Quaternion.identity);
            Debug.Log("Поставив: " + _buildPrefabs[_currentIndex].name);
        }
    }
}