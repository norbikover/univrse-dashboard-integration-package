using System.Net.Sockets;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using Newtonsoft.Json;
using TMPro;
using NaughtyAttributes;

namespace UniVRseDashboardIntegration
{
    public class LicenseClient : MonoBehaviour
    {
        [Header("References")]
        [SerializeField, Scene] private string _sceneToLoad;
        [SerializeField] private TMP_Text _errorText;

        // Private variables.
        private UdpClient _udpClient;
        private bool _stopListening = false;
        private bool _loadingScene = false;

        private void Start()
        {
            // Clear the error text.
            _errorText.text = "";

            // Bind to the same port as the server.
            _udpClient = new UdpClient(Constants.UDP_LICENSE_PORT);

            // Start listening for broadcasts.
            ListenForBroadcasts();
        }

        private void OnDestroy()
        {        
            _stopListening = true; // Avoid infinite loops when quitting from this scene.
            _udpClient?.Close();
        }

        private async void ListenForBroadcasts()
        {
            while (!_stopListening)
            {
                try
                {
                    UdpReceiveResult result = await _udpClient.ReceiveAsync();
                    string json = Encoding.UTF8.GetString(result.Buffer);
                    LicenseMessage licenseMessage = JsonConvert.DeserializeObject<LicenseMessage>(json);

                    // Return in case the server has a different version than the client.
                    if (licenseMessage.AppVersion != Application.version) throw new Exception($"A server was found but the versions do not match. Server version: {licenseMessage.AppVersion}; Client version: {Application.version}");


                    // Update the license static references environment such that the client can use it too.
                    LicenseStaticReferences.LicenseEnvironment = licenseMessage.Environment;

                    // Store the scene name and load the next scene.
                    LoadScene(_sceneToLoad);

                    // Close the upd client and return out of the function to avoid loading the scene multiple times.
                    _udpClient?.Close();
                    return;
                }
                catch (Exception ex)
                {
                    _errorText.text = ex.Message;
                }
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
    }
}