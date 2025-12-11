using System;

namespace UnityAtoms
{
    /// <summary>
    /// Event Reference of type `TroopStats_Atoms`. Inherits from `AtomEventReference&lt;TroopStats_Atoms, TroopStats_AtomsVariable, TroopStats_AtomsEvent, TroopStats_AtomsVariableInstancer, TroopStats_AtomsEventInstancer&gt;`.
    /// </summary>
    [Serializable]
    public sealed class TroopStats_AtomsEventReference : AtomEventReference<
        TroopStats_Atoms,
        TroopStats_AtomsVariable,
        TroopStats_AtomsEvent,
        TroopStats_AtomsVariableInstancer,
        TroopStats_AtomsEventInstancer>, IGetEvent 
    { }
}
