#if UNITY_2019_1_OR_NEWER
using UnityEditor;
using UnityAtoms.Editor;

namespace UnityAtoms.Editor
{
    /// <summary>
    /// Variable property drawer of type `TroopStats_Atoms`. Inherits from `AtomDrawer&lt;TroopStats_AtomsVariable&gt;`. Only availble in `UNITY_2019_1_OR_NEWER`.
    /// </summary>
    [CustomPropertyDrawer(typeof(TroopStats_AtomsVariable))]
    public class TroopStats_AtomsVariableDrawer : VariableDrawer<TroopStats_AtomsVariable> { }
}
#endif
