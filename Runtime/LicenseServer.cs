using UnityEngine;
using TMPro;
using Newtonsoft.Json;
using System;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using HttpIntegration;
using LANHelpers;
using NaughtyAttributes;
using Utilities;

namespace UniVRseDashboardIntegration
{
    public class LicenseServer : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TMP_InputField _licenseField;
        [SerializeField] private TMP_Text _errorText;
        [SerializeField] private Button _validateLicenseButton;
        [SerializeField, Scene] private string _licenseClientScene;
        [SerializeField, Scene] private string _sceneToLoad;

        // Private variables.
        private bool _isCheckingLicense = false;
        private bool _loadingScene = false;
        private string _appVersion = string.Empty;

        // The default behaviour is to automatically switch to license client on Android standalone VR builds.
        protected virtual bool IsAutomaticLicenseClientSwitchAllowed() => PlatformChecks.IsStandaloneVRBuild();

        private void Start()
        {
            if (IsAutomaticLicenseClientSwitchAllowed())
            {
                LoadScene(_licenseClientScene);
                return;
            }

            _appVersion = Application.version;

            // Auto populate the license code.
            if (PlayerPrefs.HasKey(Constants.LICENSE_CODE_KEY))
                _licenseField.text = PlayerPrefs.GetString(Constants.LICENSE_CODE_KEY);

            // Reset the error text.
            _errorText.text = "";

            // Subscribe to the validate button on click event.
            _validateLicenseButton.onClick.AddListener(OnValidateLicenceClicked);
        }

        public async void OnValidateLicenceClicked()
        {
            // Return in case there is an ongoing request.
            if (_isCheckingLicense || _loadingScene) return;

            // Check for the SECRET_LICENSE.
            if (Application.isEditor && string.Equals(_licenseField.text, Constants.SECRET_LICENSE))
            {
                // Set the static license references to DEV values.
                LicenseStaticReferences.LicenseCode = string.Empty;
                LicenseStaticReferences.LicenseEnvironment = ELicenseEnvironment.DEV;

                // Start the license server with the DEV environment.
                LANDiscovery.Instance.StartServer();
                HttpServer.Instance.StartServer();
                HttpServer.Instance.Register("/api/license", async (req) =>
                {
                    LicenseMessage licenseMessage = new LicenseMessage(ELicenseEnvironment.DEV, _appVersion);
                    return new HttpResponse(200, JsonConvert.SerializeObject(licenseMessage));
                });
                LoadScene(_sceneToLoad);
                return;
            }

            // Set the checking license to true and reset the error text.
            _isCheckingLicense = true;
            _errorText.text = "Validating ...";

            try
            {
                // Build the query string from the LicenseRequest object.
                LicenseRequest licenseRequest = new LicenseRequest(_licenseField.text, Constants.APP_ID, _appVersion);

                // Perform the license validation request.
                string responseJson = await HttpService.Instance.SendRequestAsync(
                    postfix: Constants.LICENSE_VALIDATION_POSTFIX,
                    method: HttpMethod.POST,
                    data: licenseRequest,
                    serverUrl: Constants.API_ENDPOINT);

                // Deserialize the response JSON into a LicenseResponse object.
                LicenseResponse licenseResponse = JsonConvert.DeserializeObject<LicenseResponse>(responseJson);

                // Store the used license code.
                LicenseStaticReferences.LicenseCode = _licenseField.text;
                LicenseStaticReferences.LicenseEnvironment = licenseResponse.environment.ToEnum<ELicenseEnvironment>();
                PlayerPrefs.SetString(Constants.LICENSE_CODE_KEY, _licenseField.text); // Store the used license code such that we can autopopulate it next time.       

                // Register the http server path.
                LANDiscovery.Instance.StartServer();
                HttpServer.Instance.StartServer();
                HttpServer.Instance.Register("/api/license", async (req) =>
                {
                    LicenseMessage licenseMessage = new LicenseMessage(licenseResponse.environment.ToEnum<ELicenseEnvironment>(), _appVersion);
                    return new HttpResponse(200, JsonConvert.SerializeObject(licenseMessage));
                });

                // Start the background license validation process.
                LicenseBackgroundValidator.Instance.StartBackgroundLicenseChecking(_licenseField.text);

                // Load the assigned scene.
                LoadScene(_sceneToLoad);
            }
            catch (Exception ex)
            {
                _errorText.text = ex.Message;
            }

            // Set the checking license variable back to false in order to allow other requests.
            _isCheckingLicense = false;
        }

        private void LoadScene(string sceneName)
        {
            if (_loadingScene) return;

            _loadingScene = true;
            SceneManager.LoadSceneAsync(sceneName);
        }
    
        private void OnDestroy()
        {
            // Unsubscribe from the validate button on click event.
            if(_validateLicenseButton != null) _validateLicenseButton.onClick.RemoveListener(OnValidateLicenceClicked);
        }
    }
}