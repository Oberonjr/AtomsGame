using UnityEngine;
using UnityAtoms.BaseAtoms;

namespace UnityAtoms
{
    /// <summary>
    /// Variable Instancer of type `Team`. Inherits from `AtomVariableInstancer&lt;TeamVariable, TeamPair, Team, TeamEvent, TeamPairEvent, TeamTeamFunction&gt;`.
    /// </summary>
    [EditorIcon("atom-icon-hotpink")]
    [AddComponentMenu("Unity Atoms/Variable Instancers/Team Variable Instancer")]
    public class TeamVariableInstancer : AtomVariableInstancer<
        TeamVariable,
        TeamPair,
        Team,
        TeamEvent,
        TeamPairEvent,
        TeamTeamFunction>
    { }
}
