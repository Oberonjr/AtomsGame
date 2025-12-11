#if UNITY_2019_1_OR_NEWER
using UnityEditor;
using UnityAtoms.Editor;

namespace UnityAtoms.Editor
{
    /// <summary>
    /// Event property drawer of type `TroopStats_AtomsPair`. Inherits from `AtomDrawer&lt;TroopStats_AtomsPairEvent&gt;`. Only availble in `UNITY_2019_1_OR_NEWER`.
    /// </summary>
    [CustomPropertyDrawer(typeof(TroopStats_AtomsPairEvent))]
    public class TroopStats_AtomsPairEventDrawer : AtomDrawer<TroopStats_AtomsPairEvent> { }
}
#endif
