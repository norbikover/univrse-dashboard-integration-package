using UnityEngine;

namespace UniVRseDashboardIntegration
{
    public class Constants : MonoBehaviour
    {
        #region Singleton pattern
        private static Constants _instance;

        public static Constants Instance
        {
            get
            {
                return _instance != null ? _instance : _instance = FindAnyObjectByType<Constants>();
            }
        }

        #endregion

        [Header("Dashboard Integration")]
        [SerializeField] private string _appId;
        [SerializeField] private string _apiEndpoint = "https://xtended.vercel.app/api";

        [Header("Values")]
        [SerializeField] private int _udpLicensePort = 9876;
        
        [Header("PlayerPrefs Keys")]
        [SerializeField] private string _licenseCodeKey = "LICENSE_CODE";

        [Header("Secrets")]
        [SerializeField] private string _secretLicense = "xr123!";

        #region Getters
        public static string APP_ID { get { return Instance._appId; } }
        public static string API_ENDPOINT { get { return Instance._apiEndpoint; } }
        public static int UDP_LICENSE_PORT { get { return Instance._udpLicensePort; } }
        public static string LICENSE_CODE_KEY {get {return Instance._licenseCodeKey;}}
        public static string SECRET_LICENSE {get {return Instance._secretLicense;}}
        #endregion
    }
}
