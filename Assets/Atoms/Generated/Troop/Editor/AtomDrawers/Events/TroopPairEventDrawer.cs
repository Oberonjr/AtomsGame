#if UNITY_2019_1_OR_NEWER
using UnityEditor;
using UnityAtoms.Editor;

namespace UnityAtoms.Editor
{
    /// <summary>
    /// Event property drawer of type `TroopPair`. Inherits from `AtomDrawer&lt;TroopPairEvent&gt;`. Only availble in `UNITY_2019_1_OR_NEWER`.
    /// </summary>
    [CustomPropertyDrawer(typeof(TroopPairEvent))]
    public class TroopPairEventDrawer : AtomDrawer<TroopPairEvent> { }
}
#endif
