using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;
using VInspector;

public class TestManager : MonoBehaviour
{
    public SOTest[] tests;
    [SerializeField, ReadOnly] private SOTest currentTest;
    [SerializeField, ReadOnly] private GameObject currentEnvironment;
    [SerializeField, ReadOnly] private PlayerStateMachine currentPlayer;
    [SerializeField, ReadOnly] private RobotCompanion currentRobot;

    private void Start()
    {
        currentPlayer = FindFirstObjectByType<PlayerStateMachine>();
        currentRobot = FindFirstObjectByType<RobotCompanion>();
    }
    
    
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            StartTest(0);
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            StartTest(1);
        }
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            StartTest(2);
        }
    }
    
    [Button]
    public  void StartTest(int testIndex)
    {
        StartCoroutine(StartTestLoadingSequence(testIndex));
    }
    
    [Button]
    public void RemoveCurrentTest()
    {
        if (!currentTest) return;
        StartCoroutine(UnLoadTest());
    }

    
    private IEnumerator StartTestLoadingSequence(int testIndex)
    {
        if (tests.Length <= 0 || testIndex >= tests.Length) yield break;
        
        if (currentTest)
        {
            int unloadTime = currentTest.GetTimeToUnload();
            StartCoroutine(UnLoadTest());
            yield return new WaitForSeconds(unloadTime);
            StartCoroutine(LoadTest(testIndex));
        }
        else
        {
            StartCoroutine(LoadTest(testIndex));
        }
        
    }
    
    
    
    private IEnumerator LoadTest(int testIndex)
    {
        if (tests.Length <= 0 || testIndex >= tests.Length) yield break;
        Debug.Log("Loading " + tests[testIndex].GetName());
        yield return new WaitForSeconds(tests[testIndex].GetTimeToLoad());
        Debug.Log("Loaded " + tests[testIndex].GetName());
        currentTest = tests[testIndex];
        currentEnvironment = Instantiate(currentTest.GetPrefab());
        TeleportPlayer(currentTest.GetSpawnPoint());
        TeleportRobot(currentTest.GetSpawnPoint() + new Vector3(0, 0, 2));
    }
    
    
    private IEnumerator UnLoadTest()
    {
        if (!currentTest) yield break;
        Debug.Log("Unloading " + currentTest.GetName());
        yield return new WaitForSeconds(currentTest.GetTimeToUnload());
        Debug.Log("Unloaded " + currentTest.GetName());
        Destroy(currentEnvironment);
        currentTest = null;
        
    }
    
    
    
    
    
    
    private void TeleportPlayer(Vector3 position)
    {
        if (!currentTest || !currentPlayer) return;
        currentPlayer.transform.position = position;
    }
    
    private void TeleportRobot(Vector3 position)
    {
        if (!currentTest || !currentRobot) return;
        currentRobot.transform.position = position;
    }
    
}
