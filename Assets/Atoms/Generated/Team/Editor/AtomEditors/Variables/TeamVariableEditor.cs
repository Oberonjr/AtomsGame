using UnityEditor;
using UnityAtoms.Editor;

namespace UnityAtoms.Editor
{
    /// <summary>
    /// Variable Inspector of type `Team`. Inherits from `AtomVariableEditor`
    /// </summary>
    [CustomEditor(typeof(TeamVariable))]
    public sealed class TeamVariableEditor : AtomVariableEditor<Team, TeamPair> { }
}
