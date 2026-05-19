using UnityEngine;
using TMPro;
using Newtonsoft.Json;
using System;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using HttpIntegration;
using LANHelpers;
using NaughtyAttributes;

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
        private string _appVersion;

        // Overridable properties.
        protected virtual bool SwitchToLicenseClientOnLoad => false;

        private void Start()
        {
            if (SwitchToLicenseClientOnLoad)
            {
                SceneManager.LoadSceneAsync(_licenseClientScene);
                return;
            }

            // Auto populate the license code.
            if (PlayerPrefs.HasKey(Constants.LICENSE_CODE_KEY))
                _licenseField.text = PlayerPrefs.GetString(Constants.LICENSE_CODE_KEY);

            // Store the application version.
            _appVersion = Application.version;

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
                OnLicenseValidated(string.Empty, ELicenseEnvironment.DEV);
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
                
                // Store the license validation data for offline usage.
                PlayerPrefs.SetString(Constants.LICENSE_CODE_KEY, _licenseField.text);
                PlayerPrefs.SetString(Constants.LAST_VALIDATION_TIME_KEY, DateTime.UtcNow.ToString());
                PlayerPrefs.SetString(Constants.LAST_VALIDATION_ENVIRONMENT_KEY, licenseResponse.environment);      

                // Load the assigned scene.
                OnLicenseValidated(_licenseField.text, licenseResponse.environment.ToEnum<ELicenseEnvironment>());
            }
            catch (Exception ex)
            {
                if (Utils.IsWithinOfflineGracePeriod(_licenseField.text))
                    OnLicenseValidated(_licenseField.text, PlayerPrefs.GetString(Constants.LAST_VALIDATION_ENVIRONMENT_KEY).ToEnum<ELicenseEnvironment>());
                else
                    _errorText.text = $"Error: {ex.Message}. License is not within offline grace period. Please connect to the internet!";
            }

            // Set the checking license variable back to false in order to allow other requests.
            _isCheckingLicense = false;
        }

        private void OnLicenseValidated(string license, ELicenseEnvironment environment)
        {
            if (_loadingScene) return;
            
            // Store the used license code.
            LicenseStaticReferences.LicenseCode = license;
            LicenseStaticReferences.LicenseEnvironment = environment;

            // Start the license server.
            LANDiscovery.Instance.StartServer();
            HttpServer.Instance.StartServer();
            HttpServer.Instance.Register("/api/license", async (req) =>
            {
                LicenseMessage licenseMessage = new LicenseMessage(environment, _appVersion);
                return new HttpResponse(200, JsonConvert.SerializeObject(licenseMessage));
            });

            // Start the background license validation process.
            LicenseBackgroundValidator.Instance.StartBackgroundLicenseChecking(license);

            _loadingScene = true;
            SceneManager.LoadSceneAsync(_sceneToLoad);
        }
    
        private void OnDestroy()
        {
            // Unsubscribe from the validate button on click event.
            if(_validateLicenseButton != null) _validateLicenseButton.onClick.RemoveListener(OnValidateLicenceClicked);
        }
    }
}