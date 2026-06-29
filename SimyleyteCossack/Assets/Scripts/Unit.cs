using UnityEngine;
using System.Collections.Generic;

public class Unit : MonoBehaviour
{
    // Статичні списки для миттєвого пошуку об'єктів без просідання FPS
    public static List<Unit> AllUnits { get; private set; } = new List<Unit>();
    public static List<Building> AllBuildings { get; private set; } = new List<Building>();
    public static List<ResourcePoint> AllResources { get; private set; } = new List<ResourcePoint>();

    [SerializeField] private float _speed = 5f;
    [SerializeField] private float _stopDistance = 0.3f;
    [SerializeField] private float _separationDistance = 1.2f;
    [SerializeField] private float _separationStrength = 3f;
    [SerializeField] private float _formationSpread = 1.5f;

    [SerializeField] private float _carryCapacity = 10f;
    [SerializeField] private float _gatherRate = 3f;
    [SerializeField] private float _interactRange = 2f;

    private enum UnitState
    {
        Idle,
        Move,
        MoveToResource,
        Gather,
        MoveToBuilding,
        Deposit
    }

    private UnitState _state;
    private Vector3 _targetPosition;
    private bool _isMoving;
    private bool _isSelected;
    private GameObject _selectionIndicator;
    private Material _indicatorMaterial;

    private ResourcePoint _targetResource;
    private Building _targetBuilding;
    private Building _homeBuilding; // Будівля, де юніт з'явився (його "дім")
    private int _carryAmount;
    private float _gatherCooldown;

    private static readonly Collider[] _separationBuffer = new Collider[32];

    private void OnEnable() => AllUnits.Add(this);
    private void OnDisable() => AllUnits.Remove(this);

    private void Awake()
    {
        InitializeSelectionIndicator();
    }

    private void InitializeSelectionIndicator()
    {
        var existing = transform.Find("SelectionIndicator");
        if (existing != null)
        {
            _selectionIndicator = existing.gameObject;
            _selectionIndicator.SetActive(false);
            return;
        }

        _selectionIndicator = new GameObject("SelectionIndicator");
        _selectionIndicator.transform.SetParent(transform);
        _selectionIndicator.transform.localPosition = new Vector3(0, 0.05f, 0);

        var lr = _selectionIndicator.AddComponent<LineRenderer>();
        lr.startWidth = 0.08f;
        lr.endWidth = 0.08f;
        lr.loop = true;

        _indicatorMaterial = new Material(Shader.Find("Sprites/Default"));
        lr.material = _indicatorMaterial;
        lr.startColor = new Color(0, 1, 0, 0.7f);
        lr.endColor = new Color(0, 1, 0, 0.7f);

        var segments = 30;
        var radius = 0.7f;
        lr.positionCount = segments;

        for (int i = 0; i < segments; i++)
        {
            var angle = (float)i / segments * Mathf.PI * 2f;
            lr.SetPosition(i, new Vector3(Mathf.Sin(angle) * radius, 0, Mathf.Cos(angle) * radius));
        }

        _selectionIndicator.SetActive(false);
    }

    private void Update()
    {
        if (GroundBounds.Instance != null)
            transform.position = GroundBounds.Instance.ClampPosition(transform.position);

        switch (_state)
        {
            case UnitState.Idle:
                break;
            case UnitState.Move:
            case UnitState.MoveToResource:
            case UnitState.MoveToBuilding:
                UpdateMovement();
                break;
            case UnitState.Gather:
                UpdateGather();
                break;
            case UnitState.Deposit:
                UpdateDeposit();
                break;
        }
    }

