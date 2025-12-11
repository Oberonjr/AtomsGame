#if UNITY_2019_1_OR_NEWER
using UnityEditor;
using UnityAtoms.Editor;

namespace UnityAtoms.Editor
{
    /// <summary>
    /// Variable property drawer of type `Team`. Inherits from `AtomDrawer&lt;TeamVariable&gt;`. Only availble in `UNITY_2019_1_OR_NEWER`.
    /// </summary>
    [CustomPropertyDrawer(typeof(TeamVariable))]
    public class TeamVariableDrawer : VariableDrawer<TeamVariable> { }
}
#endif
