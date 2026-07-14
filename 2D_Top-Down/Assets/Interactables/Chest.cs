using UnityEngine;

public class Chest : InteractableBase
{
    [Header("Chest Settings")]
    [SerializeField] private int _scoreReward = 100;
    [SerializeField] private GameObject _openVisual;

    private bool _isOpen;

    public override void Interact(GameObject interactor)
    {
        if (_isOpen || !_canInteract) return;

        _isOpen = true;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.CurrentScore += _scoreReward;
        }

        if (_openVisual != null)
        {
            _openVisual.SetActive(true);
        }

        if (_spriteRenderer != null)
        {
            _spriteRenderer.enabled = false;
        }

        AudioManager.Instance?.PlayItemPickup();
        VFXManager.Instance?.PlayItemPickup(transform.position);

        _canInteract = false;
    }
}
