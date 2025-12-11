using System;
using UnityAtoms.BaseAtoms;

namespace UnityAtoms
{
    /// <summary>
    /// Reference of type `Team`. Inherits from `EquatableAtomReference&lt;Team, TeamPair, TeamConstant, TeamVariable, TeamEvent, TeamPairEvent, TeamTeamFunction, TeamVariableInstancer, AtomCollection, AtomList&gt;`.
    /// </summary>
    [Serializable]
    public sealed class TeamReference : EquatableAtomReference<
        Team,
        TeamPair,
        TeamConstant,
        TeamVariable,
        TeamEvent,
        TeamPairEvent,
        TeamTeamFunction,
        TeamVariableInstancer>, IEquatable<TeamReference>
    {
        public TeamReference() : base() { }
        public TeamReference(Team value) : base(value) { }
        public bool Equals(TeamReference other) { return base.Equals(other); }
    }
}
