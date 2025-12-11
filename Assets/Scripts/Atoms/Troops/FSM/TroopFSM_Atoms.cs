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