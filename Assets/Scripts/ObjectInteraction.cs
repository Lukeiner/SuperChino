using UnityEngine;


public class ObjectInteraction : MonoBehaviour
{
    [Header("Configuración del Objeto")]
    [SerializeField] private string objectName = "Góndola de Fideos";

    [Header("Requisitos de Rol")]
    [SerializeField] private bool reqIsLadron = false; 
    [SerializeField] private RoleType requiredRole = RoleType.Repositor;

    [Header("Diálogos Personalizados (Inspector)")]
    [TextArea(2, 4)]
    [SerializeField] private string successDialog = "¡Reposiste la góndola con éxito!"; // Diálogo si el rol coincide

    [TextArea(2, 4)]
    [SerializeField] private string failDialog = "No sabés cómo hacer este trabajo."; // Diálogo si el rol NO coincide

    [Header("Tiempos según High Concept")]
    [SerializeField] private float interactionTime = 4f; 

    public string ObjectName => objectName;
    public float InteractionTime => interactionTime;

    public bool CanPlayerInteract(PlayerIdentity playerIdentity)
    {
        if (playerIdentity == null) return false;

        if (reqIsLadron)
        {
            return playerIdentity.IsLadron();
        }
        return playerIdentity.CurrentRole == requiredRole;
    }
    public string GetDialog(bool canInteract)
    {
        return canInteract ? successDialog : failDialog;
    }
    public void Interact(PlayerIdentity playerIdentity)
    {
        if (reqIsLadron && playerIdentity.IsLadron())
        {
            Debug.Log($"[ÉXITO] ¡El Ladrón robó '{objectName}'!");
        }
        else
        {
            Debug.Log($"[ÉXITO] Tarea realizada en '{objectName}'.");
        }
    }
}

