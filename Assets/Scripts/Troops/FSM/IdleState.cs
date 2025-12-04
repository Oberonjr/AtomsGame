using UnityEngine;

public class IdleState : State
{
    private Troop _troop;

    public IdleState(Troop troop)
    {
        _troop = troop;
    }

    public void Enter() { /* Play idle animation, etc. */ }
    public void Update()
    {
        // Look for new target if needed
    }
    public void Exit() { }
}