using System;
using UnityEngine.Events;

namespace UnityAtoms
{
    /// <summary>
    /// None generic Unity Event of type `Troop`. Inherits from `UnityEvent&lt;Troop&gt;`.
    /// </summary>
    [Serializable]
    public sealed class TroopUnityEvent : UnityEvent<Troop> { }
}
