using UnityEngine;

namespace UnityAtoms
{
    /// <summary>
    /// Event of type `TroopPair`. Inherits from `AtomEvent&lt;TroopPair&gt;`.
    /// </summary>
    [EditorIcon("atom-icon-cherry")]
    [CreateAssetMenu(menuName = "Unity Atoms/Events/TroopPair", fileName = "TroopPairEvent")]
    public sealed class TroopPairEvent : AtomEvent<TroopPair>
    {
    }
}
