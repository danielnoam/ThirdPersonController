using UnityEngine;

[CreateAssetMenu(fileName = "Test", menuName = "SO Test/New Test")]
public class SOTest : ScriptableObject
{
    
    [SerializeField] private new string name = "Test";
    [SerializeField, Multiline(5)] private string description = "This is a test";
    [SerializeField, Min(0)] private int timeToLoad = 1;
    [SerializeField, Min(0)] private int timeToUnload = 1;
    [SerializeField] private GameObject prefab;
    
    
    public string GetName()
    {
        return name;
    }
    
    public string GetDescription()
    {
        return description;
    }
    
    public int GetTimeToLoad()
    {
        return timeToLoad;
    }
    
    public int GetTimeToUnload()
    {
        return timeToUnload;
    }
    
    public GameObject GetPrefab()
    {
        if (!prefab)
        {
            Debug.Log("No prefab set for " + name);
            return null;
        }
        return prefab;
    }
    
    public Vector3 GetSpawnPoint()
    {
        if (!prefab)
        {
            Debug.Log("No prefab set for " + name);
            return Vector3.up;
        }

        TestSpawnPlatform spawnPlatform = prefab.GetComponentInChildren<TestSpawnPlatform>();
        if (!spawnPlatform)
        {
            Debug.Log("No TestSpawnPosition in " + name);
            return Vector3.up;
        }

        return spawnPlatform.transform.position;
    }

    public bool HasRobot()
    {
        if (!prefab)
        {
            Debug.Log("No prefab set for " + name);
            return false;
        }

        RobotCompanion robot = prefab.GetComponentInChildren<RobotCompanion>();
        if (!robot)
        {
            Debug.Log("No RobotCompanion in " + name);
            return false;
        }

        return true;
    }
    
}
