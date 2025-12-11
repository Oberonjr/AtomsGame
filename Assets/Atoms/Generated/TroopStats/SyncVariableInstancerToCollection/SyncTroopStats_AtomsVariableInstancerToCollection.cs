using UnityEngine;
using UnityAtoms.BaseAtoms;

namespace UnityAtoms
{
    /// <summary>
    /// Adds Variable Instancer's Variable of type TroopStats_Atoms to a Collection or List on OnEnable and removes it on OnDestroy. 
    /// </summary>
    [AddComponentMenu("Unity Atoms/Sync Variable Instancer to Collection/Sync TroopStats_Atoms Variable Instancer to Collection")]
    [EditorIcon("atom-icon-delicate")]
    public class SyncTroopStats_AtomsVariableInstancerToCollection : SyncVariableInstancerToCollection<TroopStats_Atoms, TroopStats_AtomsVariable, TroopStats_AtomsVariableInstancer> { }
}
