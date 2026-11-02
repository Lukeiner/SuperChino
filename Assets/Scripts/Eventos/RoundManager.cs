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
    [SerializeField] private float taskPhaseDuration = 60f;   // Tiempo de tareas/robo
    [SerializeField] private float debatePhaseDuration = 30f;

    [Header("Variables de Red")]
    public NetworkVariable<GameState> currentGameState = new NetworkVariable<GameState>(
        GameState.WaitingToStart,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<float> timeRemaining = new NetworkVariable<float>(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<int> currentRound = new NetworkVariable<int>(
        1,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<int> totalRounds = new NetworkVariable<int>(
        1,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Si somos el Servidor/Host, calculamos las rondas e iniciamos el loop de partida
        if (IsServer)
        {
            int totalPlayers = NetworkManager.Singleton.ConnectedClients.Count;

            // La cantidad de rondas totales es la cantidad de jugadores - 1
            totalRounds.Value = Mathf.Max(1, totalPlayers - 1);

            //  1. El GameManager reparte los roles al iniciar la sesión en red
            if (GameManager.Instance != null)
            {
                GameManager.Instance.AssignRolesToPlayers();
            }

            StartCoroutine(GameLoopRoutine());
        }
    }

    // Corrutina autoritativa ejecutada SOLO en el Servidor
    private IEnumerator GameLoopRoutine()
    {
        yield return new WaitForSeconds(2f); // Espera inicial previa al arranque

        while (currentRound.Value <= totalRounds.Value)
        {
            // --- 1. FASE DE TAREAS / ROBO ---
            currentGameState.Value = GameState.TaskPhase;
            timeRemaining.Value = taskPhaseDuration;

            // Permitimos el movimiento e interacción en todos los clientes
            SetPlayerControlsRpc(true);

            while (timeRemaining.Value > 0)
            {
                yield return new WaitForSeconds(1f);
                timeRemaining.Value -= 1f;

                // Si el Ladrón ya completó sus robos o el GameManager declaró GameOver, frenamos la fase de tareas
                if (GameManager.Instance != null && GameManager.Instance.isGameOver.Value)
                {
                    yield break;
                }
            }

            // --- 2. FASE DE DEBATE Y VOTACIÓN ---
            currentGameState.Value = GameState.DebatePhase;
            timeRemaining.Value = debatePhaseDuration;

            // Congelamos los controles durante el debate
            SetPlayerControlsRpc(false);

            while (timeRemaining.Value > 0)
            {
                yield return new WaitForSeconds(1f);
                timeRemaining.Value -= 1f;
            }

            // --- 3. RESOLUCIÓN DE LA RONDA ---
            currentGameState.Value = GameState.RoundEnd;

            // 2. Le pedimos al GameManager que evalúe si se cumplieron condiciones de fin de partida
            if (GameManager.Instance != null)
            {
                GameManager.Instance.CheckWinConditions();

                // Si la partida concluyó en el debate/votación, salimos de la corrutina
                if (GameManager.Instance.isGameOver.Value)
                {
                    yield break;
                }
            }

            currentRound.Value++;
            yield return new WaitForSeconds(3f); // Pausa breve previa a la siguiente ronda
        }

        // --- 4. FIN DE LA PARTIDA ---
        currentGameState.Value = GameState.GameOver;
        Debug.Log("<color=orange>[ROUND MANAGER] Se completaron todas las rondas de la partida.</color>");
    }

    // RPC actualizado a la nueva sintaxis para bloquear/desbloquear movimiento en clientes
    [Rpc(SendTo.Everyone)]
    private void SetPlayerControlsRpc(bool canMove)
    {
        if (NetworkManager.Singleton.LocalClient != null &&
            NetworkManager.Singleton.LocalClient.PlayerObject != null)
        {
            var playerController = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<PlayerController>();
            if (playerController != null)
            {
                if (!canMove)
                {
                    playerController.SetState(PlayerController.PlayerState.Frozen);
                }
                else
                {
                    playerController.SetState(PlayerController.PlayerState.Idle);
                }
            }
        }
    }

}
