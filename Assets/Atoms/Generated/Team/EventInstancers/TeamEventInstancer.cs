using UnityEngine;

namespace UnityAtoms
{
    /// <summary>
    /// Event Instancer of type `Team`. Inherits from `AtomEventInstancer&lt;Team, TeamEvent&gt;`.
    /// </summary>
    [EditorIcon("atom-icon-sign-blue")]
    [AddComponentMenu("Unity Atoms/Event Instancers/Team Event Instancer")]
    public class TeamEventInstancer : AtomEventInstancer<Team, TeamEvent> { }
}
