using System;
using UnityAtoms.BaseAtoms;

namespace UnityAtoms
{
    /// <summary>
    /// Reference of type `Troop`. Inherits from `EquatableAtomReference&lt;Troop, TroopPair, TroopConstant, TroopVariable, TroopEvent, TroopPairEvent, TroopTroopFunction, TroopVariableInstancer, AtomCollection, AtomList&gt;`.
    /// </summary>
    [Serializable]
    public sealed class TroopReference : EquatableAtomReference<
        Troop,
        TroopPair,
        TroopConstant,
        TroopVariable,
        TroopEvent,
        TroopPairEvent,
        TroopTroopFunction,
        TroopVariableInstancer>, IEquatable<TroopReference>
    {
        public TroopReference() : base() { }
        public TroopReference(Troop value) : base(value) { }
        public bool Equals(TroopReference other) { return base.Equals(other); }
    }
}
