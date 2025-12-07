/// <summary>
/// Interface for health display (Unity and Atoms)
/// </summary>
public interface IHealthBar
{
    void Initialize(ITroop troop);
    void UpdateDisplay(int currentHealth, int maxHealth);
}