using System;
using UnityEngine;

namespace UniVRseDashboardIntegration
{
    public static class Utils
    {
        public static bool IsWithinOfflineGracePeriod(string license)
        {
            if(!PlayerPrefs.HasKey(Constants.LICENSE_CODE_KEY))
                return false;

            if(!string.Equals(PlayerPrefs.GetString(Constants.LICENSE_CODE_KEY), license))
                return false;

            if (!PlayerPrefs.HasKey(Constants.LAST_VALIDATION_TIME_KEY))
                return false;

            if (!DateTime.TryParse(PlayerPrefs.GetString(Constants.LAST_VALIDATION_TIME_KEY), out DateTime lastValidation))
                return false;

            TimeSpan timeSinceValidation = DateTime.UtcNow - lastValidation;
            int gracePeriodDays = Constants.OFFLINE_GRACE_PERIOD_DAYS;

            return timeSinceValidation.TotalDays < gracePeriodDays;
        }
    }
}