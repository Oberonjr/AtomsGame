using UnityEngine;

namespace UnityAtoms
{
    /// <summary>
    /// Value List of type `Team`. Inherits from `AtomValueList&lt;Team, TeamEvent&gt;`.
    /// </summary>
    [EditorIcon("atom-icon-piglet")]
    [CreateAssetMenu(menuName = "Unity Atoms/Value Lists/Team", fileName = "TeamValueList")]
    public sealed class TeamValueList : AtomValueList<Team, TeamEvent> { }
}
