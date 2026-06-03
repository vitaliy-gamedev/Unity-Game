using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

public enum GameResult { PlayerWon, BotWon, Draw }

public class GridCreater : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject _cellPrefab;
    [SerializeField] private Transform _parent;
    [SerializeField] private Text _statusText;
    [SerializeField] private GameObject _gameOverPanel;

    [Header("Settings")]
    [SerializeField] private int _rows = 3;
    [SerializeField] private int _cols = 3;
    [SerializeField] private float _spacing = 110f;

    private Cell[,] _grid;
    private bool _isGameOver = false;

    private void Start()
    {
        
        if (_gameOverPanel != null) _gameOverPanel.SetActive(false);
        _statusText.text = "";
        CreateGrid();
    }

    private void CreateGrid()
    {
        _grid = new Cell[_rows, _cols];
        float offsetX = (_cols - 1) * _spacing / 2f;
        float offsetY = (_rows - 1) * _spacing / 2f;

        for (int r = 0; r < _rows; r++)
        {
            for (int c = 0; c < _cols; c++)
            {
                GameObject obj = Instantiate(_cellPrefab, _parent);
                obj.SetActive(true);
                obj.GetComponent<RectTransform>().anchoredPosition = new Vector2(c * _spacing - offsetX, -r * _spacing + offsetY);

                Cell cell = obj.GetComponent<Cell>();
                cell.Init(OnPlayerClick);
                _grid[r, c] = cell;
            }
        }
    }

    private void OnPlayerClick(Cell cell)
    {
        if (_isGameOver || cell.IsTaken) return;

        cell.SetSymbol("X");
        if (CheckWinner("X")) EndGame(GameResult.PlayerWon);
        else if (IsBoardFull()) EndGame(GameResult.Draw);
        else
        {
            _isGameOver = true;
            Invoke(nameof(BotMove), 0.5f);
        }
    }

    private void BotMove()
    {
        var freeCells = new List<Cell>();
        foreach (var c in _grid) if (!c.IsTaken) freeCells.Add(c);

        if (freeCells.Count > 0)
        {
            freeCells[Random.Range(0, freeCells.Count)].SetSymbol("O");
            if (CheckWinner("O")) EndGame(GameResult.BotWon);
            else if (IsBoardFull()) EndGame(GameResult.Draw);
            else _isGameOver = false;
        }
    }

    private bool CheckWinner(string s)
    {
        for (int i = 0; i < 3; i++)
        {
            if (Match(_grid[i, 0], _grid[i, 1], _grid[i, 2], s) ||
                Match(_grid[0, i], _grid[1, i], _grid[2, i], s)) return true;
        }
        return Match(_grid[0, 0], _grid[1, 1], _grid[2, 2], s) ||
               Match(_grid[0, 2], _grid[1, 1], _grid[2, 0], s);
    }

    private bool Match(Cell a, Cell b, Cell c, string s)
        => a.GetComponentInChildren<Text>().text == s &&
           b.GetComponentInChildren<Text>().text == s &&
           c.GetComponentInChildren<Text>().text == s;

    private bool IsBoardFull() => _grid.Cast<Cell>().All(c => c.IsTaken);

    private void EndGame(GameResult result)
    {
        _isGameOver = true;

       
        if (_gameOverPanel != null) _gameOverPanel.SetActive(true);

               
        if (_parent != null) _parent.gameObject.SetActive(false);

        switch (result)
        {
            case GameResult.PlayerWon: _statusText.text = "Ви перемогли!"; break;
            case GameResult.BotWon: _statusText.text = "Бот переміг!"; break;
            case GameResult.Draw: _statusText.text = "Нічия!"; break;
        }
    }

    public void RestartGame()
    {
        _isGameOver = false;
        _statusText.text = "";

        
        if (_parent != null) _parent.gameObject.SetActive(true);

        if (_gameOverPanel != null) _gameOverPanel.SetActive(false);

        
        foreach (var c in _grid)
        {
            c.SetSymbol("");
        }
    }
}