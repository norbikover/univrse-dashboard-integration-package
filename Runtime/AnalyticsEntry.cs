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
        public string createdAt; // ISO 8601 UTC, e.g. "2026-05-27T10:30:00.000Z"
        public string updatedAt; // ISO 8601 UTC, e.g. "2026-05-27T10:30:00.000Z"
        public Dictionary<string, object> data;

        public AnalyticsEntry(string licenseCode, string deviceId, float totalTime, string version, DateTime createdAt, DateTime updatedAt, Dictionary<string, object> data = null)
        {
            this.licenseCode = licenseCode;
            this.deviceId = deviceId;
            this.totalTime = totalTime;
            this.version = version;
            this.createdAt = createdAt.ToString("o"); // ISO 8601 UTC
            this.updatedAt = updatedAt.ToString("o"); // ISO 8601 UTC
            this.data = data;
        }
    }
}
