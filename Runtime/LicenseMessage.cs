namespace UniVRseDashboardIntegration
{
    public class LicenseMessage // This is the license message that the server constantly sends to clients.
    {
        private ELicenseEnvironment _environment; // Tell the client what environment to run.
        private string _appVersion; // Tell the client what the server app version is.

        public LicenseMessage(ELicenseEnvironment environment, string appVersion)
        {
            _environment = environment;
            _appVersion = appVersion;
        }

        #region Getters
        public ELicenseEnvironment Environment { get { return _environment; } }
        public string AppVersion { get { return _appVersion; } }
        #endregion
    }
}