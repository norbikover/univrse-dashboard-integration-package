using UnityEngine;
using Mirror;

namespace UniVRseDashboardIntegration
{
    public class NetworkLicenseStateManager : NetworkBehaviour
    {
        [Header("References")]
        [SerializeField] private GameObject _demoCanvas;

        // SyncVars.
        [SyncVar(hook = nameof(OnLicenseEnvironmentChanged))] private ELicenseEnvironment _licenseEnvironment;

        private void OnLicenseEnvironmentChanged(ELicenseEnvironment oldValue, ELicenseEnvironment newValue) // Called on both clients (hook) and serverOnly (manually).
        {
            LicenseStaticReferences.LicenseEnvironment = _licenseEnvironment; // Update static reference.
            if(_demoCanvas != null) _demoCanvas.SetActive(_licenseEnvironment == ELicenseEnvironment.DEMO);
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            SetLicenseEnvironment(LicenseStaticReferences.LicenseEnvironment);
        }

        [Server]
        private void SetLicenseEnvironment(ELicenseEnvironment newValue)
        {
            if(_licenseEnvironment == newValue) return;

            ELicenseEnvironment oldValue = _licenseEnvironment;
            _licenseEnvironment = newValue;
            if(isServerOnly) OnLicenseEnvironmentChanged(oldValue, newValue);
        }
    }
}