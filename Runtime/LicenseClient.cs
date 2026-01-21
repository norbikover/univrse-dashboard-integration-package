using System.Net.Sockets;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using Newtonsoft.Json;
using TMPro;
using NaughtyAttributes;
using LANHelpers;
using System.Threading.Tasks;
using HttpIntegration;

namespace UniVRseDashboardIntegration
{
    public class LicenseClient : MonoBehaviour
    {
        [Header("References")]
        [SerializeField, Scene] private string _sceneToLoad;
        [SerializeField] private TMP_Text _errorText;

        // Private variables.
        private bool _loadingScene = false;

        private void Start()
        {
            // Clear the error text.
            _errorText.text = "";

            // Start searching for servers.
            LANDiscovery.Instance.OnServerFound += OnServerFound;
            LANDiscovery.Instance.StartListening();
        }

        private async void OnServerFound(string ip)
        {
            try
            {
                string licenseJson = await HttpService.Instance.SendRequestAsync(
                    postfix: "/license",
                    method: HttpMethod.GET,
                    serverUrl: $"http://{ip}:{HttpServer.Instance.Port}/api"
                );

                LicenseMessage licenseMessage = JsonConvert.DeserializeObject<LicenseMessage>(licenseJson);

                // Return in case the server has a different version than the client.
                if (licenseMessage.AppVersion != Application.version) throw new Exception($"A server was found but the versions do not match. Server version: {licenseMessage.AppVersion}; Client version: {Application.version}");

                // Update the license static references environment such that the client can use it too.
                LicenseStaticReferences.LicenseEnvironment = licenseMessage.Environment;

                // Store the scene name and load the next scene.
                if (LANDiscovery.Instance) LANDiscovery.Instance.OnServerFound -= OnServerFound;
                if (LANDiscovery.Instance) LANDiscovery.Instance.StopListening();
                LoadScene(_sceneToLoad);
            }
            catch (Exception ex)
            {
                _errorText.text = ex.Message;
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

        private void OnDestroy()
        {
            if (LANDiscovery.Instance != null)
                LANDiscovery.Instance.OnServerFound -= OnServerFound;
        }
    }
}