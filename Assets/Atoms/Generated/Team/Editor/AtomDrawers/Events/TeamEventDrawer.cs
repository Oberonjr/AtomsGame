#if UNITY_2019_1_OR_NEWER
using UnityEditor;
using UnityAtoms.Editor;

namespace UnityAtoms.Editor
{
    /// <summary>
    /// Event property drawer of type `Team`. Inherits from `AtomDrawer&lt;TeamEvent&gt;`. Only availble in `UNITY_2019_1_OR_NEWER`.
    /// </summary>
    [CustomPropertyDrawer(typeof(TeamEvent))]
    public class TeamEventDrawer : AtomDrawer<TeamEvent> { }
}
#endif
