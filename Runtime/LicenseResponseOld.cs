namespace UniVRseDashboardIntegration
{
    public class LicenseResponseOld
    {
        public string locationId;
        public string environment;

        public LicenseResponseOld(string locationId, string environment)
        {
            this.locationId = locationId;
            this.environment = environment;
        }
    }
}