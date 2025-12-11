using System;

namespace UnityAtoms
{
    /// <summary>
    /// Event Reference of type `Team`. Inherits from `AtomEventReference&lt;Team, TeamVariable, TeamEvent, TeamVariableInstancer, TeamEventInstancer&gt;`.
    /// </summary>
    [Serializable]
    public sealed class TeamEventReference : AtomEventReference<
        Team,
        TeamVariable,
        TeamEvent,
        TeamVariableInstancer,
        TeamEventInstancer>, IGetEvent 
    { }
}
