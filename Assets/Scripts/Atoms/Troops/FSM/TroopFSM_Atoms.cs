using UnityEngine;

/// <summary>
/// Finite State Machine for Atoms troops
/// </summary>
public class TroopFSM_Atoms
{
    private IState_Atoms _currentState;

    public IState_Atoms CurrentState => _currentState;

    public void ChangeState(IState_Atoms newState)
    {
        if (PerformanceProfiler.Instance != null && _currentState != null)
        {
            string fromState = _currentState.GetType().Name;
            string toState = newState.GetType().Name;
            PerformanceProfiler.Instance.RecordStateTransition(fromState, toState);
        }

        if (_currentState != null)
        {
            _currentState.Exit();
        }

        _currentState = newState;

        if (_currentState != null)
        {
            _currentState.Enter();
        }
    }

    public void Update()
    {
        _currentState?.Update();
    }
}