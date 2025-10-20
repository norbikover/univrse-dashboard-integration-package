using System.Collections.Generic;
using System.IO;
using UnityEngine;
using FileSystem;

namespace UniVRseDashboardIntegration
{
    public class OfflineDatabaseManager : MonoBehaviour
    {
        #region Singleton Pattern

        private static OfflineDatabaseManager _instance;
        public static OfflineDatabaseManager Instance
        {
            get
            {
                return _instance ?? (_instance = FindAnyObjectByType<OfflineDatabaseManager>());
            }
        }

        #endregion

        [Header("Settings")]
        [SerializeField] private string _localDatabaseName = "LocalDatabase";

        public void AddDocumentToCollection<T>(T instance, string documentName, string collection)
        {
            FileInteraction.WriteToFile(Path.Combine(Application.persistentDataPath, _localDatabaseName, collection, $"{documentName}.json"), instance);
        }

        public void RemoveDocumentFromCollectionByName(string documentName, string collection)
        {
            File.Delete(Path.Combine(Application.persistentDataPath, _localDatabaseName, collection, $"{documentName}.json"));
        }

        public Dictionary<string, T> ReadDocumentsFromCollection<T>(string collection)
        {
            return FileInteraction.ReadFromFolder<T>(Path.Combine(Application.persistentDataPath, _localDatabaseName, collection));
        }
    }
}
