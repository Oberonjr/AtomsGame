using System;

namespace UnityAtoms
{
    /// <summary>
    /// Event Reference of type `TeamPair`. Inherits from `AtomEventReference&lt;TeamPair, TeamVariable, TeamPairEvent, TeamVariableInstancer, TeamPairEventInstancer&gt;`.
    /// </summary>
    [Serializable]
    public sealed class TeamPairEventReference : AtomEventReference<
        TeamPair,
        TeamVariable,
        TeamPairEvent,
        TeamVariableInstancer,
        TeamPairEventInstancer>, IGetEvent 
    { }
}
