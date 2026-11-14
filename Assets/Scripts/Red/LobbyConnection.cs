using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Mono.Cecil.Cil;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEditor.PlayerSettings;
using static UnityEngine.EventSystems.EventTrigger;

public class LobbyConnection : MonoBehaviour
{
    private const string ClaveRelay = "RelayJoinCode";
    private const string ClaveReady = "IsReady";

    private const float SegundosEntreHeartbeats = 15f;

    private Lobby salaActual;
    private ILobbyEvents eventosSala;
    private bool serviciosListos = false;
    private bool conectando = false;

    public Lobby SalaActual => salaActual;

    public bool EsHost => salaActual != null && salaActual.HostId == AuthenticationService.Instance.PlayerId;

    public string JoinCode => salaActual?.LobbyCode ?? "";

    public event Action SalaActualizada;
    public event Action PartidaIniciada;

    public event Action SalaCerrada;

    public async Task InicializarServicios()
    {
        if (serviciosListos) return;
        if (UnityServices.State != ServicesInitializationState.Initialized)
{
            await UnityServices.InitializeAsync();
        }
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
        serviciosListos = true;
        Debug.Log("[Lobby] Servicios inicializados. PlayerId: " +
        AuthenticationService.Instance.PlayerId);
    }

    public async Task<string> CrearSala(int maxJugadores = 4)
    {
        try
        {
            await InicializarServicios();
            salaActual = await LobbyService.Instance.CreateLobbyAsync(
            lobbyName: "Sala de " + AuthenticationService.Instance.PlayerId[..8],
            maxPlayers: maxJugadores,
            options: new CreateLobbyOptions
            {
                IsPrivate = true, 
                Player = CrearJugadorLocal()
            });
            await AlEntrarASala();
            Debug.Log("[Lobby] Sala creada. Código: " + salaActual.LobbyCode);
            return salaActual.LobbyCode;
        }

        catch (Exception e)
        {
            Debug.LogError("[Lobby] No se pudo crear la sala: " + e.Message);
            return null;
        }
    }
    public async Task<bool> UnirseConCodigo(string codigo)
    {
        if (string.IsNullOrWhiteSpace(codigo))
        {
            Debug.LogError("[Lobby] El código está vacío");
            return false;
        }
        try
        {
            await InicializarServicios();
            salaActual = await LobbyService.Instance.JoinLobbyByCodeAsync(
            lobbyCode: codigo.Trim().ToUpper(),
            options: new JoinLobbyByCodeOptions
            {
                Player = CrearJugadorLocal()
            });
            await AlEntrarASala();
            Debug.Log("[Lobby] Unido a la sala: " + salaActual.Name);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError("[Lobby] No se pudo unir a la sala: " + e.Message);
            return false;
        }
    }

    private Player CrearJugadorLocal()
    {
        return new Player(
        id: AuthenticationService.Instance.PlayerId,
        data: new Dictionary<string, PlayerDataObject>
        {
            [ClaveReady] = new PlayerDataObject(
            visibility: PlayerDataObject.VisibilityOptions.Member,
            value: "false")
        });
    }

    private async Task AlEntrarASala()
    {
        
        var callbacks = new LobbyEventCallbacks();
        callbacks.LobbyChanged += AlCambiarSala;
        callbacks.KickedFromLobby += AlPerderSala;
        eventosSala = await LobbyService.Instance.SubscribeToLobbyEventsAsync(salaActual.Id, callbacks);
        StartCoroutine(MantenerSalaViva());
    }

    private void AlCambiarSala(ILobbyChanges cambios)
    {
        if (salaActual == null) return;
        if (cambios.LobbyDeleted)
        {
            AlPerderSala();
            return;
        }

        cambios.ApplyToLobby(salaActual);
        SalaActualizada?.Invoke();

        if (!EsHost & salaActual.Data != null & salaActual.Data.ContainsKey(ClaveRelay))
{
            _ = ConectarComoCliente();
        }
    }

    private void AlPerderSala()
    {
        salaActual = null;
        eventosSala = null;
        Debug.Log("[Lobby] La sala se cerró");
        SalaCerrada?.Invoke();
    }

    private IEnumerator MantenerSalaViva()
    {
        var espera = new WaitForSecondsRealtime(SegundosEntreHeartbeats);
        while (salaActual != null)
{
            if (EsHost)

            {
                _ = EnviarHeartbeat(salaActual.Id);
            }
            yield return espera;
        }
    }

    private async Task EnviarHeartbeat(string idSala)
    {
        try
        {
            await LobbyService.Instance.SendHeartbeatPingAsync(idSala);
        }
        catch (Exception e)
        {
            Debug.LogWarning("[Lobby] Falló el heartbeat: " + e.Message);
        }
    }

