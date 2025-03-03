using System;
using UnityEngine;

public class TestEndPlatform : MonoBehaviour
{
    
    private TestManager _testManager;

    private void Awake()
    {
        _testManager = TestManager.Instance;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!_testManager)
        {
            Debug.LogError("TestManager is null");
            return;
        }
        
        if (other.TryGetComponent(out PlayerStateMachine player))
        {
            _testManager.LoadNextTest();
        }
    }
}
