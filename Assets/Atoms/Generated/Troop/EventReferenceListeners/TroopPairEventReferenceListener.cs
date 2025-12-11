using UnityEngine;

namespace UnityAtoms
{
    /// <summary>
    /// Event Reference Listener of type `TroopPair`. Inherits from `AtomEventReferenceListener&lt;TroopPair, TroopPairEvent, TroopPairEventReference, TroopPairUnityEvent&gt;`.
    /// </summary>
    [EditorIcon("atom-icon-orange")]
    [AddComponentMenu("Unity Atoms/Listeners/TroopPair Event Reference Listener")]
    public sealed class TroopPairEventReferenceListener : AtomEventReferenceListener<
        TroopPair,
        TroopPairEvent,
        TroopPairEventReference,
        TroopPairUnityEvent>
    { }
}
