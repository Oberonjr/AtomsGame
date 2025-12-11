using UnityEngine;

namespace UnityAtoms
{
    /// <summary>
    /// Event of type `Troop`. Inherits from `AtomEvent&lt;Troop&gt;`.
    /// </summary>
    [EditorIcon("atom-icon-cherry")]
    [CreateAssetMenu(menuName = "Unity Atoms/Events/Troop", fileName = "TroopEvent")]
    public sealed class TroopEvent : AtomEvent<Troop>
    {
    }
}
