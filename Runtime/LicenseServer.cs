using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace UniVRseDashboardIntegration
{
    public class LicenseServer : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float _sendInterval = 2f;

        [Header("Debug")]
        [SerializeField] private bool _debugLog = true;

        // Private variables.
        private UdpClient _udpServer;

        public async void StartBroadcast(ELicenseEnvironment environment)
        {
            if (_udpServer != null) return;

            // Initialize the udp server.
            _udpServer = new UdpClient { EnableBroadcast = true };

            // Initialize the endpoint.
            IPEndPoint endpoint = new IPEndPoint(IPAddress.Broadcast, Constants.UDP_LICENSE_PORT);

            // Initialize the message that will be sent over the network.
            string json = JsonConvert.SerializeObject(new LicenseMessage(environment, Application.version));
            byte[] data = Encoding.UTF8.GetBytes(json);

            // Keep on sending data.
            while (true)
            {
                try
                {
                    await _udpServer?.SendAsync(data, data.Length, endpoint);
                }
                catch (SocketException ex)
                {
                    if (_debugLog)
                        Debug.Log($"Broadcast error: {ex.Message}");
                }

                await Task.Delay((int)(_sendInterval * 1000));
            }
        }

        private void OnApplicationQuit()
        {
            _udpServer?.Close();
        }
    }
}