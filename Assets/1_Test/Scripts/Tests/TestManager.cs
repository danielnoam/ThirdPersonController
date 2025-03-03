using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;
using VInspector;

public class TestManager : MonoBehaviour
{
    public static TestManager Instance { get; private set; }
    
    
    
    public SOTest[] tests;
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private GameObject robotPrefab;
    [SerializeField, ReadOnly] private SOTest currentTest;
    [SerializeField, ReadOnly] private GameObject currentEnvironment;
    [SerializeField, ReadOnly] private PlayerStateMachine currentPlayer;
    [SerializeField, ReadOnly] private RobotCompanion currentRobot;
    [SerializeField, ReadOnly] private Transform currentCheckpoint;
    
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
        }
    }
    
    
    private void Start()
    {
        currentPlayer = FindFirstObjectByType<PlayerStateMachine>();
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
    
    private void OnPlayerDeath()
    {
        if (!currentCheckpoint)
        {
            TeleportPlayer(currentTest.GetSpawnPoint());
            TeleportRobot(currentTest.GetSpawnPoint() + new Vector3(0, 0, 2));
        }
        else
        {
            TeleportPlayer(currentCheckpoint.position);
            TeleportRobot(currentCheckpoint.position + new Vector3(0, 0, 2));
        }
    }


    #region Public methods ----------------------------------------------------------------------------

    [Button]
    public void StartTest(int testIndex)
    {
        StartCoroutine(StartTestLoadingSequence(testIndex));
    }
    
    [Button]
    public void RemoveCurrentTest()
    {
        if (!currentTest) return;
        StartCoroutine(UnLoadTest());
    }
    
    [Button]
    public void LoadNextTest()
    {
        // Get the current test index
        int currentTestIndex = Array.IndexOf(tests, currentTest);
        if (currentTestIndex == -1)
        {
            Debug.Log("Current test not found in the tests array");
            return;
        }
        
        // Load the next test
        if (currentTestIndex < tests.Length - 1)
        {
            StartCoroutine(StartTestLoadingSequence(currentTestIndex + 1));
        }
        else
        {
            Debug.Log("No more tests to load");
        }
    }
    
    public void SetCheckpointPosition(Transform checkpoint)
    {
        currentCheckpoint = checkpoint;
    }
    
    #endregion Public methods ----------------------------------------------------------------------------
    
    
    
    #region Private methods ----------------------------------------------------------------------------
    
    
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
        Debug.Log("Loading... " + tests[testIndex].GetName());
        yield return new WaitForSeconds(tests[testIndex].GetTimeToLoad());
        Debug.Log("Loaded " + tests[testIndex].GetName());
        currentTest = tests[testIndex];
        currentEnvironment = Instantiate(currentTest.GetPrefab());

        if (currentTest.HasRobot()) // The new test has a robot in it
        {
            if (currentRobot) Destroy(currentRobot.gameObject);
            currentRobot = FindFirstObjectByType<RobotCompanion>();
            
        } else if (!currentRobot && robotPrefab) // The new test has no robot and there is no robot in the scene
        {
            GameObject newRobot = Instantiate(robotPrefab);
            currentRobot = newRobot.GetComponent<RobotCompanion>();
            currentRobot.TurnOn();
            currentRobot.FollowPlayer();
            TeleportRobot(currentTest.GetSpawnPoint() + new Vector3(2, 1, 0));
        }
        else
        {
            TeleportRobot(currentTest.GetSpawnPoint() + new Vector3(2, 1, 0));
        }
        
        TeleportPlayer(currentTest.GetSpawnPoint());
        
    }
    
    
    private IEnumerator UnLoadTest()
    {
        if (!currentTest) yield break;
        Debug.Log("Unloading... " + currentTest.GetName());
        yield return new WaitForSeconds(currentTest.GetTimeToUnload());
        Debug.Log("Unloaded " + currentTest.GetName());
        Destroy(currentEnvironment);
        currentTest = null;
        currentEnvironment = null;
        currentCheckpoint = null;
        
    }
    
    
    private void TeleportPlayer(Vector3 position)
    {
        if (!currentPlayer) return;
        currentPlayer.transform.position = position;
    }
    
    private void TeleportRobot(Vector3 position)
    {
        if (!currentRobot) return;
        currentRobot.transform.position = position;
    }

    #endregion Private methods ----------------------------------------------------------------------------
    



    
}
