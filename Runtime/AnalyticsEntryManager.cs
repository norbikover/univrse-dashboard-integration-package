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
        public static AnalyticsEntryManager Instance => _instance != null ? _instance : (_instance = FindAnyObjectByType<AnalyticsEntryManager>());
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

        // Private variables.
        private Dictionary<string, string> _cloudDeviceEntryMap = new Dictionary<string, string>(); // Used to map device IDs to cloud entry IDs for entries that were successfully sent to the cloud in order to use PUT next time.
        private Dictionary<string, string> _offlineDeviceEntryMap = new Dictionary<string, string>(); // This is used to prevent the same device overwriting the same local document in case client connects again but server stays open.
        private Dictionary<string, AnalyticsEntry> _entries = new Dictionary<string, AnalyticsEntry>(); // Used to prevent overwriting the {createdAt} field.
        
        // 4 CASES:
        // FULL INTERNET (push directly to cloud)
        // FULL OFFLINE (store entries locally, push to cloud later)
        // ONLINE -> OFFLINE (store cloudEntryId in documentName locally and we can update the same entry in the cloud when we have internet again ... we don't want to push a new entry)
        // OFFLINE -> ONLINE (tricky as we might push both offline data and then new online data as 2 entries)
        // a. if online pushes first (with latest data) we don't want to push outdated offline too -> remove the offline document and mapping
        // b. if offline pushes first, we want the online to overwrite the same entry -> create a mapping between device and cloud entry ID and remove offline mapping BUT only if offline was created during this session.
        
        private void Start()
        {
            if (_localToCloudPushInterval > 0)
            {
                CancelInvoke(nameof(PushLocalDocumentsToCloudRepeating));
                InvokeRepeating(nameof(PushLocalDocumentsToCloudRepeating), _localToCloudPushInterval, _localToCloudPushInterval);
            }
        }

        public async void SendAnalyticsEntryToCloud(string deviceId, float totalTime, Dictionary<string, object> data)
        {
            // Store the device name (if any).
            string deviceName = DeviceMappingSystem.Instance.GetDeviceName(deviceId);

            // If the client has sent another entry before it should have a mapping in the dictionary such that we can update the existing entry from the database instead of pusing a new one.
            string cloudEntryID = _cloudDeviceEntryMap.ContainsKey(deviceId) ? _cloudDeviceEntryMap[deviceId] : string.Empty;

            if (this._debugLog) Debug.Log($"Received Analytics Entry data from a client.\nDevice ID: {deviceId}, Total Time: {totalTime} Updating Existing Cloud Entry: {!string.IsNullOrEmpty(cloudEntryID)}.");

            // Return in case no license code was previously provided (most probably DEV build).
            if (string.IsNullOrEmpty(LicenseStaticReferences.LicenseCode))
            {
                if(_debugLog) Debug.Log("Cannot push analytics to the cloud without a License Code.");
                return;
            }

            // If entry doesn't exist for this deviceId, create it.
            if(!_entries.ContainsKey(deviceId))
            {
                _entries[deviceId] = new AnalyticsEntry(
                    licenseCode: LicenseStaticReferences.LicenseCode,
                    deviceId: !string.IsNullOrEmpty(deviceName) ? deviceName : deviceId,
                    totalTime: totalTime,
                    version: Application.version,
                    createdAt: DateTime.UtcNow,
                    updatedAt: DateTime.UtcNow,
                    data: data
                );
            }
            else // Otherwise, update the existing entry for this device ID with the new values.
            {
                _entries[deviceId].licenseCode = LicenseStaticReferences.LicenseCode;
                _entries[deviceId].deviceId = !string.IsNullOrEmpty(deviceName) ? deviceName : deviceId;
                _entries[deviceId].totalTime = totalTime;
                _entries[deviceId].version = Application.version;
                // No created at.
                _entries[deviceId].updatedAt = DateTime.UtcNow.ToString("o");
                _entries[deviceId].data = data;
            }

            // Store the analytics entry.
            AnalyticsEntry analyticsEntry = _entries[deviceId];

            try
            {
                string response = await HttpService.Instance.SendRequestAsync(
                    postfix: string.IsNullOrEmpty(cloudEntryID) ? _apiPostfix : Path.Combine(_apiPostfix, cloudEntryID),
                    method: string.IsNullOrEmpty(cloudEntryID) ? HttpMethod.POST : HttpMethod.PUT,
                    data: analyticsEntry,
                    serverUrl: Constants.API_ENDPOINT);

                if(_debugLog) Debug.Log($"Entry sent successfully to the cloud: {response}");

                // Update cloud entry ID.
                cloudEntryID = response.Trim('"');

                // Create a mapping between the device ID and the cloud entry ID received from the backend.
                if(!_cloudDeviceEntryMap.ContainsKey(deviceId))
                    _cloudDeviceEntryMap[deviceId] = cloudEntryID;

                // Remove the local outdated error document (we don't want to send the offline anymore).
                if(_offlineDeviceEntryMap.ContainsKey(deviceId))
                {
                    _offlineDeviceEntryMap.Remove(deviceId);
                    OfflineDatabaseManager.Instance.RemoveDocumentFromCollectionByName($"{deviceId}_{_offlineDeviceEntryMap[deviceId]}", _errorEntriesCollectionName);
                }

                // Store the entry locally on success.
                if (_storeSuccessDocumentsLocally)
                {
                    string documentName = $"{deviceId}_{cloudEntryID}";
                    OfflineDatabaseManager.Instance.AddDocumentToCollection(analyticsEntry, documentName, _successEntriesCollectionName);
                    if(_debugLog) Debug.Log($"Stored entry in local success collection with document name: {documentName}");
                }
            }
            catch (Exception ex)
            {
                if(_debugLog) Debug.Log($"Failed to send entry to the cloud. Error: {ex.Message}");

                // If there's an error, store the entry locally.
                if (_storeErrorDocumentsLocally)
                {
                    // Create a mapping between the device ID and a local entry id.
                    if(!_offlineDeviceEntryMap.ContainsKey(deviceId)) 
                        _offlineDeviceEntryMap[deviceId] = Guid.NewGuid().ToString();

                    string documentName = string.IsNullOrEmpty(cloudEntryID) ? 
                                            $"{deviceId}_{cloudEntryID}" :
                                            $"{deviceId}_{_offlineDeviceEntryMap[deviceId]}"; 
                    
                    OfflineDatabaseManager.Instance.AddDocumentToCollection(analyticsEntry, documentName, _errorEntriesCollectionName);
                    
                    if(_debugLog) Debug.Log($"Stored entry locally with document name: {documentName}");
                }
            }
        }

        private async void PushLocalDocumentsToCloudRepeating() // We try to push all the local documents to the cloud.
        {
            // Get all the error entries from the local database and go through all of them.
            Dictionary<string, AnalyticsEntry> errorEntries = OfflineDatabaseManager.Instance.ReadDocumentsFromCollection<AnalyticsEntry>(_errorEntriesCollectionName);
            
            foreach (var kvp in errorEntries)
            {
                // Store the current values.
                string documentName = kvp.Key;
                AnalyticsEntry analyticsEntry = kvp.Value;

                // Split on the last underscore: the entry ID (GUID or MongoDB ObjectId) never contains underscores,
                // so this correctly handles deviceIds that may contain underscores.
                int separatorIndex = documentName.LastIndexOf('_');
                string deviceId = documentName[..separatorIndex];
                string entryId  = documentName[(separatorIndex + 1)..];

                // Local offline IDs are created with Guid.NewGuid(), so they parse as GUIDs → use POST.
                // Cloud IDs (e.g. MongoDB ObjectIds) will not parse as GUIDs → use PUT.
                bool usePUT = !Guid.TryParse(entryId, out _);

                try
                {
                    string response = await HttpService.Instance.SendRequestAsync(
                        postfix: !usePUT ? _apiPostfix : Path.Combine(_apiPostfix, entryId),
                        method: !usePUT ? HttpMethod.POST : HttpMethod.PUT,
                        data: analyticsEntry,
                        serverUrl: Constants.API_ENDPOINT);

                    if(_debugLog) Debug.Log($"Local entry sent successfully to the cloud: {response}. Removing local document.");
    
                    string entryCloudID = response.Trim('"');

                    // If the offline document was created during this session.
                    if(_offlineDeviceEntryMap.ContainsKey(deviceId) && _offlineDeviceEntryMap[deviceId] == entryId)
                    {
                        // Remove offline mapping.
                        _offlineDeviceEntryMap.Remove(deviceId);

                        // Create cloud mapping.
                        // This is useful in case we go offline -> online and offline pushes the entry first (we want the online to update the same entry)
                        if(!_cloudDeviceEntryMap.ContainsKey(deviceId))
                            _cloudDeviceEntryMap[deviceId] = entryCloudID;
                    }

                    // Remove the entry from the local error collection.
                    OfflineDatabaseManager.Instance.RemoveDocumentFromCollectionByName(documentName, _errorEntriesCollectionName);

                    // Add the entry to the local success documents collection.
                    if (_storeSuccessDocumentsLocally)
                    {   
                        OfflineDatabaseManager.Instance.AddDocumentToCollection(analyticsEntry, entryCloudID, _successEntriesCollectionName);
                        if(_debugLog) Debug.Log($"Stored entry in local success collection with document name: {entryCloudID}");
                    }
                }
                catch (Exception ex)
                {
                    if(_debugLog) 
                        Debug.Log($"Failed to send entry to the cloud. Document is already stored locally. Error: {ex.Message}");
                }
            }
        }

        public void ResetEntryID(string senderDeviceId) // This is used when the same network entity should send a new entry to the cloud database instead of updating an already existing entry. 
        {
            _cloudDeviceEntryMap.Remove(senderDeviceId);
            _offlineDeviceEntryMap.Remove(senderDeviceId);
            _entries.Remove(senderDeviceId);
        }
    }
}