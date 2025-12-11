using UnityEngine;

namespace UnityAtoms
{
    /// <summary>
    /// Event Reference Listener of type `Team`. Inherits from `AtomEventReferenceListener&lt;Team, TeamEvent, TeamEventReference, TeamUnityEvent&gt;`.
    /// </summary>
    [EditorIcon("atom-icon-orange")]
    [AddComponentMenu("Unity Atoms/Listeners/Team Event Reference Listener")]
    public sealed class TeamEventReferenceListener : AtomEventReferenceListener<
        Team,
        TeamEvent,
        TeamEventReference,
        TeamUnityEvent>
    { }
}
