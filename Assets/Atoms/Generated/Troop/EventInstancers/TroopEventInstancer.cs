using UnityEngine;

namespace UnityAtoms
{
    /// <summary>
    /// Event Instancer of type `Troop`. Inherits from `AtomEventInstancer&lt;Troop, TroopEvent&gt;`.
    /// </summary>
    [EditorIcon("atom-icon-sign-blue")]
    [AddComponentMenu("Unity Atoms/Event Instancers/Troop Event Instancer")]
    public class TroopEventInstancer : AtomEventInstancer<Troop, TroopEvent> { }
}