    private void UpdateMovement()
    {
        if (!_isMoving)
        {
            TransitionToNextState();
            return;
        }

        var toTarget = _targetPosition - transform.position;
        toTarget.y = 0;
        var distance = toTarget.magnitude;

        // Зупиняємося на радіусі взаємодії, якщо біжимо до будівлі чи ресурсу
        float currentStopDistance = (_state == UnitState.MoveToResource || _state == UnitState.MoveToBuilding)
            ? _interactRange
            : _stopDistance;

        if (distance <= currentStopDistance)
        {
            _isMoving = false;
            TransitionToNextState();
            return;
        }

        var moveDirection = toTarget / distance;
        var moveVector = moveDirection * (_speed * Time.deltaTime);

        // Рахуємо розділення між юнітами
        var separation = Vector3.zero;
        var nearbyCount = Physics.OverlapSphereNonAlloc(transform.position, _separationDistance, _separationBuffer);

        for (var i = 0; i < nearbyCount; i++)
        {
            var col = _separationBuffer[i];
            if (col.gameObject == gameObject) continue;

            var other = col.GetComponentInParent<Unit>();
            if (other == null) continue;

            var diff = transform.position - other.transform.position;
            diff.y = 0;
            var dist = diff.magnitude;
            if (dist < 0.01f) continue;

            var strength = 1f - (dist / _separationDistance);
            separation += diff.normalized * (strength * _separationStrength * Time.deltaTime);
        }

        var finalMove = moveVector + separation;
        transform.position += finalMove;

        if (finalMove != Vector3.zero)
        {
            var lookDir = new Vector3(finalMove.x, 0, finalMove.z).normalized;
            if (lookDir != Vector3.zero)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), 10f * Time.deltaTime);
        }
    }

    private void TransitionToNextState()
    {
        switch (_state)
        {
            case UnitState.Move: _state = UnitState.Idle; break;
            case UnitState.MoveToResource: _state = UnitState.Gather; break;
            case UnitState.MoveToBuilding: _state = UnitState.Deposit; break;
        }
    }

    private void UpdateGather()
    {
        _gatherCooldown -= Time.deltaTime;
        if (_gatherCooldown > 0) return;

        _gatherCooldown = 1f;

        if (_targetResource == null || !_targetResource.HasResources)
        {
            FindNearestResource();
            if (_targetResource != null)
            {
                _state = UnitState.MoveToResource;
                SetDestination(_targetResource.transform.position);
            }
            else
            {
                _state = UnitState.Idle;
            }
            return;
        }

        var taken = _targetResource.Gather(Mathf.RoundToInt(_gatherRate));
        _carryAmount += taken;

        if (_carryAmount >= _carryCapacity || !_targetResource.HasResources)
        {
            FindNearestBuilding();
            if (_targetBuilding != null)
            {
                _state = UnitState.MoveToBuilding;
                SetDestination(_targetBuilding.transform.position);
            }
            else
            {
                _state = UnitState.Idle;
            }
        }
    }

    private void UpdateDeposit()
    {
        if (_targetBuilding != null)
            _targetBuilding.Deposit(_carryAmount);

        _carryAmount = 0;

        if (_targetResource != null && _targetResource.HasResources)
        {
            _state = UnitState.MoveToResource;
            SetDestination(_targetResource.transform.position);
        }
        else
        {
            FindNearestResource();
            if (_targetResource != null)
            {
                _state = UnitState.MoveToResource;
                SetDestination(_targetResource.transform.position);
            }
            else
            {
                _state = UnitState.Idle;
            }
        }
    }

    private void FindNearestBuilding()
    {
        // Якщо у юніта є свій рідний барак, і він активний — йдемо строго туди
        if (_homeBuilding != null && _homeBuilding.isActiveAndEnabled)
        {
            _targetBuilding = _homeBuilding;
            return;
        }

        // Резервний варіант: якщо дому немає (наприклад, зруйнований), шукаємо найближчу будівлю
        float nearestDist = float.MaxValue;
        _targetBuilding = null;

        foreach (var b in AllBuildings)
        {
            if (b == null || !b.isActiveAndEnabled) continue;

            var dist = Vector3.Distance(transform.position, b.transform.position);
            if (dist < nearestDist)
            {
                nearestDist = dist;
                _targetBuilding = b;
            }
        }
    }

    private void FindNearestResource()
    {
        float nearestDist = float.MaxValue;
        _targetResource = null;

        foreach (var r in AllResources)
        {
            if (r == null || !r.HasResources) continue;

            var dist = Vector3.Distance(transform.position, r.transform.position);
            if (dist < nearestDist)
            {
                nearestDist = dist;
                _targetResource = r;
            }
        }
    }

    private void SetDestination(Vector3 destination)
    {
        _targetPosition = destination;
        _targetPosition.y = transform.position.y;
        _isMoving = true;
    }

    public void MoveToCommand(Vector3 destination)
    {
        _state = UnitState.Move;
        _targetResource = null;
        _targetBuilding = null;

        if (GroundBounds.Instance != null)
            destination = GroundBounds.Instance.ClampPosition(destination);

        var offset = Random.insideUnitCircle * _formationSpread;
        _targetPosition = destination + new Vector3(offset.x, 0, offset.y);
        _targetPosition.y = transform.position.y;

        if (GroundBounds.Instance != null)
            _targetPosition = GroundBounds.Instance.ClampPosition(_targetPosition);

        _isMoving = true;
    }

    public void StartGathering(ResourcePoint resource)
    {
        if (resource == null || !resource.HasResources) return;

        _targetResource = resource;
        _gatherCooldown = 0;
        _state = UnitState.MoveToResource;

        SetDestination(resource.transform.position);
    }

    // Метод для встановлення "рідної" будівлі при спавні
    public void SetHomeBuilding(Building home)
    {
        _homeBuilding = home;
    }

    public void SetSelected(bool selected)
    {
        _isSelected = selected;
        if (_selectionIndicator != null)
            _selectionIndicator.SetActive(selected);
    }

    private void OnDestroy()
    {
        if (_indicatorMaterial != null) Destroy(_indicatorMaterial);
    }
}