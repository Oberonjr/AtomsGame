#if UNITY_2019_1_OR_NEWER
using UnityEditor;
using UnityEngine.UIElements;
using UnityAtoms.Editor;

namespace UnityAtoms.Editor
{
    /// <summary>
    /// Event property drawer of type `TroopPair`. Inherits from `AtomEventEditor&lt;TroopPair, TroopPairEvent&gt;`. Only availble in `UNITY_2019_1_OR_NEWER`.
    /// </summary>
    [CustomEditor(typeof(TroopPairEvent))]
    public sealed class TroopPairEventEditor : AtomEventEditor<TroopPair, TroopPairEvent> { }
}
#endif
