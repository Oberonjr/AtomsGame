using System;

namespace UnityAtoms
{
    /// <summary>
    /// Event Reference of type `TroopStats_AtomsPair`. Inherits from `AtomEventReference&lt;TroopStats_AtomsPair, TroopStats_AtomsVariable, TroopStats_AtomsPairEvent, TroopStats_AtomsVariableInstancer, TroopStats_AtomsPairEventInstancer&gt;`.
    /// </summary>
    [Serializable]
    public sealed class TroopStats_AtomsPairEventReference : AtomEventReference<
        TroopStats_AtomsPair,
        TroopStats_AtomsVariable,
        TroopStats_AtomsPairEvent,
        TroopStats_AtomsVariableInstancer,
        TroopStats_AtomsPairEventInstancer>, IGetEvent 
    { }
}
