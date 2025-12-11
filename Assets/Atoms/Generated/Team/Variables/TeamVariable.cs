using UnityEngine;

namespace UnityAtoms
{
    /// <summary>
    /// Variable of type `Team`. Inherits from `EquatableAtomVariable&lt;Team, TeamPair, TeamEvent, TeamPairEvent, TeamTeamFunction&gt;`.
    /// </summary>
    [EditorIcon("atom-icon-lush")]
    [CreateAssetMenu(menuName = "Unity Atoms/Variables/Team", fileName = "TeamVariable")]
    public sealed class TeamVariable : EquatableAtomVariable<Team, TeamPair, TeamEvent, TeamPairEvent, TeamTeamFunction>
    {
    }
}
