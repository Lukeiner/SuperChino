using UnityEngine;
public enum Faction
{
    Empleado,
    Ladron
}

public enum RoleType
{
    Limpieza,
    Repositor,
    Cajero
}
public class PlayerIdentity : MonoBehaviour
{
    [Header("Identidad del Jugador")]
    [SerializeField] private Faction faction = Faction.Empleado;
    [SerializeField] private RoleType role = RoleType.Repositor;

    public Faction CurrentFaction => faction;
    public RoleType CurrentRole => role;

    public void AssignRole(Faction newFaction, RoleType newRole)
    {
        faction = newFaction;
        role = newRole;

        Debug.Log($"[PLAYER SETUP] Faccion: {faction} | Rol asignado: {role}");
    }

    public bool IsLadron()
    {
        return faction == Faction.Ladron;
    }
}
