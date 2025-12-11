#if UNITY_2019_1_OR_NEWER
using UnityEditor;
using UnityEngine.UIElements;
using UnityAtoms.Editor;

namespace UnityAtoms.Editor
{
    /// <summary>
    /// Event property drawer of type `Troop`. Inherits from `AtomEventEditor&lt;Troop, TroopEvent&gt;`. Only availble in `UNITY_2019_1_OR_NEWER`.
    /// </summary>
    [CustomEditor(typeof(TroopEvent))]
    public sealed class TroopEventEditor : AtomEventEditor<Troop, TroopEvent> { }
}
#endif
