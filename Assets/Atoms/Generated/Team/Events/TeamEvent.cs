using UnityEngine;

namespace UnityAtoms
{
    /// <summary>
    /// Event of type `Team`. Inherits from `AtomEvent&lt;Team&gt;`.
    /// </summary>
    [EditorIcon("atom-icon-cherry")]
    [CreateAssetMenu(menuName = "Unity Atoms/Events/Team", fileName = "TeamEvent")]
    public sealed class TeamEvent : AtomEvent<Team>
    {
    }
}
