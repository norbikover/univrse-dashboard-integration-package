using UnityEngine;
using Mirror;
using System.Collections.Generic;
using Newtonsoft.Json;
using MirrorUtils;

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

        [Header("Activity Detection (Optional)")]
        [SerializeField] private Transform _xrCamera;
        [SerializeField] private float _positionThreshold = 0.01f;
        [SerializeField] private float _rotationThreshold = 1f;
        [SerializeField] private float _inactivityWindowDuration = 300f; // 5 minutes
        [SerializeField] private bool _stopOnInactivity = true;

        [Header("Debug")]
        [SerializeField] protected bool DebugLog = true;

        // Mandatory fields.
        protected float TotalTime;

        // Activity tracking.
        private float _inactivityTimer = 0f;
        private Vector3 _lastCameraPosition = Vector3.zero;
        private Quaternion _lastCameraRotation = Quaternion.identity;

        // Helpers.
        protected bool Inactive => !Application.isEditor && _xrCamera != null && _inactivityTimer >= _inactivityWindowDuration;

        /// <summary>
        /// Client-first analytics approach: client sends data to server, which forwards it to the cloud.
        /// 
        /// Scenarios:
        /// a) Normal flow: Server opens → Client connects → Client sends data → Server pushes to cloud
        /// 
        /// b) Reconnection: Server stays open → Client reconnects with existing data → Server updates 
        ///    existing cloud entry (using clientId-cloudId mapping)
        /// 
        /// c) Client reset: Server stays open → Client closes then reconnects with no data (TotalTime == 0)
        ///    → Server resets clientId-cloudId mapping → New cloud entry created
        ///    (Prevents overwriting cloud data with empty data)
        /// 
        /// d) Server reset: Server closes → Client stays running with data (LocalTime > GlobalTime)
        ///    → Client must clear all data on reconnection → Server creates new entry with fresh mapping
        ///    (Prevents old data being duplicated in new cloud entry)
        /// </summary>

        public override void OnStartClient()
        {
            base.OnStartClient();

            // c. In case the client is a new one (no reconnection) we want to make sure the server pushes to a new entry id in the cloud.
            if(this.TotalTime == 0)
                ResetEntryID();

            // d. In case the server was closed but the client still has some data, we want to reset it.
            if(NetworkGlobalTimer.Instance.LocalTime > NetworkGlobalTimer.Instance.GlobalTime)
                ResetAllData();

            // Send an initial entry to the server.
            if(_sendOnStartClient) 
                SendAnalyticsEntryToServer();

            // Repeatedly send updates to the server.
            if(_sendAtTimeInterval)
            {
                CancelInvoke(nameof(SendAnalyticsEntryToServer));
                InvokeRepeating(nameof(SendAnalyticsEntryToServer), _sendInterval, _sendInterval);
            }
        }

        [ClientCallback]
        private void CheckForInactivity()
        {
            if(_xrCamera == null) return;

            // Check if movement exceeds thresholds.
            bool movementDetected = Vector3.Distance(_xrCamera.position, _lastCameraPosition) > _positionThreshold || 
                                    Quaternion.Angle(_xrCamera.rotation, _lastCameraRotation) > _rotationThreshold;

            // Update last known position and rotation.
            _lastCameraPosition = _xrCamera.position;
            _lastCameraRotation = _xrCamera.rotation;

            if(!movementDetected) // Increment inactivity timer.     
                _inactivityTimer += Time.deltaTime;
            else _inactivityTimer = 0f;
        }

        [ClientCallback]
        protected virtual void Update()
        {
            CheckForInactivity();
            if(this.Inactive) return; // Do not increase time if inactive.

            this.TotalTime += Time.deltaTime;
        }

        [ClientCallback]
        protected virtual void SendAnalyticsEntryToServer() // Called externally on the client side (the server might call it too but the [ClientCallback] flag makes sure the server won't push an entry).
        {   
            if(this.Inactive) return; // Make sure whoever overrides this method also respects inactivity.

            // Empty dictionary.
            Dictionary<string, object> data = new Dictionary<string, object>();

            // Send the analytics entry to the server.
            CmdPushAnalyticsEntry(SystemInfo.deviceUniqueIdentifier, this.TotalTime, JsonConvert.SerializeObject(data));

            if (this.DebugLog) Debug.Log("Sent analytics entry to server.");
        }

        [Command(requiresAuthority = false)]
        protected void CmdPushAnalyticsEntry(string deviceId, float totalTime, string dataJson)
        {
            if (this.DebugLog) Debug.Log("[Server] Received Analytics Entry data from a client. Pushing it to the cloud ....");

            // Send the entry's data to the cloud.
            AnalyticsEntryManager.Instance.SendAnalyticsEntryToCloud(deviceId, totalTime, JsonConvert.DeserializeObject<Dictionary<string, object>>(dataJson));
        }

        [Client]
        public virtual void ResetAllData()
        {
            // Reset the total time.
            this.TotalTime = 0f;

            // Reset activity tracking.
            _inactivityTimer = 0f;
        }
        
        [Client]
        public void ResetEntryID()
        {
            // Tell the server to remove the clientId-documentId mapping.
            CmdResetEntryID(SystemInfo.deviceUniqueIdentifier);
        }

        [Command(requiresAuthority = false)]
        private void CmdResetEntryID(string deviceId)
        {
            AnalyticsEntryManager.Instance.ResetEntryID(deviceId);
        }
    }
}