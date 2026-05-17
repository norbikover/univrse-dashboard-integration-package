using UnityEngine;

namespace UniVRseDashboardIntegration
{
    public class Constants : MonoBehaviour
    {
        #region Singleton pattern
        private static Constants _instance;
        public static Constants Instance => _instance != null ? _instance : (_instance = FindAnyObjectByType<Constants>());
        #endregion

        [Header("Dashboard Integration")]
        [SerializeField] private string _appId;
        [SerializeField] private string _apiEndpoint = "https://xtended.vercel.app/api";
        [SerializeField] private string _licenseValidationPostfix = "/license-validation";
        
        [Header("PlayerPrefs Keys")]
        [SerializeField] private string _licenseCodeKey = "LICENSE_CODE";
        [SerializeField] private string _lastValidationTimeKey = "LICENSE_LAST_VALIDATION";
        [SerializeField] private string _lastValidationEnvironmentKey = "LICENSE_LAST_ENVIRONMENT";

        [Header("Offline Grace Period")]
        [SerializeField] private int _offlineGracePeriodDays = 3;

        [Header("Secrets")]
        [SerializeField] private string _secretLicense = "xr123!";

        #region Getters
        public static string APP_ID { get { return Instance._appId; } }
        public static string API_ENDPOINT { get { return Instance._apiEndpoint; } }
        public static string LICENSE_VALIDATION_POSTFIX { get { return Instance._licenseValidationPostfix; } }
        public static string LICENSE_CODE_KEY {get {return Instance._licenseCodeKey;}}
        public static string LAST_VALIDATION_TIME_KEY {get {return Instance._lastValidationTimeKey;}}
        public static string LAST_VALIDATION_ENVIRONMENT_KEY {get {return Instance._lastValidationEnvironmentKey;}}
        public static int OFFLINE_GRACE_PERIOD_DAYS {get {return Instance._offlineGracePeriodDays;}}
        public static string SECRET_LICENSE {get {return Instance._secretLicense;}}
        #endregion
    }
}
