using System.Collections.Generic;

namespace GameFoundation.Pro.Analytics
{
    public interface IAnalyticsService
    {
        void LogEvent(string eventName, Dictionary<string, object> parameters = null);
        void LogScreenView(string screenName);
    }
}
