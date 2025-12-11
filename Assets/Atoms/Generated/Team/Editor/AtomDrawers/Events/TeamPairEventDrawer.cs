#if UNITY_2019_1_OR_NEWER
using UnityEditor;
using UnityAtoms.Editor;

namespace UnityAtoms.Editor
{
    /// <summary>
    /// Event property drawer of type `TeamPair`. Inherits from `AtomDrawer&lt;TeamPairEvent&gt;`. Only availble in `UNITY_2019_1_OR_NEWER`.
    /// </summary>
    [CustomPropertyDrawer(typeof(TeamPairEvent))]
    public class TeamPairEventDrawer : AtomDrawer<TeamPairEvent> { }
}
#endif
