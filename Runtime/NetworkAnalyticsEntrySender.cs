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
        [SerializeField] private bool _sendOnStartClient = true;
        [SerializeField] private bool _sendAtTimeInterval = true;
        [SerializeField] private float _sendInterval = 60f;

        [Header("Debug")]
        [SerializeField] protected bool DebugLog = true;

        // Mandatory fields.
        protected float TotalTime;

        public override void OnStartClient()
        {
            base.OnStartClient();

            // In case the client is a new one (no reconnection) we want to make sure the server pushes to a new entry id in the cloud.
            if(this.TotalTime == 0)
                ResetEntryID();

            // Send an initial entry to the server.
            if(_sendOnStartClient) 
                SendAnalyticsEntryToServer();

            // Repeatedly send updates to the server.
            if(_sendAtTimeInterval) 
                InvokeRepeating(nameof(SendAnalyticsEntryToServer), _sendInterval, _sendInterval);   
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
            // Empty dictionary.
            Dictionary<string, object> data = new Dictionary<string, object>();

            // Send the analytics entry to the server.
            CmdPushAnalyticsEvent(SystemInfo.deviceUniqueIdentifier, TotalTime, JsonConvert.SerializeObject(data));

            if (this.DebugLog) Debug.Log("Sent analytics event to server.");
        }

        [Command(requiresAuthority = false)]
        protected void CmdPushAnalyticsEvent(string deviceId, float totalTime, string dataJson)
        {
            if (this.DebugLog) Debug.Log("[Server] Received Analytics Event data from a client. Pushing it to the cloud ....");

            // Send the entry's data to the cloud.
            AnalyticsEntryManager.Instance.SendAnalyticsEntryToCloud(deviceId, totalTime, JsonConvert.DeserializeObject<Dictionary<string, object>>(dataJson));
        }
        
        [Client]
        public void ResetEntryID()
        {
            // First reset all the stored data to default values.
            ResetAllData();

            // Tell the server to remove the clientId-documentId mapping.
            CmdResetEntryID(SystemInfo.deviceUniqueIdentifier);
        }

        [Command(requiresAuthority = false)]
        private void CmdResetEntryID(string deviceId)
        {
            AnalyticsEntryManager.Instance.ResetEntryID(deviceId);
        }

        [Client]
        public virtual void ResetAllData()
        {
            // Reset the total time.
            this.TotalTime = 0f;
        }
    }
}