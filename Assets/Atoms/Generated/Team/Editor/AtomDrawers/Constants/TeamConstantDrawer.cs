#if UNITY_2019_1_OR_NEWER
using UnityEditor;
using UnityAtoms.Editor;

namespace UnityAtoms.Editor
{
    /// <summary>
    /// Constant property drawer of type `Team`. Inherits from `AtomDrawer&lt;TeamConstant&gt;`. Only availble in `UNITY_2019_1_OR_NEWER`.
    /// </summary>
    [CustomPropertyDrawer(typeof(TeamConstant))]
    public class TeamConstantDrawer : VariableDrawer<TeamConstant> { }
}
#endif
