using UnityEngine;
using TMPro;
using Newtonsoft.Json;
using System;
using UnityEngine.SceneManagement;
using UnityEngine.XR;
using UnityEngine.UI;
using HttpIntegration;
using NaughtyAttributes;
using LANHelpers;

namespace UniVRseDashboardIntegration
{
    public class LicenseServerOld : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TMP_InputField _licenseField;
        [SerializeField] private TMP_Text _errorText;
        [SerializeField] private Button _validateLicenseButton;
        [SerializeField, Scene] private string _licenseClientScene;
        [SerializeField, Scene] private string _templateScene;
        [SerializeField, Scene] private string _homeScreenScene;

        // Private variables.
        private bool _isCheckingLicense = false;
        private bool _loadingScene = false;

        private void Start()
        {
            if (!Application.isEditor && XRSettings.enabled)
            {
                LoadScene(_licenseClientScene);
                return;
            }

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
            if (string.Equals(_licenseField.text, Constants.SECRET_LICENSE))
            {
                // Start the license server with the DEV environment.
                LANDiscovery.Instance.StartServer();
                HttpServer.Instance.StartServer();
                HttpServer.Instance.Register("/api/license", async (req) =>
                {
                    LicenseMessageOld licenseMessage = new LicenseMessageOld(Constants.DEFAULT_SCENE_NAME, ELicenseEnvironment.DEV, Application.version);
                    return new HttpResponse(200, JsonConvert.SerializeObject(licenseMessage));
                });
                LoadScene(Constants.DEFAULT_SCENE_NAME);
                return;
            }

            // Set the checking license to true and reset the error text.
            _isCheckingLicense = true;
            _errorText.text = "";

            try
            {
                // Build the query string from the LicenseRequest object.
                LicenseRequest licenseRequest = new LicenseRequest(_licenseField.text, Constants.APP_ID, Application.version);

                // Perform the license validation request.
                string responseJson = await HttpService.Instance.SendRequestAsync(
                    postfix: Constants.LICENSE_VALIDATION_POSTFIX,
                    method: HttpMethod.POST,
                    data: licenseRequest,
                    serverUrl: Constants.API_ENDPOINT);

                // Deserialize the response JSON into a LicenseResponse object.
                LicenseResponseOld licenseResponse = JsonConvert.DeserializeObject<LicenseResponseOld>(responseJson);

                // Store the used license code.
                LicenseStaticReferences.LicenseCode = _licenseField.text;
                LicenseStaticReferences.LicenseEnvironment = licenseResponse.environment.ToEnum<ELicenseEnvironment>();
                PlayerPrefs.SetString(Constants.LICENSE_CODE_KEY, _licenseField.text); // Store the used license code such that we can autopopulate it next time.       

                // Get the correct scene name.
                string sceneName = LocationIdSceneNameMapping.Instance.GetSceneNameByLocationId(licenseResponse.locationId);

                // Start the license server.
                LANDiscovery.Instance.StartServer();
                HttpServer.Instance.StartServer();
                HttpServer.Instance.Register("/api/license", async (req) =>
                {
                    LicenseMessageOld licenseMessage = new LicenseMessageOld(sceneName, licenseResponse.environment.ToEnum<ELicenseEnvironment>(), Application.version);
                    return new HttpResponse(200, JsonConvert.SerializeObject(licenseMessage));
                });

                // Start the background license validator process. 
                LicenseBackgroundValidator.Instance.StartBackgroundLicenseChecking(_licenseField.text);

                // Load the correct scene.
                LoadScene(string.Equals(sceneName, _templateScene) ? _homeScreenScene : sceneName);
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