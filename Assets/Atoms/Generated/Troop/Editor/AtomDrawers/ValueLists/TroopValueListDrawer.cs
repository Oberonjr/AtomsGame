#if UNITY_2019_1_OR_NEWER
using UnityEditor;
using UnityAtoms.Editor;

namespace UnityAtoms.Editor
{
    /// <summary>
    /// Value List property drawer of type `Troop`. Inherits from `AtomDrawer&lt;TroopValueList&gt;`. Only availble in `UNITY_2019_1_OR_NEWER`.
    /// </summary>
    [CustomPropertyDrawer(typeof(TroopValueList))]
    public class TroopValueListDrawer : AtomDrawer<TroopValueList> { }
}
#endif
