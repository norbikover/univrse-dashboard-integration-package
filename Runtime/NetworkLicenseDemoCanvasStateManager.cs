using UnityEngine;
using Mirror;

namespace UniVRseDashboardIntegration
{
    public class NetworkLicenseDemoCanvasStateManager : NetworkBehaviour
    {
        [Header("References")]
        [SerializeField] private GameObject _canvas;

        // SyncVars.
        [SyncVar(hook = nameof(OnLicenseEnvironmentChanged))] private ELicenseEnvironment _licenseEnvironment;

        private void OnLicenseEnvironmentChanged(ELicenseEnvironment oldValue, ELicenseEnvironment newValue) // Called on both clients (hook) and serverOnly (manually).
        {
            _canvas.SetActive(_licenseEnvironment == ELicenseEnvironment.DEMO);
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