namespace UniVRseDashboardIntegration
{
    public class LicenseResponse
    {
        private string _environment;

        public LicenseResponse(string environment)
        {
            _environment = environment;
        }

        #region Getters
        public string Environment { get { return _environment; } }
        #endregion
    }
}