    public async Task<bool> MarcarReady(bool ready)
    {
        if (salaActual == null)
        {
            Debug.LogError("[Lobby] No hay sala activa para marcar ready");
            return false;
        }
        try
        {
            var opciones = new UpdatePlayerOptions
            {
                Data = new Dictionary<string, PlayerDataObject>
                {
                    [ClaveReady] = new PlayerDataObject(
                        visibility: PlayerDataObject.VisibilityOptions.Member,
                        value: ready ? "true" : "false")
                }
            };
            salaActual = await LobbyService.Instance.UpdatePlayerAsync(salaActual.Id,AuthenticationService.Instance.PlayerId,opciones);
            Debug.Log("[Lobby] Estado ready actualizado: " + ready);
            SalaActualizada?.Invoke();
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError("[Lobby] Error al marcar ready: " + e.Message);

            return false;
        }
    }

    public static bool EstaListo(Player jugador)
    {
        return jugador.Data != null && jugador.Data.TryGetValue(ClaveReady, out var dato) && dato.Value == "true";
    }

    public bool TodosListos()
    {
        if (salaActual == null) return false;
        foreach (var jugador in salaActual.Players)
        {
            if (!EstaListo(jugador)) return false;
        }
        return true;
    }

    public async Task<bool> IniciarPartida()
    {
        if (!EsHost)
        {
            Debug.LogError("[Lobby] Solo el host puede iniciar la partida");
            return false;
        }
        if (conectando | NetworkManager.Singleton.IsListening) return false;
        conectando = true;
        try
        {

            Allocation alloc = await RelayService.Instance.CreateAllocationAsync(salaActual.MaxPlayers - 1);

            string codigoRelay = await RelayService.Instance.GetJoinCodeAsync(alloc.AllocationId);

            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            //transport.SetRelayServerData(alloc.ToRelayServerData("dtls"));
            if (!NetworkManager.Singleton.StartHost())
            {
                Debug.LogError("[Lobby] StartHost() falló");
                return false;
            }

            salaActual = await LobbyService.Instance.UpdateLobbyAsync(salaActual.Id,
            new UpdateLobbyOptions
            {
                IsLocked = true, 
                Data = new Dictionary<string, DataObject>
                {
                    [ClaveRelay] = new DataObject(visibility: DataObject.VisibilityOptions.Member,value: codigoRelay)
                }
            });

            Debug.Log("[Lobby] Partida iniciada como Host a través de Relay");
            //PartidaIniciada? Invoke();
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError("[Lobby] Error al iniciar la partida: " + e.Message);
            return false;
        }
        finally
        {
            conectando = false;
        }
    }

    public async Task<bool> ConectarComoCliente()
    {
        if (salaActual == null | EsHost) return false;
        if (conectando | NetworkManager.Singleton.IsListening) return false;
        if (salaActual.Data == null |!salaActual.Data.TryGetValue(ClaveRelay, out var dato) |string.IsNullOrEmpty(dato.Value))
        {
            Debug.LogError("[Lobby] El host todavía no publicó el join code de Relay");
            return false;
        }

        conectando = true;
        try
        {

            JoinAllocation join = await RelayService.Instance.JoinAllocationAsync(dato.Value);

            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            //transport.SetRelayServerData(join.ToRelayServerData("dtls"));

            if (!NetworkManager.Singleton.StartClient())
                {
                    Debug.LogError("[Lobby] StartClient() falló");
                    return false;
            }
            Debug.Log("[Lobby] Conectado como Cliente a través de Relay");
            PartidaIniciada?.Invoke();
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError("[Lobby] Error al conectar como cliente: " + e.Message);
            return false;
        }
        finally
        {
            conectando = false;
        }
    }

    public async Task<bool> DejarSala()
    {
        if (salaActual == null) return true;
        try
        {
            string idSala = salaActual.Id;
            await DejarDeEscuchar();
            await LobbyService.Instance.RemovePlayerAsync(idSala, AuthenticationService.Instance.PlayerId);
            Debug.Log("[Lobby] Jugador dejó la sala");
            salaActual = null;
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError("[Lobby] Error al dejar la sala: " + e.Message);
            return false;
        }
    }

    public async Task<bool> BorrarSala()
    {
        if (!EsHost)
        {
            Debug.LogError("[Lobby] Solo el host puede borrar la sala");
            return false;
        }
        try
        {
            string idSala = salaActual.Id;
            await DejarDeEscuchar();
            await LobbyService.Instance.DeleteLobbyAsync(idSala);
            Debug.Log("[Lobby] Sala borrada");
            salaActual = null;
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError("[Lobby] Error al borrar la sala: " + e.Message);
            return false;
        }
    }

    private async Task DejarDeEscuchar()
    {
        if (eventosSala == null) return;
        var eventos = eventosSala;
        eventosSala = null;
        await eventos.UnsubscribeAsync();
    }

    private void OnDestroy()
    {
        if (salaActual != null)
        {
           _ = LobbyService.Instance.RemovePlayerAsync(
           salaActual.Id,
           AuthenticationService.Instance.PlayerId);
           salaActual = null;

        }
    }
}
