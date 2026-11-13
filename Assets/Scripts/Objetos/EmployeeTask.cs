using UnityEngine;

public class EmployeeTask : BaseInteractable
{
    [Header("Reglas de Empleado")]
    [SerializeField] private RoleType requiredRole = RoleType.Repositor;

    public override bool CanPlayerInteract(PlayerIdentity playerIdentity)
    {
        if (playerIdentity == null || isCompleted.Value) return false;

        // Validación exclusiva de empleados
        return playerIdentity.CurrentRole == requiredRole;
    }
}
