using UnityEngine;
using Unity.Netcode;
using TMPro;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [SerializeField] Button hostButton;
    [SerializeField] Button joinButton;
    [SerializeField] Button quit;
    private void Start()
    {
        joinButton.onClick.AddListener(Join);
        hostButton.onClick.AddListener(Host);
        quit.onClick.AddListener(Application.Quit);
    }

    private void Join()
    {
        // El cliente únicamente inicia la conexión.
        // Netcode cambiará al cliente de escena automáticamente cuando el Host esté listo.
        NetworkManager.Singleton.StartClient();
    }

    private void Host()
    {
        NetworkManager.Singleton.StartHost();

        // Cargamos la escena de juego mediante el SceneManager de Netcode para sincronizar a todos los clientes
        NetworkManager.Singleton.SceneManager.LoadScene("SampleScene", UnityEngine.SceneManagement.LoadSceneMode.Single);
    }
}
