using UnityEngine;

namespace UnityAtoms
{
    /// <summary>
    /// Value List of type `TroopStats_Atoms`. Inherits from `AtomValueList&lt;TroopStats_Atoms, TroopStats_AtomsEvent&gt;`.
    /// </summary>
    [EditorIcon("atom-icon-piglet")]
    [CreateAssetMenu(menuName = "Unity Atoms/Value Lists/TroopStats_Atoms", fileName = "TroopStats_AtomsValueList")]
    public sealed class TroopStats_AtomsValueList : AtomValueList<TroopStats_Atoms, TroopStats_AtomsEvent> { }
}
