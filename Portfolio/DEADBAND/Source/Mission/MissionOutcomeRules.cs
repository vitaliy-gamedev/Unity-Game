namespace Deadband.Missions
{
    public static class MissionOutcomeRules
    {
        public static RunResult ResolveExtraction(int livingMembersInside, int originalSquadSize)
        {
            if (livingMembersInside <= 0 || originalSquadSize <= 0)
            {
                return RunResult.FailedExtraction;
            }

            return livingMembersInside >= originalSquadSize
                ? RunResult.FullExtraction
                : RunResult.PartialExtraction;
        }

        public static bool IsVictory(RunResult result)
        {
            return result is RunResult.FullExtraction or RunResult.PartialExtraction;
        }
    }
}
