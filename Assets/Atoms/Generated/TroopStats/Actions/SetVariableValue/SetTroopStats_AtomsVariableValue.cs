using UnityEngine;
using UnityAtoms.BaseAtoms;

namespace UnityAtoms
{
    /// <summary>
    /// Set variable value Action of type `TroopStats_Atoms`. Inherits from `SetVariableValue&lt;TroopStats_Atoms, TroopStats_AtomsPair, TroopStats_AtomsVariable, TroopStats_AtomsConstant, TroopStats_AtomsReference, TroopStats_AtomsEvent, TroopStats_AtomsPairEvent, TroopStats_AtomsVariableInstancer&gt;`.
    /// </summary>
    [EditorIcon("atom-icon-purple")]
    [CreateAssetMenu(menuName = "Unity Atoms/Actions/Set Variable Value/TroopStats_Atoms", fileName = "SetTroopStats_AtomsVariableValue")]
    public sealed class SetTroopStats_AtomsVariableValue : SetVariableValue<
        TroopStats_Atoms,
        TroopStats_AtomsPair,
        TroopStats_AtomsVariable,
        TroopStats_AtomsConstant,
        TroopStats_AtomsReference,
        TroopStats_AtomsEvent,
        TroopStats_AtomsPairEvent,
        TroopStats_AtomsTroopStats_AtomsFunction,
        TroopStats_AtomsVariableInstancer>
    { }
}
