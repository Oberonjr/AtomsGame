using System;
using UnityEngine;
namespace UnityAtoms
{
    /// <summary>
    /// IPair of type `&lt;Troop&gt;`. Inherits from `IPair&lt;Troop&gt;`.
    /// </summary>
    [Serializable]
    public struct TroopPair : IPair<Troop>
    {
        public Troop Item1 { get => _item1; set => _item1 = value; }
        public Troop Item2 { get => _item2; set => _item2 = value; }

        [SerializeField]
        private Troop _item1;
        [SerializeField]
        private Troop _item2;

        public void Deconstruct(out Troop item1, out Troop item2) { item1 = Item1; item2 = Item2; }
    }
}