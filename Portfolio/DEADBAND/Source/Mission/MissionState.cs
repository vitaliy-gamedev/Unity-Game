namespace Deadband.Missions
{
    public enum MissionState
    {
        TraceSignal,
        LocateRescue,
        ExtractionAvailable,
        Extracting,
        Completed,
        Failed
    }

    public enum RunResult
    {
        None,
        FullExtraction,
        PartialExtraction,
        FailedExtraction
    }
}
