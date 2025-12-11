#if UNITY_2019_1_OR_NEWER
using UnityEditor;
using UnityEngine.UIElements;
using UnityAtoms.Editor;

namespace UnityAtoms.Editor
{
    /// <summary>
    /// Event property drawer of type `TroopStats_AtomsPair`. Inherits from `AtomEventEditor&lt;TroopStats_AtomsPair, TroopStats_AtomsPairEvent&gt;`. Only availble in `UNITY_2019_1_OR_NEWER`.
    /// </summary>
    [CustomEditor(typeof(TroopStats_AtomsPairEvent))]
    public sealed class TroopStats_AtomsPairEventEditor : AtomEventEditor<TroopStats_AtomsPair, TroopStats_AtomsPairEvent> { }
}
#endif
