using UnityEngine;

namespace UnityAtoms
{
    /// <summary>
    /// Event Instancer of type `TeamPair`. Inherits from `AtomEventInstancer&lt;TeamPair, TeamPairEvent&gt;`.
    /// </summary>
    [EditorIcon("atom-icon-sign-blue")]
    [AddComponentMenu("Unity Atoms/Event Instancers/TeamPair Event Instancer")]
    public class TeamPairEventInstancer : AtomEventInstancer<TeamPair, TeamPairEvent> { }
}
