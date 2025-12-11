#if UNITY_2019_1_OR_NEWER
using UnityEditor;
using UnityAtoms.Editor;

namespace UnityAtoms.Editor
{
    /// <summary>
    /// Event property drawer of type `TroopStats_Atoms`. Inherits from `AtomDrawer&lt;TroopStats_AtomsEvent&gt;`. Only availble in `UNITY_2019_1_OR_NEWER`.
    /// </summary>
    [CustomPropertyDrawer(typeof(TroopStats_AtomsEvent))]
    public class TroopStats_AtomsEventDrawer : AtomDrawer<TroopStats_AtomsEvent> { }
}
#endif
