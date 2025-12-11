#if UNITY_2019_1_OR_NEWER
using UnityEditor;
using UnityEngine.UIElements;
using UnityAtoms.Editor;

namespace UnityAtoms.Editor
{
    /// <summary>
    /// Event property drawer of type `TroopStats_Atoms`. Inherits from `AtomEventEditor&lt;TroopStats_Atoms, TroopStats_AtomsEvent&gt;`. Only availble in `UNITY_2019_1_OR_NEWER`.
    /// </summary>
    [CustomEditor(typeof(TroopStats_AtomsEvent))]
    public sealed class TroopStats_AtomsEventEditor : AtomEventEditor<TroopStats_Atoms, TroopStats_AtomsEvent> { }
}
#endif
