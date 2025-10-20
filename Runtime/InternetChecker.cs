using System.Net;
using UnityEngine;
using System.Threading;

namespace UniVRseDashboardIntegration
{
    public class InternetChecker : MonoBehaviour
    {
        #region Singleton Pattern

        private static InternetChecker _instance;
        public static InternetChecker Instance
        {
            get
            {
                return _instance ?? (_instance = FindAnyObjectByType<InternetChecker>());
            }
        }

        #endregion

        [SerializeField] private float _checkInterval = 5f;
        private const string GOOGLE_URL = "http://www.google.com";
        private volatile bool _isConnected; // Use volatile to ensure thread safety
        private Thread _checkThread;

        private void Start()
        {
            _checkThread = new Thread(CheckInternetLoop);
            _checkThread.Start();
        }

        private void OnDestroy()
        {
            if (_checkThread != null && _checkThread.IsAlive)
            {
                _checkThread.Abort();
            }
        }

        private void CheckInternetLoop()
        {
            while (true)
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(GOOGLE_URL);
                request.Timeout = ((int)_checkInterval - 1) * 1000;

                try
                {
                    using (var response = (HttpWebResponse)request.GetResponse())
                    {
                        _isConnected = response.StatusCode == HttpStatusCode.OK;
                    }
                }
                catch (System.Exception)
                {
                    _isConnected = false;
                }

                Thread.Sleep((int)(_checkInterval * 1000));
            }
        }

        public bool IsConnectedToInternet()
        {
            return _isConnected;
        }
    }
}
