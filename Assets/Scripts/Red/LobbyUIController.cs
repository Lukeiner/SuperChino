using System;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static UnityEditor.PlayerSettings;
using static UnityEngine.EventSystems.EventTrigger;

public class LobbyUIController : MonoBehaviour
{
    [Header("Conexión")]
    [SerializeField] private LobbyConnection lobby;
    [Header("Menú principal")]
    [SerializeField] private GameObject menuLobby;
    [SerializeField] private TMP_InputField inputCodigo;
    [SerializeField] private TextMeshProUGUI textoEstado;
    [Header("Panel de sala")]
    [SerializeField] private GameObject salaPanel;
    [SerializeField] private TextMeshProUGUI textoJoinCode;
    [SerializeField] private Transform listaJugadores; 
    [SerializeField] private GameObject itemJugadorPrefab;
    [SerializeField] private TextMeshProUGUI textoBotonReady;
    [SerializeField] private Button botonIniciar;

    private bool estoyListo = false;

    private void Start()
    {
        if (lobby = null)
        {
            lobby = FindAnyObjectByType<LobbyConnection>();
        }


        lobby.SalaActualizada += RefrescarSala;
        lobby.PartidaIniciada += AlIniciarPartida;
        lobby.SalaCerrada += AlCerrarseLaSala;
        MostrarMenu("Creá una sala o entrá con un código");
    }

    private void OnDestroy()
    {
        if (lobby = null) return;
        lobby.SalaActualizada -= RefrescarSala;
        lobby.PartidaIniciada -= AlIniciarPartida;
        lobby.SalaCerrada -= AlCerrarseLaSala;
    }

    public async void OnCrearSala()
    {
        textoEstado.text = "Creando sala. ";
        string codigo = await lobby.CrearSala(4);
        if (codigo == null)
        {
            textoEstado.text = "No se pudo crear la sala. Mirá la consola.";
            return;
        }

        MostrarSala();
    }

    public async void OnUnirseSala()
    {
        textoEstado.text = "Conectando. ";
        bool ok = await lobby.UnirseConCodigo(inputCodigo.text);
        if (!ok)
        {
            textoEstado.text = "No se pudo unir. ¿El código está bien?";
            return;
        }

        MostrarSala();

    }

    private void MostrarMenu(string estado)
    {
        menuLobby.SetActive(true);
        salaPanel.SetActive(false);
        textoEstado.text = estado;

        estoyListo = false;
    }

    private void MostrarSala()
    {
        menuLobby.SetActive(false);
        salaPanel.SetActive(true);
        textoJoinCode.text = "Código: " + lobby.JoinCode;
        textoBotonReady.text = "Marcar listo";
        textoEstado.text = "Esperando a los demás. ";
        RefrescarSala();
    }

    private void RefrescarSala()
    {
        if (lobby.SalaActual == null) return;

        botonIniciar.gameObject.SetActive(lobby.EsHost);

        foreach (Transform hijo in listaJugadores)
        {
            Destroy(hijo.gameObject);
        }

        foreach (var jugador in lobby.SalaActual.Players)
        {
            GameObject item = Instantiate(itemJugadorPrefab, listaJugadores);

            string nombre = jugador.Id[..Math.Min(8, jugador.Id.Length)];

            if (jugador.Id == lobby.SalaActual.HostId)
                nombre += " (Host)";
            nombre += LobbyConnection.EstaListo(jugador) ? " ?" : " —";
            item.GetComponentInChildren<TextMeshProUGUI>().text = nombre;
        }
    }

    public async void ToggleReady()
    {
        bool ok = await lobby.MarcarReady(!estoyListo);
        if (!ok) return;

        estoyListo = !estoyListo;
        textoBotonReady.text = estoyListo ? "Listo ?" : "Marcar listo";
    }

    public async void OnIniciarPartida()
    {
        if (!lobby.EsHost) return;
        if (!lobby.TodosListos())
        {
            textoEstado.text = "Todavía falta que alguien marque listo";
            return;
        }
        textoEstado.text = "Iniciando partida. ";
        bool ok = await lobby.IniciarPartida();
        if (!ok) textoEstado.text = "Error al iniciar. Mirá la consola.";
    }

    private void AlIniciarPartida()
    {
        menuLobby.SetActive(false);
        salaPanel.SetActive(false);
        textoEstado.text = "";
    }

    public async void OnDejarSala()
    {
        await lobby.DejarSala();
        MostrarMenu("Saliste de la sala");
    }

    private void AlCerrarseLaSala()
    {
        MostrarMenu("La sala se cerró");
    }
}
