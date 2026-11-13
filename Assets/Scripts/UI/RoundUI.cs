using TMPro;
using UnityEngine;

public class RoundUI : MonoBehaviour
{
    [Header("Referencias de UI")]
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private TMP_Text phaseText;
    [SerializeField] private TMP_Text roundText;

    private void Start()
    {
        // Si el RoundManager ya está spawnado en la red, nos suscribimos a las variables
        if (RoundManager.Instance != null)
        {
            SubscribeToNetworkVariables();
        }
    }

    private void SubscribeToNetworkVariables()
    {
        // Escuchamos los cambios en el tiempo, fase y número de ronda
        RoundManager.Instance.timeRemaining.OnValueChanged += OnTimeChanged;
        RoundManager.Instance.currentGameState.OnValueChanged += OnGameStateChanged;
        RoundManager.Instance.currentRound.OnValueChanged += OnRoundChanged;

        // Actualización inicial
        UpdateTimerDisplay(RoundManager.Instance.timeRemaining.Value);
        UpdatePhaseDisplay(RoundManager.Instance.currentGameState.Value);
        UpdateRoundDisplay(RoundManager.Instance.currentRound.Value, RoundManager.Instance.totalRounds.Value);
    }

    private void OnDestroy()
    {
        // Cancelamos las suscripciones al destruir el objeto para evitar memory leaks
        if (RoundManager.Instance != null)
        {
            RoundManager.Instance.timeRemaining.OnValueChanged -= OnTimeChanged;
            RoundManager.Instance.currentGameState.OnValueChanged -= OnGameStateChanged;
            RoundManager.Instance.currentRound.OnValueChanged -= OnRoundChanged;
        }
    }

    // Callbacks de cambio de valor
    private void OnTimeChanged(float previousValue, float newValue)
    {
        UpdateTimerDisplay(newValue);
    }

    private void OnGameStateChanged(RoundManager.GameState previousState, RoundManager.GameState newState)
    {
        UpdatePhaseDisplay(newState);
    }

    private void OnRoundChanged(int previousRound, int newRound)
    {
        if (RoundManager.Instance != null)
        {
            UpdateRoundDisplay(newRound, RoundManager.Instance.totalRounds.Value);
        }
    }

    // Formateo de texto en pantalla
    private void UpdateTimerDisplay(float seconds)
    {
        if (timerText != null)
        {
            int displaySeconds = Mathf.Max(0, Mathf.CeilToInt(seconds));
            timerText.text = $"{displaySeconds}s";
        }
    }

    private void UpdatePhaseDisplay(RoundManager.GameState state)
    {
        if (phaseText == null) return;

        switch (state)
        {
            case RoundManager.GameState.WaitingToStart:
                phaseText.text = "Esperando inicio...";
                break;
            case RoundManager.GameState.TaskPhase:
                phaseText.text = "¡Fase de Tareas!";
                phaseText.color = Color.green;
                break;
            case RoundManager.GameState.DebatePhase:
                phaseText.text = "¡Fase de Debate!";
                phaseText.color = Color.yellow;
                break;
            case RoundManager.GameState.RoundEnd:
                phaseText.text = "Resolución de Votación";
                phaseText.color = Color.orange;
                break;
            case RoundManager.GameState.GameOver:
                phaseText.text = "FIN DE LA PARTIDA";
                phaseText.color = Color.red;
                break;
        }
    }

    private void UpdateRoundDisplay(int current, int total)
    {
        if (roundText != null)
        {
            roundText.text = $"Ronda: {current} / {total}";
        }
    }
}
