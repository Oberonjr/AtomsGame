using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface State
{
    public abstract void Enter();
    public abstract void Update();
    public abstract void Exit();
}
