using System;

namespace UnityAtoms
{
    /// <summary>
    /// Event Reference of type `Troop`. Inherits from `AtomEventReference&lt;Troop, TroopVariable, TroopEvent, TroopVariableInstancer, TroopEventInstancer&gt;`.
    /// </summary>
    [Serializable]
    public sealed class TroopEventReference : AtomEventReference<
        Troop,
        TroopVariable,
        TroopEvent,
        TroopVariableInstancer,
        TroopEventInstancer>, IGetEvent 
    { }
}
