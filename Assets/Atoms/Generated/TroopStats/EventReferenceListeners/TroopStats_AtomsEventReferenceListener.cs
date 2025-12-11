using UnityEngine;

namespace UnityAtoms
{
    /// <summary>
    /// Event Reference Listener of type `TroopStats_Atoms`. Inherits from `AtomEventReferenceListener&lt;TroopStats_Atoms, TroopStats_AtomsEvent, TroopStats_AtomsEventReference, TroopStats_AtomsUnityEvent&gt;`.
    /// </summary>
    [EditorIcon("atom-icon-orange")]
    [AddComponentMenu("Unity Atoms/Listeners/TroopStats_Atoms Event Reference Listener")]
    public sealed class TroopStats_AtomsEventReferenceListener : AtomEventReferenceListener<
        TroopStats_Atoms,
        TroopStats_AtomsEvent,
        TroopStats_AtomsEventReference,
        TroopStats_AtomsUnityEvent>
    { }
}
