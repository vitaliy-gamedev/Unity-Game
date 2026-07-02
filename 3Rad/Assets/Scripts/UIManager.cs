using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _scoreText;
    [SerializeField] private TextMeshProUGUI _movesText;
    [SerializeField] private GameObject _gameOverPanel;
    [SerializeField] private TextMeshProUGUI _finalScoreText;

    public void UpdateScore(int score)
    {
        _scoreText.text = $"Score: {score}";
    }

    public void UpdateMoves(int moves)
    {
        _movesText.text = $"Moves: {moves}";
    }

    public void ShowGameOver(int finalScore)
    {
        _gameOverPanel.SetActive(true);
        _finalScoreText.text = $"Final Score: {finalScore}";
    }

    public void HideGameOver()
    {
        _gameOverPanel.SetActive(false);
    }
}
