using UnityEngine;

public class Door : InteractableBase
{
    [Header("Door Settings")]
    [SerializeField] private string _requiredKeyId = "red_key";
    [SerializeField] private GameObject _openedDoorVisual;
    [SerializeField] private BoxCollider2D _doorCollider;

    private bool _isOpen;

    public override void Interact(GameObject interactor)
    {
        if (_isOpen || !_canInteract) return;

        if (InventoryManager.Instance != null &&
            InventoryManager.Instance.HasKey(_requiredKeyId))
        {
            OpenDoor();
        }
        else
        {
            _promptText = $"Need {_requiredKeyId}";
        }
    }

    private void OpenDoor()
    {
        _isOpen = true;
        _canInteract = false;

        if (_doorCollider != null)
            _doorCollider.enabled = false;

        if (_openedDoorVisual != null)
            _openedDoorVisual.SetActive(true);

        if (_spriteRenderer != null)
            _spriteRenderer.enabled = false;

        AudioManager.Instance?.PlayItemPickup();

        _promptText = "";
    }

    public override void OnPlayerEnter()
    {
        if (!_isOpen)
            base.OnPlayerEnter();
    }
}
