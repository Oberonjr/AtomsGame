using UnityEditor;
using UnityAtoms.Editor;

namespace UnityAtoms.Editor
{
    /// <summary>
    /// Variable Inspector of type `Troop`. Inherits from `AtomVariableEditor`
    /// </summary>
    [CustomEditor(typeof(TroopVariable))]
    public sealed class TroopVariableEditor : AtomVariableEditor<Troop, TroopPair> { }
}
