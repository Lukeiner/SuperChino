using Unity.Netcode;
using UnityEditor.PackageManager;
using UnityEngine;


public class ObjectInteraction : NetworkBehaviour
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

    [Header("Visual Feedback (Feedback de Color)")]
    [SerializeField] private SpriteRenderer objectSpriteRenderer;
    [SerializeField] private Color successColor = Color.green;
    [SerializeField] private Color failColor = Color.red;

    public NetworkVariable<bool> isCompleted = new NetworkVariable<bool>(
    false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public string ObjectName => objectName;
    public float InteractionTime => interactionTime;

    private void Awake()
    {
        // Si no se asignó manualmente en el Inspector, busca el SpriteRenderer del objeto
        if (objectSpriteRenderer == null)
            objectSpriteRenderer = GetComponent<SpriteRenderer>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Escuchamos los cambios de la NetworkVariable para sincronizar el color verde en todos los clientes
        isCompleted.OnValueChanged += OnCompletedStateChanged;

        // Si al hacer Spawn la tarea ya estaba completada, aplicamos el verde
        if (isCompleted.Value && objectSpriteRenderer != null)
        {
            objectSpriteRenderer.color = successColor;
        }
    }
    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        isCompleted.OnValueChanged -= OnCompletedStateChanged;
    }

    private void OnCompletedStateChanged(bool previousValue, bool newValue)
    {
        if (newValue && objectSpriteRenderer != null)
        {
            objectSpriteRenderer.color = successColor;
        }
    }

    public bool CanPlayerInteract(PlayerIdentity playerIdentity)
    {
        if (playerIdentity == null || isCompleted.Value) return false;

        if (reqIsLadron)
        {
            return playerIdentity.IsLadron();
        }
        return playerIdentity.CurrentRole == requiredRole;
    }
    public string GetDialog(bool canInteract)
    {
        if (isCompleted.Value) return "Esta tarea ya fue completada por otro jugador.";
        return canInteract ? successDialog : failDialog;
    }


    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void RequestInteractRpc(ulong clientId)
    {
        if (!NetworkObject.IsSpawned || !NetworkManager.IsServer) return;

        // Si ya está completada, ignoramos cualquier otra interacción
        if (isCompleted.Value) return;

        // Obtenemos el PlayerObject del cliente que envió la petición
        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
        {
            var playerIdentity = client.PlayerObject.GetComponent<PlayerIdentity>();

            // VALIDACIÓN AUTORITATIVA EN EL SERVIDOR
            bool hasValidRole = CanPlayerInteract(playerIdentity);

            if (hasValidRole)
            {
                // Solo si el servidor confirma que coincide el rol, se completa
                isCompleted.Value = true;
                SetColorRpc(successColor);
            }
            else
            {
                // Si no coincide el rol en el servidor, tiñe de rojo la estación
                SetColorRpc(failColor);
            }
        }

        //NotifyInteractionRpc(objectName, playerId);
    }

    [Rpc(SendTo.Everyone)]

    public void SetColorRpc(Color targetColor)
    {
        if (objectSpriteRenderer != null)
        {
            objectSpriteRenderer.color = targetColor;
        }
    }
    private void NotifyInteractionRpc(string taskName, ulong playerId)
    {
        Debug.Log($"<color=cyan>[RED]:</color> El jugador {playerId} completó la tarea en '{taskName}'.");
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

