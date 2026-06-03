using UnityEngine;

public class Builder : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera _mainCamera; // Головна камера (звідки пускаємо промінь)
    [SerializeField] private GameObject _buildIndicator; // Напівпрозорий індикатор споруди

    [Header("Prefabs to Build")]
    [SerializeField] private GameObject _prefab1; // Об'єкт на клавішу 1
    [SerializeField] private GameObject _prefab2; // Об'єкт на клавішу 2
    [SerializeField] private GameObject _prefab3; // Об'єкт на клавішу 3

    [Header("Settings")]
    [SerializeField] private float _buildDistance = 10f; // Максимальна дальльність будівництва
    [SerializeField] private LayerMask _buildableLayer; // Шар, на якому можна будувати (наприклад, Ground)

    private GameObject _selectedPrefab;
    private bool _isBuildingMode = false;

    private void Start()
    {
        // Якщо камеру не призначили, беремо головну
        if (_mainCamera == null) _mainCamera = Camera.main;

        // Спочатку вимикаємо індикатор
        if (_buildIndicator != null) _buildIndicator.SetActive(false);
    }

    private void Update()
    {
        HandleInput();

        if (_isBuildingMode)
        {
            UpdateIndicatorPosition();

            // Будуємо на Ліве Клацання Миші (ЛКМ)
            if (Input.GetMouseButtonDown(0) && _selectedPrefab != null)
            {
                BuildObject();
            }
        }
    }

    // 1. Обробка натискання клавіш 1, 2, 3 та скасування на Escape
    private void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            SelectStructure(_prefab1);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            SelectStructure(_prefab2);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            SelectStructure(_prefab3);
        }

        // Вихід з режиму будівництва на Escape
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ExitBuildingMode();
        }
    }

    private void SelectStructure(GameObject prefab)
    {
        if (prefab == null) return;

        _selectedPrefab = prefab;
        _isBuildingMode = true;

        if (_buildIndicator != null) _buildIndicator.SetActive(true);
    }

    // 2. Логіка переміщення індикатора за прицілом із виправленням висоти (офсет 0.5f)
    private void UpdateIndicatorPosition()
    {
        if (_buildIndicator == null || _mainCamera == null) return;

        // Пускаємо промінь чітко через центр екрана (де твій приціл)
        Ray ray = _mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, _buildDistance, _buildableLayer))
        {
            // Умикаємо індикатор, якщо він був схований
            if (!_buildIndicator.activeSelf) _buildIndicator.SetActive(true);

            // Виправляємо занурення в землю: піднімаємо точку спавну на половину висоти стандартного куба (0.5m)
            float yOffset = 0.5f;
            Vector3 spawnPosition = hit.point + new Vector3(0f, yOffset, 0f);

            // Ставимо індикатор у підняту позицію
            _buildIndicator.transform.position = spawnPosition;

            // Поворот залишаємо рівним, щоб блоки завжди ставали прямо, а не косо
            _buildIndicator.transform.rotation = Quaternion.identity;
        }
        else
        {
            // Якщо дивимося в небо або занадто далеко — ховаємо індикатор
            _buildIndicator.SetActive(false);
        }
    }

    // 3. Спавн реального об'єкта
    private void BuildObject()
    {
        // Будуємо тільки якщо індикатор активний (бачить правильну поверхню)
        if (_buildIndicator.activeSelf)
        {
            // Спавнить об'єкт точно в позиції індикатора (яка вже піднята на 0.5m) і з рівним поворотом
            Instantiate(_selectedPrefab, _buildIndicator.transform.position, _buildIndicator.transform.rotation);

            // Якщо хочеш, щоб після одного будівництва режим вимикався — розкоментуй рядок нижче:
            // ExitBuildingMode();
        }
    }

    private void ExitBuildingMode()
    {
        _isBuildingMode = false;
        _selectedPrefab = null;
        if (_buildIndicator != null) _buildIndicator.SetActive(false);
    }
}