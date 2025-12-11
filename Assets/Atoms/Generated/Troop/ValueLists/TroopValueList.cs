using UnityEngine;

namespace UnityAtoms
{
    /// <summary>
    /// Value List of type `Troop`. Inherits from `AtomValueList&lt;Troop, TroopEvent&gt;`.
    /// </summary>
    [EditorIcon("atom-icon-piglet")]
    [CreateAssetMenu(menuName = "Unity Atoms/Value Lists/Troop", fileName = "TroopValueList")]
    public sealed class TroopValueList : AtomValueList<Troop, TroopEvent> { }
}
