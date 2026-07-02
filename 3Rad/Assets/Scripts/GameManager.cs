using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] private Board _board;
    [SerializeField] private ScoreManager _scoreManager;
    [SerializeField] private UIManager _uiManager;
    [SerializeField] private AudioManager _audioManager;
    [SerializeField] private int _maxMoves = 30;

    private int _score;
    private int _movesLeft;
    private Tile _selectedTile;
    private bool _isGameOver;

    private void Start()
    {
        _movesLeft = _maxMoves;
        _score = 0;
        _uiManager.UpdateScore(_score);
        _uiManager.UpdateMoves(_movesLeft);
        _uiManager.HideGameOver();
    }

    public void OnTileClicked(Tile tile)
    {
        
        if (_isGameOver || _board.IsProcessing) return;

        if (_selectedTile == null)
        {
            _selectedTile = tile;
            _selectedTile.SetHighlight(true);
        }
        else if (_selectedTile == tile)
        {
            _selectedTile.SetHighlight(false);
            _selectedTile = null;
        }
        else if (_board.IsAdjacent(_selectedTile, tile))
        {
            Tile first = _selectedTile;
            first.SetHighlight(false);
            _selectedTile = null;
            _board.TrySwap(first, tile);
        }
        else
        {
            _selectedTile.SetHighlight(false);
            _selectedTile = tile;
            _selectedTile.SetHighlight(true);
        }
    }

    public void AddScore(int tilesMatched)
    {
        _scoreManager.AddScore(tilesMatched);
        _score = _scoreManager.Score;
        _uiManager.UpdateScore(_score);
        _audioManager.PlayMatch();
    }

    public void IncrementCombo()
    {
        _scoreManager.AddComboMultiplier();
    }

    public void ResetCombo()
    {
        _scoreManager.ResetCombo();
    }

    public void OnMoveCompleted()
    {
        _movesLeft--;
        _uiManager.UpdateMoves(_movesLeft);

        if (_movesLeft <= 0)
        {
            GameOver();
        }
    }

    private void GameOver()
    {
        _isGameOver = true;
        _uiManager.ShowGameOver(_score);
    }

    public void RestartGame()
    {
        _isGameOver = false;
        _movesLeft = _maxMoves;
        _score = 0;
        _selectedTile = null;

        _scoreManager.ResetScore();
        _uiManager.UpdateScore(_score);
        _uiManager.UpdateMoves(_movesLeft);
        _uiManager.HideGameOver();

        _board.ShuffleBoard();
    }
}
