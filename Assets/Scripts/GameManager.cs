using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Configuración de Partida")]
    [SerializeField] private int totalStolenItemsToWin = 5;

    [Header("Variables de Red (Sincronizadas)")]
    public NetworkVariable<int> currentStolenItems = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server
    );

    public NetworkVariable<bool> isGameOver = new NetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server
    );

    public List<Transform> spawnedSacredObjects = new List<Transform>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Solo el servidor asigna roles y evalúa la partida
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        if (IsServer && NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        // Opcional: Lógica cuando se conecta un cliente en mitad del lobby
    }

    // 1. ASIGNACIÓN ALEATORIA DE ROLES (Solo ejecutado por el Host/Servidor)
    public void AssignRolesToPlayers()
    {
        if (!IsServer) return;

        List<ulong> connectedClients = new List<ulong>(NetworkManager.Singleton.ConnectedClientsIds);
        if (connectedClients.Count == 0) return;

        // Elegimos un Ladrón al azar entre los conectados
        int thiefIndex = Random.Range(0, connectedClients.Count);
        ulong thiefClientId = connectedClients[thiefIndex];

        // Lista de roles de empleado disponibles para repartir
        RoleType[] availableRoles = (RoleType[])System.Enum.GetValues(typeof(RoleType));

        foreach (ulong clientId in connectedClients)
        {
            var clientObject = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject;
            if (clientObject != null)
            {
                var identity = clientObject.GetComponent<PlayerIdentity>();
                if (identity != null)
                {
                    if (clientId == thiefClientId)
                    {
                        // Asignamos facción Ladrón (y un rol secundario para disimular)
                        RoleType fakeRole = availableRoles[Random.Range(0, availableRoles.Length)];
                        identity.SetIdentityServerRpc(Faction.Ladron, fakeRole);
                    }
                    else
                    {
                        // Asignamos facción Empleado y un rol al azar
                        RoleType assignedRole = availableRoles[Random.Range(0, availableRoles.Length)];
                        identity.SetIdentityServerRpc(Faction.Empleado, assignedRole);
                    }
                }
            }
        }

        Debug.Log("[GAME MANAGER] Roles asignados exitosamente a todos los jugadores.");
    }

    public void RegisterSacredObject(Transform sacredObjectTransform)
    {
        if (!IsServer) return;
        if (!spawnedSacredObjects.Contains(sacredObjectTransform))
        {
            spawnedSacredObjects.Add(sacredObjectTransform);
        }
    }

    // 2. REGISTRO DE ROBO (Llamado desde ObjectInteraction cuando el ladrón roba)
    public void NotifyItemStolen()
    {
        if (!IsServer) return;

        currentStolenItems.Value++;
        Debug.Log($"[GAME MANAGER] Ítem robado. Total: {currentStolenItems.Value}/{totalStolenItemsToWin}");

        CheckWinConditions();
    }


    public void CheckWinConditions()
    {
        if (!IsServer || isGameOver.Value) return;

        // Victoria del Ladrón por robos
        if (currentStolenItems.Value >= totalStolenItemsToWin)
        {
            EndGameServerRpc(Faction.Ladron);
            return;
        }

        // TODO: Agregar condición si los empleados completaron todas las tareas del mapa
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void TriggerVoterEliminationServerRpc(ulong eliminatedPlayerId, bool wasThief)
    {
        if (!IsServer || isGameOver.Value) return;

        if (wasThief)
        {
            // Si expulsaron al Ladrón, ganan los Empleados
            Debug.Log("[GAME MANAGER] ¡El Ladrón fue descubierto y expulsado en la votación! Ganan los Empleados.");
            EndGameServerRpc(Faction.Empleado);
        }
        else
        {
            // Si votaron mal y echaron a un inocente, pierde el inocente (o se evalúa si quedan suficientes)
            Debug.Log($"[GAME MANAGER] El jugador {eliminatedPlayerId} fue expulsado pero era inocente.");
        }
    }

    [ServerRpc]
    private void EndGameServerRpc(Faction winningFaction)
    {
        isGameOver.Value = true;
        NotifyGameEndClientRpc(winningFaction);
    }

    [ClientRpc]
    private void NotifyGameEndClientRpc(Faction winningFaction)
    {
        Debug.Log($"<color=gold>[FIN DE LA PARTIDA]: ¡Ha ganado la facción {winningFaction}!</color>");
        // TODO: Activar UI de Victoria/Derrota en pantalla
    }
}
