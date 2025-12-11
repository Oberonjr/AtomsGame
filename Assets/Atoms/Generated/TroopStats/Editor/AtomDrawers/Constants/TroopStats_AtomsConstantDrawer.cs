#if UNITY_2019_1_OR_NEWER
using UnityEditor;
using UnityAtoms.Editor;

namespace UnityAtoms.Editor
{
    /// <summary>
    /// Constant property drawer of type `TroopStats_Atoms`. Inherits from `AtomDrawer&lt;TroopStats_AtomsConstant&gt;`. Only availble in `UNITY_2019_1_OR_NEWER`.
    /// </summary>
    [CustomPropertyDrawer(typeof(TroopStats_AtomsConstant))]
    public class TroopStats_AtomsConstantDrawer : VariableDrawer<TroopStats_AtomsConstant> { }
}
#endif
