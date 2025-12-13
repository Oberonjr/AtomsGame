using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TroopFSM
{
    private State _currentState;
    
    public State CurrentState => _currentState;

    public void ChangeState(State newState)
    {
        if (PerformanceProfiler.Instance != null && _currentState != null)
        {
            string fromState = _currentState.GetType().Name;
            string toState = newState.GetType().Name;
            PerformanceProfiler.Instance.RecordStateTransition(fromState, toState);
        }

        _currentState?.Exit();
        _currentState = newState;
        _currentState.Enter();
    }

    public void Update()
    {
        _currentState?.Update();
    }
}
