using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    private int _score;
    private int _comboMultiplier = 1;
    private int _basePointsPerTile = 10;

    public int Score => _score;

    public void AddScore(int tilesMatched)
    {
        int basePoints = tilesMatched * _basePointsPerTile;
        int multipliedPoints = basePoints * _comboMultiplier;
        _score += multipliedPoints;
    }

    public void AddComboMultiplier()
    {
        _comboMultiplier++;
    }

    public void ResetCombo()
    {
        _comboMultiplier = 1;
    }

    public void ResetScore()
    {
        _score = 0;
        _comboMultiplier = 1;
    }
}
