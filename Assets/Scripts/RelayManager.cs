using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

public class RelayManager : MonoBehaviour
{
    public static RelayManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private async void Start()
    {
        // 1. Inicializar los servicios de Unity y autenticar de forma anónima
        await UnityServices.InitializeAsync();

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            Debug.Log($"[RELAY]: Autenticado anónimamente como ID: {AuthenticationService.Instance.PlayerId}");
        }
    }

    public async Task<string> CreateRelay(int maxPlayers = 4)
    {
        try
        {
            
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxPlayers);

            
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            Debug.Log($"[RELAY]: Sala creada con éxito. Join Code: {joinCode}");

            
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetHostRelayData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData
            );

            
            NetworkManager.Singleton.StartHost();

            return joinCode;
        }
        catch (RelayServiceException e)
        {
            Debug.LogError($"[RELAY ERROR]: No se pudo crear la sala: {e.Message}");
            return null;
        }
    }

    // Método que llamará el botón "Unirse / Client" pasando el código ingresado en el InputField
    public async Task<bool> JoinRelay(string joinCode)
    {
        try
        {
            Debug.Log($"[RELAY]: Intentando unirse a la sala con código: {joinCode}");

            // Validar e ingresar a la asignación con el código
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

            // Configurar el Unity Transport para usar los datos de la sala cliente
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetClientRelayData(
                joinAllocation.RelayServer.IpV4,
                (ushort)joinAllocation.RelayServer.Port,
                joinAllocation.AllocationIdBytes,
                joinAllocation.Key,
                joinAllocation.ConnectionData,
                joinAllocation.HostConnectionData
            );

            // Iniciar el Cliente en Netcode
            NetworkManager.Singleton.StartClient();

            return true;
        }
        catch (RelayServiceException e)
        {
            Debug.LogError($"[RELAY ERROR]: No se pudo unir a la sala: {e.Message}");
            return false;
        }
    }

}
