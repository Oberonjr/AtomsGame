using UnityEngine;
using UnityAtoms.BaseAtoms;

namespace UnityAtoms
{
    /// <summary>
    /// Variable Instancer of type `Troop`. Inherits from `AtomVariableInstancer&lt;TroopVariable, TroopPair, Troop, TroopEvent, TroopPairEvent, TroopTroopFunction&gt;`.
    /// </summary>
    [EditorIcon("atom-icon-hotpink")]
    [AddComponentMenu("Unity Atoms/Variable Instancers/Troop Variable Instancer")]
    public class TroopVariableInstancer : AtomVariableInstancer<
        TroopVariable,
        TroopPair,
        Troop,
        TroopEvent,
        TroopPairEvent,
        TroopTroopFunction>
    { }
}
