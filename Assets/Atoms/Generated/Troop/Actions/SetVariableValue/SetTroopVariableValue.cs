using UnityEngine;
using UnityAtoms.BaseAtoms;

namespace UnityAtoms
{
    /// <summary>
    /// Set variable value Action of type `Troop`. Inherits from `SetVariableValue&lt;Troop, TroopPair, TroopVariable, TroopConstant, TroopReference, TroopEvent, TroopPairEvent, TroopVariableInstancer&gt;`.
    /// </summary>
    [EditorIcon("atom-icon-purple")]
    [CreateAssetMenu(menuName = "Unity Atoms/Actions/Set Variable Value/Troop", fileName = "SetTroopVariableValue")]
    public sealed class SetTroopVariableValue : SetVariableValue<
        Troop,
        TroopPair,
        TroopVariable,
        TroopConstant,
        TroopReference,
        TroopEvent,
        TroopPairEvent,
        TroopTroopFunction,
        TroopVariableInstancer>
    { }
}
