using UnityEngine;

namespace UnityAtoms
{
    /// <summary>
    /// Event Reference Listener of type `TeamPair`. Inherits from `AtomEventReferenceListener&lt;TeamPair, TeamPairEvent, TeamPairEventReference, TeamPairUnityEvent&gt;`.
    /// </summary>
    [EditorIcon("atom-icon-orange")]
    [AddComponentMenu("Unity Atoms/Listeners/TeamPair Event Reference Listener")]
    public sealed class TeamPairEventReferenceListener : AtomEventReferenceListener<
        TeamPair,
        TeamPairEvent,
        TeamPairEventReference,
        TeamPairUnityEvent>
    { }
}
