using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    [Header("UI Text")]
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text linesText;
    [SerializeField] private TMP_Text gameOverScoreText;

    [Header("UI Panels")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private Button restartButton;

    private void Awake()
    {
        HideGameOverPanel();
    }

    private void Start()
    {
        if (restartButton != null)
            restartButton.onClick.AddListener(() => GameManager.Instance.RestartGame());
    }

    private void HideGameOverPanel()
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
    }

    public void UpdateScore(int score, int linesCleared)
    {
        if (scoreText != null)
            scoreText.text = $"Score: {score}";

        if (linesText != null)
            linesText.text = $"Lines: {linesCleared}";
    }

    public void ShowGameOver(int score, int linesCleared)
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);

        if (gameOverScoreText != null)
        {
            gameOverScoreText.text =
                $"Game Over!\n\nScore: {score}\nLines: {linesCleared}";
        }
    }
}