using UnityEngine;

public abstract class InteractableBase : MonoBehaviour
{
    [Header("Interaction")]
    [SerializeField] protected string _promptText = "Press E to interact";
    [SerializeField] protected bool _canInteract = true;
    [SerializeField] protected bool _oneTimeUse = true;
    [SerializeField] protected Sprite _highlightSprite;

    protected SpriteRenderer _spriteRenderer;
    protected Sprite _defaultSprite;

    public string PromptText => _promptText;
    public bool CanInteract => _canInteract;

    protected virtual void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        if (_spriteRenderer != null)
            _defaultSprite = _spriteRenderer.sprite;
    }

    public virtual void OnPlayerEnter()
    {
        if (_highlightSprite != null && _spriteRenderer != null)
            _spriteRenderer.sprite = _highlightSprite;
    }

    public virtual void OnPlayerExit()
    {
        if (_spriteRenderer != null)
            _spriteRenderer.sprite = _defaultSprite;
    }

    public abstract void Interact(GameObject interactor);

    protected void Deactivate()
    {
        _canInteract = false;
        OnPlayerExit();

        if (_oneTimeUse)
        {
            gameObject.SetActive(false);
        }
    }
}
