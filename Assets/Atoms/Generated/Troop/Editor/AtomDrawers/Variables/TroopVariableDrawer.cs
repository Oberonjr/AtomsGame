#if UNITY_2019_1_OR_NEWER
using UnityEditor;
using UnityAtoms.Editor;

namespace UnityAtoms.Editor
{
    /// <summary>
    /// Variable property drawer of type `Troop`. Inherits from `AtomDrawer&lt;TroopVariable&gt;`. Only availble in `UNITY_2019_1_OR_NEWER`.
    /// </summary>
    [CustomPropertyDrawer(typeof(TroopVariable))]
    public class TroopVariableDrawer : VariableDrawer<TroopVariable> { }
}
#endif
