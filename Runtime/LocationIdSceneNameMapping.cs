using UnityEngine;
using System.Collections.Generic;
using NaughtyAttributes;

public class LocationIdSceneNameMapping : MonoBehaviour // TODO: This will be removed.
{
    #region Singleton pattern

    private static LocationIdSceneNameMapping _instance;

    public static LocationIdSceneNameMapping Instance
    {
        get
        {
            return _instance ?? (_instance = FindAnyObjectByType<LocationIdSceneNameMapping>());
        }
    }

    #endregion

    [SerializeField] private List<Map> _mappings;

    #region Getters
    public string GetSceneNameByLocationId(string locationId)
    {
        foreach (Map map in _mappings)
        {
            if (map.LocationId.Equals(locationId))
                return map.SceneName;
        }

        return string.Empty;
    }
    #endregion

    [System.Serializable]
    public class Map
    {
        [SerializeField] private string _locationId;
        [SerializeField][Scene] private string _sceneName;

        #region Setters and Getters
        public string LocationId { get { return _locationId; } }
        public string SceneName { get { return _sceneName; } }
        #endregion
    }
}
