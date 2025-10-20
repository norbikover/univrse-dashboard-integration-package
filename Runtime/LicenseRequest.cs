namespace UniVRseDashboardIntegration
{
    public class LicenseRequest
    {
        public string licenseCode;
        public string appId;
        public string appVersion;

        public LicenseRequest(string licenseCode, string appId, string appVersion)
        {
            this.licenseCode = licenseCode;
            this.appId = appId;
            this.appVersion = appVersion;
        }
    }
}