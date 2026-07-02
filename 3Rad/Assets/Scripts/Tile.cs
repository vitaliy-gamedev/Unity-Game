using UnityEngine;
using System.Collections;

public class Tile : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _spriteRenderer;
    [SerializeField] private SpriteRenderer _highlightRenderer;
    [SerializeField] private ParticleSystem _destroyEffect;

    private int _column, _row;
    private TileType _type;
    private Board _board;
    private Vector2 _targetPosition;
    private bool _isMoving;
    private float _moveSpeed = 15f; // Трохи швидше для кращого відгуку

    public int Column => _column;
    public int Row => _row;
    public TileType Type => _type;
    public bool IsMoving => _isMoving;

    public event System.Action<Tile> Clicked;

    public void Initialize(Board board, int column, int row, TileType type, Sprite sprite, Color color)
    {
        _board = board;
        _column = column;
        _row = row;
        _type = type;
        _spriteRenderer.sprite = sprite;
        _spriteRenderer.color = color;
        _targetPosition = transform.position;
        SetHighlight(false);
    }

    public void SetGridPosition(int column, int row) { _column = column; _row = row; }

    public void MoveToPosition(Vector2 position)
    {
        _targetPosition = position;
        StopAllCoroutines();
        StartCoroutine(MoveCoroutine());
    }

    private IEnumerator MoveCoroutine()
    {
        _isMoving = true;
        while (Vector2.Distance(transform.position, _targetPosition) > 0.01f)
        {
            transform.position = Vector2.Lerp(transform.position, _targetPosition, _moveSpeed * Time.deltaTime);
            yield return null;
        }
        transform.position = _targetPosition;
        _isMoving = false;
    }

    public void SetHighlight(bool state)
    {
        if (_highlightRenderer != null)
        {
            // Замість SetActive(state), просто змінюємо прозорість кольору
            Color c = _highlightRenderer.color;
            c.a = state ? 1.0f : 0.0f; // 1 - видно, 0 - прозоро
            _highlightRenderer.color = c;
        }
    }

    public void PlayDestroyEffect()
    {
        if (_destroyEffect != null)
        {
            var effect = Instantiate(_destroyEffect, transform.position, Quaternion.identity);
            Destroy(effect.gameObject, 1f);
        }
    }

    private void OnMouseDown() => Clicked?.Invoke(this);
}