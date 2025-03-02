using UnityEngine;

[CreateAssetMenu(fileName = "Test", menuName = "SO Test/New Test")]
public class SOTest : ScriptableObject
{
    
    [SerializeField] private new string name = "Test";
    [SerializeField, Multiline(5)] private string description = "This is a test";
    [SerializeField, Min(0)] private int timeToLoad = 1;
    [SerializeField, Min(0)] private int timeToUnload = 1;
    [SerializeField] private GameObject prefab;
    [SerializeField] private Transform spawnPoint;
    
    
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
        if (!spawnPoint)
        {
            Debug.Log("No spawn point set for " + name);
            return Vector3.up;
        }
        return spawnPoint.position;
    }
}
