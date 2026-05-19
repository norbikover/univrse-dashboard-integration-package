using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using Newtonsoft.Json;
using TMPro;
using NaughtyAttributes;
using LANHelpers;
using HttpIntegration;

namespace UniVRseDashboardIntegration
{
    public class LicenseClient : MonoBehaviour
    {
        [Header("References")]
        [SerializeField, Scene] private string _licenseServerScene;
        [SerializeField, Scene] private string _sceneToLoad;
        [SerializeField] private TMP_Text[] _errorTexts;

        // Private variables.
        private bool _validServerFound = false;
        private bool _loadingScene = false;
        private string _appVersion;

        // Overridable properties.
        protected virtual bool SwitchToLicenseServerOnLoad => false;

        private void Start()
        {
            if (SwitchToLicenseServerOnLoad)
            {
                LoadScene(_licenseServerScene);
                return;
            }

            // Clear the error text.
            UpdateTexts(_errorTexts, "");

            // Store the application version.
            _appVersion = Application.version;

            // Start searching for servers.
            LANDiscovery.Instance.OnServerFound += OnServerFound;
            LANDiscovery.Instance.StartListening();
        }

        private async void OnServerFound(string ip)
        {
            if(_validServerFound) return;
            _validServerFound = true;

            try
            {
                UpdateTexts(_errorTexts, "");

                string licenseJson = await HttpService.Instance.SendRequestAsync(
                    postfix: "/license",
                    method: HttpMethod.GET,
                    serverUrl: $"http://{ip}:{HttpServer.Instance.Port}/api"
                );

                LicenseMessage licenseMessage = JsonConvert.DeserializeObject<LicenseMessage>(licenseJson);

                // Return in case the server has a different version than the client.
                if (licenseMessage.AppVersion != _appVersion) throw new Exception($"A server was found but the versions do not match. Server version: {licenseMessage.AppVersion}; Client version: {_appVersion}");

                // Update the license static references environment such that the client can use it too.
                LicenseStaticReferences.LicenseEnvironment = licenseMessage.Environment;

                // Store the scene name and load the next scene.
                if (LANDiscovery.Instance) LANDiscovery.Instance.OnServerFound -= OnServerFound;
                if (LANDiscovery.Instance) LANDiscovery.Instance.StopListening();
                LoadScene(_sceneToLoad);
            }
            catch (Exception ex)
            {
                _validServerFound = false;
                UpdateTexts(_errorTexts, ex.Message);
            }
        }

        private void LoadScene(string sceneName)
        {
            if (_loadingScene) return;
            _loadingScene = true;

            SceneManager.LoadSceneAsync(sceneName);
        }

        public void SkipLicenseChecking()
        {
            LoadScene(_sceneToLoad);
        }

        private void UpdateTexts(TMP_Text[] texts, string message)
        {
            foreach (TMP_Text text in texts)
            {
                if (text != null) text.text = message;
            }
        }

        private void OnDestroy()
        {
            if (LANDiscovery.Instance != null)
                LANDiscovery.Instance.OnServerFound -= OnServerFound;
        }
    }
}