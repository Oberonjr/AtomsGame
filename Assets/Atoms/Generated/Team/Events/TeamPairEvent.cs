using UnityEngine;

namespace UnityAtoms
{
    /// <summary>
    /// Event of type `TeamPair`. Inherits from `AtomEvent&lt;TeamPair&gt;`.
    /// </summary>
    [EditorIcon("atom-icon-cherry")]
    [CreateAssetMenu(menuName = "Unity Atoms/Events/TeamPair", fileName = "TeamPairEvent")]
    public sealed class TeamPairEvent : AtomEvent<TeamPair>
    {
    }
}
