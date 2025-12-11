using UnityEngine;

namespace UnityAtoms
{
    /// <summary>
    /// Event Instancer of type `TroopPair`. Inherits from `AtomEventInstancer&lt;TroopPair, TroopPairEvent&gt;`.
    /// </summary>
    [EditorIcon("atom-icon-sign-blue")]
    [AddComponentMenu("Unity Atoms/Event Instancers/TroopPair Event Instancer")]
    public class TroopPairEventInstancer : AtomEventInstancer<TroopPair, TroopPairEvent> { }
}
