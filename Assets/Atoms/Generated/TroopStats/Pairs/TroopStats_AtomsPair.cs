using System;
using UnityEngine;
namespace UnityAtoms
{
    /// <summary>
    /// IPair of type `&lt;TroopStats_Atoms&gt;`. Inherits from `IPair&lt;TroopStats_Atoms&gt;`.
    /// </summary>
    [Serializable]
    public struct TroopStats_AtomsPair : IPair<TroopStats_Atoms>
    {
        public TroopStats_Atoms Item1 { get => _item1; set => _item1 = value; }
        public TroopStats_Atoms Item2 { get => _item2; set => _item2 = value; }

        [SerializeField]
        private TroopStats_Atoms _item1;
        [SerializeField]
        private TroopStats_Atoms _item2;

        public void Deconstruct(out TroopStats_Atoms item1, out TroopStats_Atoms item2) { item1 = Item1; item2 = Item2; }
    }
}