using UnityEditor.SearchService;

namespace UniVRseDashboardIntegration
{
    public class LicenseMessageOld // This is the license message that the server constantly sends to clients.
    {
        private string _sceneName;
        private ELicenseEnvironment _environment; // Tell the client what environment to run.
        private string _appVersion; // Tell the client what the server app version is.

        public LicenseMessageOld(string sceneName, ELicenseEnvironment environment, string appVersion)
        {
            _sceneName = sceneName;
            _environment = environment;
            _appVersion = appVersion;
        }

        #region Getters
        public string SceneName { get { return _sceneName; } }
        public ELicenseEnvironment Environment { get { return _environment; } }
        public string AppVersion { get { return _appVersion; } }
        #endregion
    }
}