using System;
using UnityEngine.Events;

namespace UnityAtoms
{
    /// <summary>
    /// None generic Unity Event of type `Team`. Inherits from `UnityEvent&lt;Team&gt;`.
    /// </summary>
    [Serializable]
    public sealed class TeamUnityEvent : UnityEvent<Team> { }
}
