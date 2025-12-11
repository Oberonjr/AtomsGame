#if UNITY_2019_1_OR_NEWER
using UnityEditor;
using UnityAtoms.Editor;

namespace UnityAtoms.Editor
{
    /// <summary>
    /// Value List property drawer of type `TroopStats_Atoms`. Inherits from `AtomDrawer&lt;TroopStats_AtomsValueList&gt;`. Only availble in `UNITY_2019_1_OR_NEWER`.
    /// </summary>
    [CustomPropertyDrawer(typeof(TroopStats_AtomsValueList))]
    public class TroopStats_AtomsValueListDrawer : AtomDrawer<TroopStats_AtomsValueList> { }
}
#endif
