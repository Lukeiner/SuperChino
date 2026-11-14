using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class RoundManager : NetworkBehaviour
{
    public static RoundManager Instance { get; private set; }

    public enum GameState
    {
        WaitingToStart,
        TaskPhase,    // Tiempo para hacer tareas y robar
        DebatePhase,  // Tiempo para debatir y votar
        RoundEnd,     // Resolución de votación
        GameOver      // Fin de la partida
    }

    [Header("Configuración de Tiempos (Segundos)")]
    [SerializeField] private float taskPhaseDuration = 60f;
    [SerializeField] private float debatePhaseDuration = 30f;

    [Header("Variables de Red")]
    public NetworkVariable<GameState> currentGameState = new NetworkVariable<GameState>(
        GameState.WaitingToStart, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server
    );

    public NetworkVariable<float> timeRemaining = new NetworkVariable<float>(
        0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server
    );

    public NetworkVariable<int> currentRound = new NetworkVariable<int>(
        1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server
    );

    public NetworkVariable<int> totalRounds = new NetworkVariable<int>(
        1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server
    );

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer)
        {
            int totalPlayers = NetworkManager.Singleton.ConnectedClients.Count;
            totalRounds.Value = Mathf.Max(1, totalPlayers - 1);

            // El GameManager reparte los roles al arrancar la partida en red
            if (GameManager.Instance != null)
            {
                GameManager.Instance.AssignRolesToPlayers();
            }

            StartCoroutine(GameLoopRoutine());
        }
    }

    private IEnumerator GameLoopRoutine()
    {
        yield return new WaitForSeconds(2f); // Breve espera inicial

        while (currentRound.Value <= totalRounds.Value)
        {
            // --- 1. FASE DE TAREAS / ROBO ---
            currentGameState.Value = GameState.TaskPhase;
            timeRemaining.Value = taskPhaseDuration;
            SetPlayerControlsRpc(true);

            while (timeRemaining.Value > 0)
            {
                yield return new WaitForSeconds(1f);
                timeRemaining.Value -= 1f;

                // Si el Ladrón robó todo o terminó la partida, salimos del bucle
                if (GameManager.Instance != null && GameManager.Instance.isGameOver.Value)
                {
                    yield break;
                }
            }

            // --- 2. FASE DE DEBATE Y VOTACIÓN ---
            currentGameState.Value = GameState.DebatePhase;
            timeRemaining.Value = debatePhaseDuration;
            SetPlayerControlsRpc(false); // Congelamos controles

            while (timeRemaining.Value > 0)
            {
                yield return new WaitForSeconds(1f);
                timeRemaining.Value -= 1f;
            }

            // --- 3. RESOLUCIÓN DE LA RONDA ---
            currentGameState.Value = GameState.RoundEnd;

            if (GameManager.Instance != null)
            {
                GameManager.Instance.CheckWinConditions();
                if (GameManager.Instance.isGameOver.Value)
                {
                    yield break;
                }
            }

            currentRound.Value++;
            yield return new WaitForSeconds(3f);
        }

        // --- 4. FIN DE LA PARTIDA ---
        currentGameState.Value = GameState.GameOver;
        Debug.Log("<color=orange>[ROUND MANAGER] Se completaron todas las rondas.</color>");
    }

    [Rpc(SendTo.Everyone)]
    private void SetPlayerControlsRpc(bool canMove)
    {
        if (NetworkManager.Singleton.LocalClient != null &&
            NetworkManager.Singleton.LocalClient.PlayerObject != null)
        {
            var playerController = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<PlayerController>();
            if (playerController != null)
            {
                playerController.SetState(canMove ? PlayerController.PlayerState.Idle : PlayerController.PlayerState.Frozen);
            }
        }
    }

}
