#if UNITY_2019_1_OR_NEWER
using UnityEditor;
using UnityEngine.UIElements;
using UnityAtoms.Editor;

namespace UnityAtoms.Editor
{
    /// <summary>
    /// Event property drawer of type `TeamPair`. Inherits from `AtomEventEditor&lt;TeamPair, TeamPairEvent&gt;`. Only availble in `UNITY_2019_1_OR_NEWER`.
    /// </summary>
    [CustomEditor(typeof(TeamPairEvent))]
    public sealed class TeamPairEventEditor : AtomEventEditor<TeamPair, TeamPairEvent> { }
}
#endif
