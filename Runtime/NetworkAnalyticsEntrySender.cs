using UnityEngine;
using Mirror;
using System.Collections.Generic;
using Newtonsoft.Json;
using System;
using MirrorUtils;
using NaughtyAttributes;

namespace UniVRseDashboardIntegration
{
    public class NetworkAnalyticsEntrySender : NetworkBehaviour // This script is responsible for sending the local data from the client to the server.
    {
        #region Singleton pattern
        private static NetworkAnalyticsEntrySender _instance;
        public static NetworkAnalyticsEntrySender Instance => _instance != null ? _instance : _instance = FindAnyObjectByType<NetworkAnalyticsEntrySender>();
        #endregion

        [Header("Setttings")]
        [SerializeField] private bool _sendOnStartClient = true;
        [SerializeField] private bool _sendAtTimeInterval = true;
        [SerializeField] private float _sendInterval = 60f;

        [Header("Activity Detection (Optional)")]
        [SerializeField] private bool _stopWhenInactive = true;
        [SerializeField, ShowIf("_stopWhenInactive")] private Transform _xrCamera;
        [SerializeField, ShowIf("_stopWhenInactive")] private float _positionThreshold = 0.01f;
        [SerializeField, ShowIf("_stopWhenInactive")] private float _rotationThreshold = 1f;
        [SerializeField, ShowIf("_stopWhenInactive")] private float _inactivityWindowDuration = 300f; // 5 minutes

        [Header("Debug")]
        [SerializeField] protected bool DebugLog = true;

        // Mandatory fields.
        protected float TotalTime;

        // Server reset.
        private DateTime _cachedServerStartTime; // Used to detect server resets on the client side.

        // Activity tracking.
        private float _inactivityTimer = 0f;
        private Vector3 _lastCameraPosition = Vector3.zero;
        private Quaternion _lastCameraRotation = Quaternion.identity;

        // Helpers.
        protected bool StopWhenInactive => _stopWhenInactive && !Application.isEditor; // Editor will never count inactivity.
        protected bool IsSessionInactive => this.StopWhenInactive && _inactivityTimer >= _inactivityWindowDuration;

        /// <summary>
        /// Client-first analytics approach: client sends data to server, which forwards it to the cloud.
        /// 
        /// Scenarios:
        /// a) Normal flow: Server is open -> Client connects (no data)
        ///     - Server resets clientId-cloudId mapping (if any)
        ///     - New cloud entry created with new mapping
        ///     - Client sends empty data and server pushes to new entry in the cloud.
        ///     (Prevents overwriting data on the cloud with previous mappings still stored on the server)
        /// 
        /// b) Client Reconnection: Server stays open -> Client reconnects with existing data
        ///     - Server updates existing cloud entry (using clientId-cloudId mapping)
        ///     (Prevents multiple entries for the same session and allows clients to keep sending data to the same cloud entry when not closing the game)
        /// 
        /// c) Server reset: Server closes → Client stays running with data
        ///     - Client must clear all data
        ///     - Server creates new entry with fresh mapping
        ///     (Prevents old data being duplicated in new cloud entry)
        /// </summary>

        public override void OnStartClient()
        {
            base.OnStartClient();

            // Fresh connection (Client has no data).
            if(_cachedServerStartTime == default)
            {
                if(this.DebugLog) Debug.Log("a. Client connected first time to the server.");
                ResetEntryID();
            }
            // Server reset (when client stays on but server restarts).
            else if(_cachedServerStartTime != NetworkGlobalTimer.Instance.ServerStartTime)
            {
                if(this.DebugLog) Debug.Log("c. Server reset detected.");
                ResetAllData();
                ResetEntryID();
            }
            // Client reconnection (when client disconnects but server stays on. Client has data).
            else if(this.TotalTime > 0)
            {
                if(this.DebugLog) Debug.Log("b. Client reconnection detected.");
            }

            _cachedServerStartTime = NetworkGlobalTimer.Instance.ServerStartTime;

            // Send an initial entry to the server.
            if(_sendOnStartClient) SendAnalyticsEntryToServer();

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
            if(_xrCamera == null || !StopWhenInactive) return;

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
            if(this.IsSessionInactive) return; // Do not increase time if inactive.

            this.TotalTime += Time.deltaTime;
        }

        [ClientCallback]
        protected virtual void SendAnalyticsEntryToServer() // Called externally on the client side (the server might call it too but the [ClientCallback] flag makes sure the server won't push an entry).
        {   
            if(this.IsSessionInactive) return; // Make sure whoever overrides this method also respects inactivity.

            // Empty dictionary.
            Dictionary<string, object> data = new Dictionary<string, object>();

            // Send the analytics entry to the server.
            CmdPushAnalyticsEntry(SystemInfo.deviceUniqueIdentifier, this.TotalTime, JsonConvert.SerializeObject(data));

            if (this.DebugLog) Debug.Log("Sent analytics entry to server.");
        }

        [Command(requiresAuthority = false)]
        protected void CmdPushAnalyticsEntry(string deviceId, float totalTime, string dataJson)
        {
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