using System;
using UnityAtoms.BaseAtoms;

namespace UnityAtoms
{
    /// <summary>
    /// Reference of type `TroopStats_Atoms`. Inherits from `EquatableAtomReference&lt;TroopStats_Atoms, TroopStats_AtomsPair, TroopStats_AtomsConstant, TroopStats_AtomsVariable, TroopStats_AtomsEvent, TroopStats_AtomsPairEvent, TroopStats_AtomsTroopStats_AtomsFunction, TroopStats_AtomsVariableInstancer, AtomCollection, AtomList&gt;`.
    /// </summary>
    [Serializable]
    public sealed class TroopStats_AtomsReference : EquatableAtomReference<
        TroopStats_Atoms,
        TroopStats_AtomsPair,
        TroopStats_AtomsConstant,
        TroopStats_AtomsVariable,
        TroopStats_AtomsEvent,
        TroopStats_AtomsPairEvent,
        TroopStats_AtomsTroopStats_AtomsFunction,
        TroopStats_AtomsVariableInstancer>, IEquatable<TroopStats_AtomsReference>
    {
        public TroopStats_AtomsReference() : base() { }
        public TroopStats_AtomsReference(TroopStats_Atoms value) : base(value) { }
        public bool Equals(TroopStats_AtomsReference other) { return base.Equals(other); }
    }
}
