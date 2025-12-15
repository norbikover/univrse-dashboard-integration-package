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
        public string version;
        public Dictionary<string, object> data;

        public AnalyticsEntry(string licenseCode, string deviceId, float totalTime, string version, Dictionary<string, object> data = null)
        {
            this.licenseCode = licenseCode;
            this.deviceId = deviceId;
            this.totalTime = totalTime;
            this.version = version;
            this.data = data;
        }
    }
}
