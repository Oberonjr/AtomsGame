/// <summary>
/// Interface for combat coordination (Unity and Atoms)
/// </summary>
public interface ICombatManager
{
    void Initialize();
    void UpdateCombat();
    void Cleanup();

    ITroop FindNearestEnemy(ITroop troop);
    void RegisterTroop(ITroop troop);
    void UnregisterTroop(ITroop troop);
}