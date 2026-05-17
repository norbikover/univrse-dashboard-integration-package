using UnityEngine;
using TMPro;
using System;
using UnityEngine.UI;
using HttpIntegration;

namespace UniVRseDashboardIntegration
{
    public class LicenseBackgroundValidator : MonoBehaviour
    {
        #region Singleton Pattern
        private static LicenseBackgroundValidator _instance;
        public static LicenseBackgroundValidator Instance => _instance != null ? _instance : (_instance = FindAnyObjectByType<LicenseBackgroundValidator>());
        #endregion

        [Header("References")]
        [SerializeField] private GameObject _canvas;
        [SerializeField] private TMP_Text _errorText;
        [SerializeField] private TMP_Text _quitTimerText;
        [SerializeField] private Button _quitButton;
        [SerializeField] private Button _retryButton;
        [SerializeField] private float _licenseRepeatingInterval = 120f;
        [SerializeField] private float _quitTimeDelay = 60f;

        // Private variables.
        private bool _popupEnabled = false;
        private bool _isCheckingLicense = false;
        private bool _isQuitting = false;
        private string _currentLicense = string.Empty;
        private float _quitTimer = 0f;

        private void Start()
        {
            // Reset the error text and disable the canvas.
            _errorText.text = "";
            _canvas.SetActive(false);

            // Make sure the buttons are hooked to the correct functions.
            _quitButton.onClick.AddListener(OnQuitButtonPressed);
            _retryButton.onClick.AddListener(OnRetryButtonPressed);
        }

        public void StartBackgroundLicenseChecking(string license)
        {
            if(string.IsNullOrEmpty(license)) return;
            _currentLicense = license;
            CancelInvoke(nameof(ValidateLicense));
            Invoke(nameof(ValidateLicense), _licenseRepeatingInterval);
        }

        private async void ValidateLicense()
        {
            if (_isCheckingLicense) return;

            // If offline check for grace period.
            if(Application.internetReachability == NetworkReachability.NotReachable)
            {
                if(Utils.IsWithinOfflineGracePeriod(_currentLicense))
                {
                    // In case of success, automatically recheck the license again after the given delay.
                    SetPopupState(false);
                    CancelInvoke(nameof(ValidateLicense));
                    Invoke(nameof(ValidateLicense), _licenseRepeatingInterval);
                }
                else
                {
                    _errorText.text = "No internet connection and offline grace period has expired. Please connect to the internet!";
                    SetPopupState(true);
                    CancelInvoke(nameof(ValidateLicense));
                }

                return;
            }

            _isCheckingLicense = true;
            _errorText.text = "";

            try
            {
                // Build the query string from the LicenseRequest object.
                LicenseRequest licenseRequest = new LicenseRequest(_currentLicense, Constants.APP_ID, Application.version);

                // Perform the license validation request.
                string responseJson = await HttpService.Instance.SendRequestAsync(
                    postfix: Constants.LICENSE_VALIDATION_POSTFIX,
                    method: HttpMethod.POST,
                    data: licenseRequest,
                    serverUrl: Constants.API_ENDPOINT);
                
                // Automatically recheck the license again after the given delay.
                SetPopupState(false);
                CancelInvoke(nameof(ValidateLicense));
                Invoke(nameof(ValidateLicense), _licenseRepeatingInterval);
            }
            catch (Exception ex) // In case of an error.
            {
                _errorText.text = ex.Message;
                SetPopupState(true);
                CancelInvoke(nameof(ValidateLicense));
            }

            _isCheckingLicense = false;
        }

        private void Update()
        {
            if (_popupEnabled)
            {
                _quitTimer -= Time.deltaTime;
                _quitTimerText.text = $"Application will close in {(int)_quitTimer} seconds";
                if (_quitTimer <= 0 && !_isQuitting)
                {
                    _isQuitting = true; 
                    Application.Quit();
                }
            }
        }

        private void OnQuitButtonPressed()
        {
            if (_isQuitting) return;
            _isQuitting = true;
            Application.Quit();
        }

        private void OnRetryButtonPressed()
        {
            ValidateLicense();
        }

        private void SetPopupState(bool state)
        {
            if(state && !_popupEnabled)
            {
                _popupEnabled = true;
                _canvas.SetActive(true);
                _quitTimer = _quitTimeDelay;
            }
            else if(!state && _popupEnabled)
            {
                _popupEnabled = false;
                _canvas.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            if(_quitButton != null) _quitButton.onClick.RemoveListener(OnQuitButtonPressed);
            if(_retryButton != null) _retryButton.onClick.RemoveListener(OnRetryButtonPressed);
        }
    }
}