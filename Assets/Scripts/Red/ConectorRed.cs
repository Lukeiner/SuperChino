using UnityEngine;
using Unity.Netcode;

public class ConectorRed : MonoBehaviour
{
    private NetworkManager networkManager;

    void Awake()
    {
        networkManager = NetworkManager.Singleton;
    }

    void Update()
    {
   
        if (Input.GetKeyDown(KeyCode.H))
        {
            IniciarComoHost();
        }

        if (Input.GetKeyDown(KeyCode.C))
        {
            ConectarComoCliente();
        }

        if (Input.GetKeyDown(KeyCode.Q))
        {
            Desconectar();
        }
    }

    void IniciarComoHost()
    {
        if (networkManager.IsHost || networkManager.IsClient) return;
        Debug.Log("Iniciando como HOST. ");
        networkManager.StartHost();
    }

    void ConectarComoCliente()
    {
        if (networkManager.IsClient) return;
        Debug.Log("Conectando como CLIENTE. ");
        networkManager.StartClient();
    }

    void Desconectar()
    {
        if (networkManager.IsListening || networkManager.IsClient)
        {
            Debug.Log("Desconectando. ");
            networkManager.Shutdown();
        }
    }

}
