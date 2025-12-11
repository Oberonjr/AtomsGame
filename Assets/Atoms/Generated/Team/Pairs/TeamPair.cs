using System;
using UnityEngine;
namespace UnityAtoms
{
    /// <summary>
    /// IPair of type `&lt;Team&gt;`. Inherits from `IPair&lt;Team&gt;`.
    /// </summary>
    [Serializable]
    public struct TeamPair : IPair<Team>
    {
        public Team Item1 { get => _item1; set => _item1 = value; }
        public Team Item2 { get => _item2; set => _item2 = value; }

        [SerializeField]
        private Team _item1;
        [SerializeField]
        private Team _item2;

        public void Deconstruct(out Team item1, out Team item2) { item1 = Item1; item2 = Item2; }
    }
}