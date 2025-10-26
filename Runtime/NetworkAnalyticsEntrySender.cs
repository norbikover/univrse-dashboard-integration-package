using UnityEngine;
using Mirror;
using System.Collections.Generic;
using Newtonsoft.Json;
namespace UniVRseDashboardIntegration
{
    public class NetworkAnalyticsEntrySender : NetworkBehaviour // This script is responsible for sending the local data from the client to the server.
    {
        #region Singleton pattern

        private static NetworkAnalyticsEntrySender _instance;

        public static NetworkAnalyticsEntrySender Instance
        {
            get
            {
                return _instance ?? (_instance = FindAnyObjectByType<NetworkAnalyticsEntrySender>());
            }
        }

        #endregion

        [Header("Setttings")]
        [SerializeField] private float _sendInterval = 60f;

        [Header("Debug")]
        [SerializeField] protected bool DebugLog = true;

        // Mandatory fields.
        protected float TotalTime;

        public override void OnStartClient()
        {
            base.OnStartClient();

            // Initialize the time since start.
            this.TotalTime = 0f;

            // Send an initial entry to the server and then repeatedly send updates to the server.
            if(_sendInterval > 0) InvokeRepeating(nameof(SendAnalyticsEntryToServer), 0f, _sendInterval);
        }

        [ClientCallback]
        protected virtual void Update()
        {
            // Increase the time since start.
            this.TotalTime += Time.deltaTime;
        }

        [ClientCallback]
        protected virtual void SendAnalyticsEntryToServer() // Called externally on the client side (the server might call it too but the [ClientCallback] flag makes sure the server won't push an event).
        {
            Dictionary<string, object> data = new Dictionary<string, object>();

            // Send the analytics entry to the server.
            CmdPushAnalyticsEvent(SystemInfo.deviceUniqueIdentifier, TotalTime, JsonConvert.SerializeObject(data));

            if (this.DebugLog) Debug.Log("Sent analytics event to server.");
        }

        [Command(requiresAuthority = false)]
        protected void CmdPushAnalyticsEvent(string deviceId, float totalTime, string dataJson, NetworkConnectionToClient sender = null)
        {
            if (this.DebugLog) Debug.Log("[Server] Received Analytics Event data from a client. Pushing it to the cloud ....");

            // Send the entry's data to the cloud.
            AnalyticsEntryManager.Instance.SendAnalyticsEntryToCloud(deviceId, totalTime, JsonConvert.DeserializeObject<Dictionary<string, object>>(dataJson), (int)sender.identity.netId);
        }
    }
}