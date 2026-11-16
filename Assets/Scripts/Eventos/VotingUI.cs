using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class VotingUI : MonoBehaviour
{
    public static VotingUI Instance { get; private set; }

    [Header("Paneles")]
    [SerializeField] private GameObject votingPanel;        // Panel con la lista de sospechosos (respaldo si no esta el tablero)
    [SerializeField] private GameObject eliminatedPanel;    // Pantalla "Fuiste expulsado - Game Over"

    [Header("Lista Dinámica de Botones")]
    [SerializeField] private Transform buttonsContainer;  // Un objeto con VerticalLayoutGroup
    [SerializeField] private Button playerButtonPrefab;

    [Header("Tablero de corcho (diseño de Leo)")]
    [SerializeField] private bool usarTablero = true;       // si se apaga, vuelve la lista de botones original

    private bool isEliminated = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (votingPanel != null) votingPanel.SetActive(false);
        if (eliminatedPanel != null) eliminatedPanel.SetActive(false);
    }

    private static List<ulong> Vivos()
    {
        var lista = new List<ulong>();
        if (VotingManager.Instance != null)
            foreach (ulong id in VotingManager.Instance.alivePlayers) lista.Add(id);
        return lista;
    }

    public void OpenVotingPanel()
    {
        // Si este jugador ya fue expulsado en una ronda anterior, no puede votar
        if (isEliminated) return;

        ulong myId = NetworkManager.Singleton.LocalClientId;

        // Tablero de corcho: una foto por jugador vivo
        TableroVotacionUI tablero = usarTablero ? TableroVotacionUI.Obtener() : null;
        if (tablero != null)
        {
            tablero.AbrirVotacion(Vivos(), myId, OnVoteButtonClicked);
            return;
        }

        if (votingPanel == null) return;

        // Limpiamos botones de rondas anteriores
        foreach (Transform child in buttonsContainer)
        {
            Destroy(child.gameObject);
        }

        // Creamos un botón por cada jugador que sigue vivo en la NetworkList
        foreach (ulong targetId in VotingManager.Instance.alivePlayers)
        {
            Button btn = Instantiate(playerButtonPrefab, buttonsContainer);
            TMP_Text btnText = btn.GetComponentInChildren<TMP_Text>();
            if (btnText != null)
            {
                btnText.text = (targetId == myId) ? $"Jugador {targetId} (Vos)" : $"Jugador {targetId}";
            }

            ulong capturedTargetId = targetId;
            btn.onClick.AddListener(() => OnVoteButtonClicked(capturedTargetId));
        }

        votingPanel.SetActive(true);
    }

    private void OnVoteButtonClicked(ulong targetId)
    {
        ulong myId = NetworkManager.Singleton.LocalClientId;
        VotingManager.Instance.CastVoteServerRpc(myId, targetId);

        // Bloqueamos los botones para que vea que su voto ya fue emitido
        if (buttonsContainer != null)
        {
            foreach (Transform child in buttonsContainer)
            {
                Button btn = child.GetComponent<Button>();
                if (btn != null) btn.interactable = false;
            }
        }
    }

    public void CloseVotingPanel()
    {
        if (votingPanel != null) votingPanel.SetActive(false);
        if (TableroVotacionUI.Instance != null) TableroVotacionUI.Instance.CerrarVotacion();
    }

    /// <summary>Lo llama VotingManager en todos: muestra quien fue despedido (o que nadie).</summary>
    public void ShowResult(TableroVotacionUI.Resultado tipo, ulong eliminatedId)
    {
        TableroVotacionUI tablero = usarTablero ? TableroVotacionUI.Obtener() : null;
        if (tablero != null) tablero.MostrarResultado(tipo, eliminatedId, Vivos());
    }

    public void ShowEliminatedGameOver()
    {
        isEliminated = true;
        CloseVotingPanel();
        if (eliminatedPanel != null) eliminatedPanel.SetActive(true);
    }

}
