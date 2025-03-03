using System;
using UnityEngine;

public class TestSpawnPlatform : MonoBehaviour
{
    private bool _hasReached;
    private TestManager _testManager;
    
    private void Awake()
    {
        _testManager = TestManager.Instance;
    }
    private void OnTriggerEnter(Collider other)
    {
        if (_hasReached) return;
        
        if (!_testManager)
        {
            Debug.LogError("TestManager is null");
            return;
        }
        
        
        if (other.TryGetComponent(out PlayerStateMachine player))
        {
            _hasReached = true;
            _testManager.SetCheckpointPosition(transform);
        }
    }
}
