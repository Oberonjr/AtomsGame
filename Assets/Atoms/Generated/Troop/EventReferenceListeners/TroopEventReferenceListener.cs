using UnityEngine;

namespace UnityAtoms
{
    /// <summary>
    /// Event Reference Listener of type `Troop`. Inherits from `AtomEventReferenceListener&lt;Troop, TroopEvent, TroopEventReference, TroopUnityEvent&gt;`.
    /// </summary>
    [EditorIcon("atom-icon-orange")]
    [AddComponentMenu("Unity Atoms/Listeners/Troop Event Reference Listener")]
    public sealed class TroopEventReferenceListener : AtomEventReferenceListener<
        Troop,
        TroopEvent,
        TroopEventReference,
        TroopUnityEvent>
    { }
}
