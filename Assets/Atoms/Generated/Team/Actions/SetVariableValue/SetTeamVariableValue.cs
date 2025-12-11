using UnityEngine;
using UnityAtoms.BaseAtoms;

namespace UnityAtoms
{
    /// <summary>
    /// Set variable value Action of type `Team`. Inherits from `SetVariableValue&lt;Team, TeamPair, TeamVariable, TeamConstant, TeamReference, TeamEvent, TeamPairEvent, TeamVariableInstancer&gt;`.
    /// </summary>
    [EditorIcon("atom-icon-purple")]
    [CreateAssetMenu(menuName = "Unity Atoms/Actions/Set Variable Value/Team", fileName = "SetTeamVariableValue")]
    public sealed class SetTeamVariableValue : SetVariableValue<
        Team,
        TeamPair,
        TeamVariable,
        TeamConstant,
        TeamReference,
        TeamEvent,
        TeamPairEvent,
        TeamTeamFunction,
        TeamVariableInstancer>
    { }
}
