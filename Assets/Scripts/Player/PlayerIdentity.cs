using Unity.Netcode;
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
public class PlayerIdentity : NetworkBehaviour
{
    [Header("Identidad del Jugador")]
    [SerializeField] private Faction faction = Faction.Empleado;
    [SerializeField] private RoleType role = RoleType.Repositor;

    public NetworkVariable<Faction> netFaction = new NetworkVariable<Faction>(
        Faction.Empleado, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server
    );

    public NetworkVariable<RoleType> netRole = new NetworkVariable<RoleType>(
        RoleType.Repositor, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server
    );

    public Faction CurrentFaction => netFaction.Value;
    public RoleType CurrentRole => netRole.Value;

    public void AssignRole(Faction newFaction, RoleType newRole)
    {
        faction = newFaction;
        role = newRole;

        Debug.Log($"[PLAYER SETUP] Faccion: {faction} | Rol asignado: {role}");
    }

    public void SetIdentityOnServer(Faction newFaction, RoleType newRole)
    {
        if (!IsServer) return;
        netFaction.Value = newFaction;
        netRole.Value = newRole;
        Debug.Log($"[IDENTIDAD RED] Cliente {OwnerClientId} -> {newFaction} ({newRole})");
    }

    public bool IsLadron()
    {
        return netFaction.Value == Faction.Ladron;
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void SetIdentityServerRpc(Faction newFaction, RoleType newRole)
    {
        // Actualiza la variable o sincroniza el rol por red
        AssignRole(newFaction, newRole);
    }
}
