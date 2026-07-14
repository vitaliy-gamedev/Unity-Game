using UnityEngine;

[RequireComponent(typeof(InputReader))]
public class PlayerInteraction : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private PlayerConfigSO _config;

    [Header("Interaction Settings")]
    [SerializeField] private Transform _interactionPoint;
    [SerializeField] private float _interactionRadius = 0.5f;
    [SerializeField] private LayerMask _interactableLayer = -1;

    private InputReader _inputReader;
    private InteractableBase _currentInteractable;
    private InteractionPromptUI _promptUI;

    private void Awake()
    {
        _inputReader = GetComponent<InputReader>();
        _promptUI = FindObjectOfType<InteractionPromptUI>();
    }

    private void OnEnable()
    {
        _inputReader.OnInteract += PerformInteraction;
    }

    private void OnDisable()
    {
        _inputReader.OnInteract -= PerformInteraction;
    }

    private void Update()
    {
        DetectInteractable();
    }

    private void DetectInteractable()
    {
        Vector3 origin = _interactionPoint != null
            ? _interactionPoint.position
            : transform.position;

        Collider2D hit = Physics2D.OverlapCircle(origin, _interactionRadius, _interactableLayer);

        InteractableBase interactable = hit != null
            ? hit.GetComponent<InteractableBase>()
            : null;

        if (_currentInteractable != interactable)
        {
            if (_currentInteractable != null)
                _currentInteractable.OnPlayerExit();

            _currentInteractable = interactable;

            if (_currentInteractable != null)
                _currentInteractable.OnPlayerEnter();
        }

        if (_promptUI != null)
        {
            bool canInteract = _currentInteractable != null && _currentInteractable.CanInteract;
            string text = canInteract ? _currentInteractable.PromptText : "";
            _promptUI.ShowPrompt(canInteract, text);
        }
    }

    private void PerformInteraction()
    {
        if (_currentInteractable != null && _currentInteractable.CanInteract)
        {
            _currentInteractable.Interact(gameObject);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 origin = _interactionPoint != null
            ? _interactionPoint.position
            : transform.position;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(origin, _interactionRadius);
    }
}
