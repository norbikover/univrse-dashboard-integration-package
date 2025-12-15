using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System;
using HttpIntegration;
using Utilities;

namespace UniVRseDashboardIntegration
{
    public class AnalyticsEntryManager : MonoBehaviour
    {
        #region Singleton Pattern
        private static AnalyticsEntryManager _instance;
        public static AnalyticsEntryManager Instance
        {
            get
            {
                return _instance != null ? _instance : _instance = FindAnyObjectByType<AnalyticsEntryManager>();
            }
        }
        #endregion

        [Header("Settings")]
        [SerializeField] private string _apiPostfix = "/add-entry";
        [SerializeField] private string _successEntriesCollectionName = "SuccessEntries";
        [SerializeField] private string _errorEntriesCollectionName = "ErrorEntries";
        [SerializeField] private float _localToCloudPushInterval = 30f;
        [SerializeField] private bool _storeSuccessDocumentsLocally = false;
        [SerializeField] private bool _storeErrorDocumentsLocally = true;

        [Header("Debug")]
        [SerializeField] private bool _debugLog = true;

        // This represents the ID (from Mongo) of the first AnalyticsEntry sent on start.
        // Basically each client that sends entries to the server will first receive a mongoid for that entry.
        // We will then map that mongoID with the user's device id.
        // If a client sends a new entry we can now match the given device id to the mongo entry id and update the entry instead of pushing a new one.
        // Because we are using deviceID, the server will remove the deviceID from the Dictionary whenever a client joins the game. This is not needed when clients reconnect cause local data is preserved so we actually want to use PUT in that case.
        private Dictionary<string, string> _entriesIDS = new Dictionary<string, string>();
        private DateTime _startTime;

        private void Start()
        {
            _startTime = DateTime.Now;

            if (_localToCloudPushInterval > 0)
            {
                CancelInvoke(nameof(PushLocalDocumentsToCloudRepeating));
                InvokeRepeating(nameof(PushLocalDocumentsToCloudRepeating), _localToCloudPushInterval, _localToCloudPushInterval);
            }
        }

        public async void SendAnalyticsEntryToCloud(string deviceId, float totalTime, Dictionary<string, object> data)
        {
            // Return in case no license code was previously provided (most probably DEV build).
            if (string.IsNullOrEmpty(LicenseStaticReferences.LicenseCode))
            {
                Debug.Log("Cannot push analytics to the cloud without a License Code.");
                return;
            }
            
            // Store the device name (if any).
            string deviceName = DeviceMappingSystem.Instance.GetDeviceName(deviceId);

            // Prepare the analytics entry.
            AnalyticsEntry analyticsEntry = new AnalyticsEntry(
                licenseCode: LicenseStaticReferences.LicenseCode,
                deviceId: !string.IsNullOrEmpty(deviceName) ? deviceName : deviceId,
                totalTime: totalTime,
                version: Application.version,
                data: data
            );

            // If the client has sent another entry before it should have a mapping in the dictionary such that we can update the existing entry from the database instead of pusing a new one.
            string senderEntryID = _entriesIDS.ContainsKey(deviceId) ? _entriesIDS[deviceId] : "";

            try
            {
                string response = await HttpService.Instance.SendRequestAsync(
                    postfix: string.IsNullOrEmpty(senderEntryID) ? _apiPostfix : Path.Combine(_apiPostfix, senderEntryID),
                    method: string.IsNullOrEmpty(senderEntryID) ? HttpMethod.POST : HttpMethod.PUT,
                    data: analyticsEntry,
                    serverUrl: Constants.API_ENDPOINT);

                Debug.Log($"Entry sent successfully to the cloud: {response}");

                string entryCloudID = response.Trim('"');

                // Create a mapping between the sender's device ID and the entry ID received from the backend.
                _entriesIDS[deviceId] = entryCloudID;

                // Store the entry locally on success.
                if (_storeSuccessDocumentsLocally) OfflineDatabaseManager.Instance.AddDocumentToCollection(analyticsEntry, entryCloudID, _successEntriesCollectionName);

            }
            catch (Exception ex)
            {
                Debug.Log($"Failed to send entry to the cloud. Storing the document locally. Error: {ex.Message}");

                // Check if the sender already has an entry in the database. If it does we will be able to update it later, otherwise we will later push the error entry as a new one.
                string entryCloudID = _entriesIDS.ContainsKey(deviceId) ? _entriesIDS[deviceId] : "";

                // If there's an error, store the entry locally.
                if (_storeErrorDocumentsLocally)
                {
                    string documentName = string.IsNullOrEmpty(entryCloudID) ? $"{_startTime.ToLongString()}({deviceId})" : entryCloudID;
                    OfflineDatabaseManager.Instance.AddDocumentToCollection(analyticsEntry, documentName, _errorEntriesCollectionName);
                }
            }
        }

        private async void PushLocalDocumentsToCloudRepeating() // We try to push all the local documents to the cloud.
        {
            // Check if the device is connected to the internet
            if (!InternetChecker.Instance.IsConnectedToInternet()) return;

            // Get all the error entries from the local database and go through all of them.
            Dictionary<string, AnalyticsEntry> errorEntries = OfflineDatabaseManager.Instance.ReadDocumentsFromCollection<AnalyticsEntry>(_errorEntriesCollectionName);
            foreach (var kvp in errorEntries)
            {
                // Store the current values.
                string documentName = kvp.Key;
                AnalyticsEntry analyticsEntry = kvp.Value;

                // Check if the PUT method should be used for sending the entry to the database (i.e. the entry already has an ID).
                bool usePUT = documentName.Length == 24;

                try
                {
                    string response = await HttpService.Instance.SendRequestAsync(
                        postfix: !usePUT ? _apiPostfix : Path.Combine(_apiPostfix, documentName),
                        method: !usePUT ? HttpMethod.POST : HttpMethod.PUT,
                        data: analyticsEntry,
                        serverUrl: Constants.API_ENDPOINT);

                    Debug.Log($"Local entry sent successfully to the cloud: {response}");

                    string entryCloudID = response.Trim('"');

                    // Remove the entry from the local error collection.
                    OfflineDatabaseManager.Instance.RemoveDocumentFromCollectionByName(documentName, _errorEntriesCollectionName);

                    // Add the entry to the local success documents collection.
                    if (_storeSuccessDocumentsLocally) OfflineDatabaseManager.Instance.AddDocumentToCollection(analyticsEntry, entryCloudID, _successEntriesCollectionName);
                }
                catch (Exception ex)
                {
                    Debug.Log($"Failed to send entry to the cloud. Document is already stored locally. Error: {ex.Message}");
                }
            }
        }

        public void ResetEntryID(string senderDeviceId) // This is used when the same network entity should send a new entry to the cloud database instead of updating an already existing entry. 
        {
            _entriesIDS.Remove(senderDeviceId);
        }
    }
}