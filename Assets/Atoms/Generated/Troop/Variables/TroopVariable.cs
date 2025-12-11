using UnityEngine;

namespace UnityAtoms
{
    /// <summary>
    /// Variable of type `Troop`. Inherits from `EquatableAtomVariable&lt;Troop, TroopPair, TroopEvent, TroopPairEvent, TroopTroopFunction&gt;`.
    /// </summary>
    [EditorIcon("atom-icon-lush")]
    [CreateAssetMenu(menuName = "Unity Atoms/Variables/Troop", fileName = "TroopVariable")]
    public sealed class TroopVariable : EquatableAtomVariable<Troop, TroopPair, TroopEvent, TroopPairEvent, TroopTroopFunction>
    {
    }
}
