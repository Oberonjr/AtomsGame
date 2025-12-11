using UnityEditor;
using UnityAtoms.Editor;

namespace UnityAtoms.Editor
{
    /// <summary>
    /// Variable Inspector of type `TroopStats_Atoms`. Inherits from `AtomVariableEditor`
    /// </summary>
    [CustomEditor(typeof(TroopStats_AtomsVariable))]
    public sealed class TroopStats_AtomsVariableEditor : AtomVariableEditor<TroopStats_Atoms, TroopStats_AtomsPair> { }
}
