using System;
using System.Threading.Tasks;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
public class MainMenu : MonoBehaviour
{
    //[SerializeField] private Button hostButton;
    //[SerializeField] private Button joinButton;
    [SerializeField] private Button quit;
    [SerializeField] private TMP_Text codeDisplayText;


    [SerializeField] private TMP_InputField joinCodeInput;
    private void Start()
    {
        //joinButton.onClick.AddListener(Join);
        //hostButton.onClick.AddListener(Host);
        quit.onClick.AddListener(Application.Quit);
    }

    //private void Join()
    //{
    // El cliente únicamente inicia la conexión.
    // Netcode cambiará al cliente de escena automáticamente cuando el Host esté listo.
    // NetworkManager.Singleton.StartClient();
    //}

    //private void Host()
    // {
    //NetworkManager.Singleton.StartHost();

    // Cargamos la escena de juego mediante el SceneManager de Netcode para sincronizar a todos los clientes
    //NetworkManager.Singleton.SceneManager.LoadScene("SampleScene", UnityEngine.SceneManagement.LoadSceneMode.Single);
    //}


    public async void OnCreateHostClicked()
    {
        if (codeDisplayText != null)
            codeDisplayText.text = "Generando código...";

        // 1. Llama al método asíncrono de RelayManager
        string joinCode = await RelayManager.Instance.CreateRelay(4);

        if (!string.IsNullOrEmpty(joinCode))
        {
            // 2. Muestra el Join Code en el texto en pantalla
            if (codeDisplayText != null)
            {
                codeDisplayText.text = $"CÓDIGO DE SALA:\n<color=yellow>{joinCode}</color>";
                await Task.Delay(10000);
                NetworkManager.Singleton.SceneManager.LoadScene("SampleScene", LoadSceneMode.Single);
            }
        }
        else
        {
            if (codeDisplayText != null)
                codeDisplayText.text = "Error al crear la sala.";
        }
    }

    public async void OnJoinClientClicked()
    {
        // 1. Validar que la referencia no esté nula y que se haya escrito algo
        if (joinCodeInput == null)
        {
            Debug.LogError("[UI]: Falta asignar el Join Code InputField en el Inspector de Unity.");
            return;
        }

        if (string.IsNullOrEmpty(joinCodeInput.text))
        {
            Debug.LogWarning("[UI]: Por favor, ingresá un código válido en la casilla.");
            return;
        }

        string codeToJoin = joinCodeInput.text.Trim();

        // 2. Intentar unirse mediante el RelayManager
        bool success = await RelayManager.Instance.JoinRelay(codeToJoin);

        if (success)
        {
            Debug.Log("[UI]: Unidox con éxito a la sala.");
        }
        else
        {
            Debug.LogError("[UI]: El código ingresado es incorrecto o la sala no existe.");
        }
    }
}
