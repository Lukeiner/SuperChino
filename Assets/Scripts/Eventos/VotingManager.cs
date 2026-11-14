using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class VotingManager : NetworkBehaviour
{
    public static VotingManager Instance { get; private set; }

    public NetworkList<ulong> alivePlayers;

    private Dictionary<ulong, ulong> currentVotes = new Dictionary<ulong, ulong>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        alivePlayers = new NetworkList<ulong>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer)
        {
            alivePlayers.Clear();
            foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
            {
                alivePlayers.Add(clientId);
            }
        }
    }

    public void StartVotingSession()
    {
        if (!IsServer) return;

        currentVotes.Clear();
        ToggleVotingUIClientRpc(true);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void CastVoteServerRpc(ulong voterClientId, ulong targetClientId)
    {
        if (!IsServer) return;

        // Solo pueden votar y ser votados los que siguen vivos
        if (!alivePlayers.Contains(voterClientId) || !alivePlayers.Contains(targetClientId)) return;

        // Evitamos doble voto en la misma ronda
        if (currentVotes.ContainsKey(voterClientId)) return;

        currentVotes[voterClientId] = targetClientId;
        Debug.Log($"[VOTACIÓN] Jugador {voterClientId} votó a Jugador {targetClientId}");
    }

    public void ResolveVotes()
    {
        if (!IsServer) return;

        ToggleVotingUIClientRpc(false);

        if (currentVotes.Count == 0)
        {
            Debug.Log("[VOTACIÓN] Nadie votó. No hay expulsados.");
            return;
        }

        // 1. Contamos votos por sospechoso
        Dictionary<ulong, int> voteCounts = new Dictionary<ulong, int>();
        foreach (ulong targetId in currentVotes.Values)
        {
            if (!voteCounts.ContainsKey(targetId)) voteCounts[targetId] = 0;
            voteCounts[targetId]++;
        }

        // 2. Buscamos la mayoría (y detectamos si hay empate)
        ulong mostVotedId = 0;
        int maxVotes = 0;
        bool isTie = false;

        foreach (var pair in voteCounts)
        {
            if (pair.Value > maxVotes)
            {
                maxVotes = pair.Value;
                mostVotedId = pair.Key;
                isTie = false;
            }
            else if (pair.Value == maxVotes)
            {
                isTie = true;
            }
        }

        if (isTie)
        {
            Debug.Log("[VOTACIÓN] Hubo empate. Nadie fue expulsado.");
            return;
        }

        // 3. Procesamos al expulsado
        EliminatePlayer(mostVotedId);
    }

    private void EliminatePlayer(ulong eliminatedId)
    {
        alivePlayers.Remove(eliminatedId);

        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(eliminatedId, out var client))
        {
            var identity = client.PlayerObject.GetComponent<PlayerIdentity>();
            bool wasThief = identity != null && identity.IsLadron();

            if (wasThief)
            {
                // ¡Atraparon al Ladrón! Ganan los Empleados
                Debug.Log($"[VOTACIÓN] ¡El jugador {eliminatedId} ERA EL LADRÓN! Ganan los Empleados.");
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.TriggerVoterEliminationServerRpc(eliminatedId, true);
                }
            }
            else
            {
                // Era inocente: le avisamos a todos para ocultarlo y a él le saltará su Game Over
                Debug.Log($"[VOTACIÓN] El jugador {eliminatedId} era inocente y quedó eliminado.");
                HandleInnocentEliminatedClientRpc(eliminatedId);
            }
        }
    }

    [Rpc(SendTo.Everyone)]
    private void ToggleVotingUIClientRpc(bool show)
    {
        if (VotingUI.Instance != null)
        {
            if (show) VotingUI.Instance.OpenVotingPanel();
            else VotingUI.Instance.CloseVotingPanel();
        }
    }

    [Rpc(SendTo.Everyone)]
    private void HandleInnocentEliminatedClientRpc(ulong eliminatedId)
    {
        // Ocultamos el personaje eliminado en las pantallas de todos
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(
            NetworkManager.Singleton.ConnectedClients[eliminatedId].PlayerObject.NetworkObjectId, out var netObj))
        {
            netObj.gameObject.SetActive(false);
        }

        // Si el eliminado soy YO, muestro mi pantalla individual de Game Over
        if (NetworkManager.Singleton.LocalClientId == eliminatedId && VotingUI.Instance != null)
        {
            VotingUI.Instance.ShowEliminatedGameOver();
        }
    }
}
