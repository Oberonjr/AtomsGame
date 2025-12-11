#if UNITY_2019_1_OR_NEWER
using UnityEditor;
using UnityAtoms.Editor;

namespace UnityAtoms.Editor
{
    /// <summary>
    /// Value List property drawer of type `Team`. Inherits from `AtomDrawer&lt;TeamValueList&gt;`. Only availble in `UNITY_2019_1_OR_NEWER`.
    /// </summary>
    [CustomPropertyDrawer(typeof(TeamValueList))]
    public class TeamValueListDrawer : AtomDrawer<TeamValueList> { }
}
#endif
