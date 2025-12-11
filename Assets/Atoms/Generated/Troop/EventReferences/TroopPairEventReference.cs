using System;

namespace UnityAtoms
{
    /// <summary>
    /// Event Reference of type `TroopPair`. Inherits from `AtomEventReference&lt;TroopPair, TroopVariable, TroopPairEvent, TroopVariableInstancer, TroopPairEventInstancer&gt;`.
    /// </summary>
    [Serializable]
    public sealed class TroopPairEventReference : AtomEventReference<
        TroopPair,
        TroopVariable,
        TroopPairEvent,
        TroopVariableInstancer,
        TroopPairEventInstancer>, IGetEvent 
    { }
}
