using UnityEngine;

namespace UnityAtoms
{
    /// <summary>
    /// Event Reference Listener of type `TroopStats_AtomsPair`. Inherits from `AtomEventReferenceListener&lt;TroopStats_AtomsPair, TroopStats_AtomsPairEvent, TroopStats_AtomsPairEventReference, TroopStats_AtomsPairUnityEvent&gt;`.
    /// </summary>
    [EditorIcon("atom-icon-orange")]
    [AddComponentMenu("Unity Atoms/Listeners/TroopStats_AtomsPair Event Reference Listener")]
    public sealed class TroopStats_AtomsPairEventReferenceListener : AtomEventReferenceListener<
        TroopStats_AtomsPair,
        TroopStats_AtomsPairEvent,
        TroopStats_AtomsPairEventReference,
        TroopStats_AtomsPairUnityEvent>
    { }
}
