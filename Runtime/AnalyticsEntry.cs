using System;
using System.Collections.Generic;

namespace UniVRseDashboardIntegration
{
    [Serializable]
    public class AnalyticsEntry
    {
        public string licenseCode;
        public string deviceId;
        public float totalTime;
        public Dictionary<string, object> data;

        public AnalyticsEntry(string licenseCode, string deviceId, float totalTime, Dictionary<string, object> data = null)
        {
            this.licenseCode = licenseCode;
            this.deviceId = deviceId;
            this.totalTime = totalTime;
            this.data = data;
        }
    }
}
