using System;
using System.Collections.Generic;

[Serializable]
public class SaveData
{
    public int maxUnlockedLevel = 1;
    public List<int> completedLevels = new List<int>();
    public int totalScore = 0;

    public bool IsLevelCompleted(int levelNumber)
    {
        return completedLevels.Contains(levelNumber);
    }

    public void CompleteLevel(int levelNumber)
    {
        if (!completedLevels.Contains(levelNumber))
        {
            completedLevels.Add(levelNumber);
        }

        if (levelNumber >= maxUnlockedLevel)
        {
            maxUnlockedLevel = levelNumber + 1;
        }
    }
}
