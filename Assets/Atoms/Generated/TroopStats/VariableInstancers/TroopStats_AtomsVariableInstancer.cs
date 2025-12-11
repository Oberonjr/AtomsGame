using UnityEngine;
using UnityAtoms.BaseAtoms;

namespace UnityAtoms
{
    /// <summary>
    /// Variable Instancer of type `TroopStats_Atoms`. Inherits from `AtomVariableInstancer&lt;TroopStats_AtomsVariable, TroopStats_AtomsPair, TroopStats_Atoms, TroopStats_AtomsEvent, TroopStats_AtomsPairEvent, TroopStats_AtomsTroopStats_AtomsFunction&gt;`.
    /// </summary>
    [EditorIcon("atom-icon-hotpink")]
    [AddComponentMenu("Unity Atoms/Variable Instancers/TroopStats_Atoms Variable Instancer")]
    public class TroopStats_AtomsVariableInstancer : AtomVariableInstancer<
        TroopStats_AtomsVariable,
        TroopStats_AtomsPair,
        TroopStats_Atoms,
        TroopStats_AtomsEvent,
        TroopStats_AtomsPairEvent,
        TroopStats_AtomsTroopStats_AtomsFunction>
    { }
}
