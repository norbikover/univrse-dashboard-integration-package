using UnityEngine;

namespace UniVRseDashboardIntegration
{
    public class LicenseDemoCanvasStateManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameObject _canvas;

        private void Start()
        {
            _canvas.SetActive(LicenseStaticReferences.LicenseEnvironment == ELicenseEnvironment.BETA);
        }
    }
}