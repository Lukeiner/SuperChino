using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class VotingUI : MonoBehaviour
{
    public static VotingUI Instance { get; private set; }

    [Header("Paneles")]
    [SerializeField] private GameObject votingPanel;        // Panel con la lista de sospechosos
    [SerializeField] private GameObject eliminatedPanel;    // Pantalla "Fuiste expulsado - Game Over"

    [Header("Lista Dinámica de Botones")]
    [SerializeField] private Transform buttonsContainer;  // Un objeto con VerticalLayoutGroup
    [SerializeField] private Button playerButtonPrefab;

    private bool isEliminated = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (votingPanel != null) votingPanel.SetActive(false);
        if (eliminatedPanel != null) eliminatedPanel.SetActive(false);
    }

    public void OpenVotingPanel()
    {
        // Si este jugador ya fue expulsado en una ronda anterior, no puede votar
        if (isEliminated || votingPanel == null) return;

        // Limpiamos botones de rondas anteriores
        foreach (Transform child in buttonsContainer)
        {
            Destroy(child.gameObject);
        }

        ulong myId = NetworkManager.Singleton.LocalClientId;

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
        foreach (Transform child in buttonsContainer)
        {
            Button btn = child.GetComponent<Button>();
            if (btn != null) btn.interactable = false;
        }
    }

    public void CloseVotingPanel()
    {
        if (votingPanel != null) votingPanel.SetActive(false);
    }
    public void ShowEliminatedGameOver()
    {
        isEliminated = true;
        CloseVotingPanel();
        if (eliminatedPanel != null) eliminatedPanel.SetActive(true);
    }

}
