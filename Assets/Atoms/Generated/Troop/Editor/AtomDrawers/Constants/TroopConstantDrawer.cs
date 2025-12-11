#if UNITY_2019_1_OR_NEWER
using UnityEditor;
using UnityAtoms.Editor;

namespace UnityAtoms.Editor
{
    /// <summary>
    /// Constant property drawer of type `Troop`. Inherits from `AtomDrawer&lt;TroopConstant&gt;`. Only availble in `UNITY_2019_1_OR_NEWER`.
    /// </summary>
    [CustomPropertyDrawer(typeof(TroopConstant))]
    public class TroopConstantDrawer : VariableDrawer<TroopConstant> { }
}
#endif
