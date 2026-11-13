using UnityEngine;
using Unity.Netcode;
using System.Collections;
public class PlayerInteraction : NetworkBehaviour
{
    [Header("Referencias")]
    [SerializeField] private PlayerIdentity playerIdentity;
    [SerializeField] private PlayerController playerController;

    [Header("Teclas")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;

    private BaseInteractable currentInteractable;

    private void Awake()
    {
        if (playerIdentity == null) playerIdentity = GetComponent<PlayerIdentity>();
        if (playerController == null) playerController = GetComponent<PlayerController>();
    }
    private void Update()
    {
        if (!IsOwner) return;

        // Si estamos en fase de debate o fuera de la fase de tareas, no leemos interacción
        if (RoundManager.Instance != null && RoundManager.Instance.currentGameState.Value != RoundManager.GameState.TaskPhase)
        {
            return;
        }

        if (currentInteractable != null && !currentInteractable.isCompleted.Value && Input.GetKeyDown(interactKey))
        {
            TryInteractWithCurrentObject();
        }
    }

    private void TryInteractWithCurrentObject()
    {
        if (currentInteractable == null || currentInteractable.isCompleted.Value) return;

        bool canInteract = currentInteractable.CanPlayerInteract(playerIdentity);

        if (canInteract)
        {
            BaseInteractable targetObject = currentInteractable;
            currentInteractable = null;
            StartCoroutine(ProcessInteractionRoutine(targetObject));
        }
        else
        {
            currentInteractable.RequestInteractRpc(OwnerClientId);
        }
    }
    private IEnumerator ProcessInteractionRoutine(BaseInteractable targetObject)
    {
        // 1. Congelamos al jugador y emitimos la señal (si es un objeto robable)
        playerController.SetState(PlayerController.PlayerState.Interacting);
        targetObject.OnInteractionStarted(playerIdentity);

        // 2. Esperamos los segundos definidos en el inspector (el Ladrón esperará más)
        yield return new WaitForSeconds(targetObject.InteractionTime);

        // 3. Consumamos la interacción en el servidor y liberamos al jugador
        targetObject.RequestInteractRpc(OwnerClientId);
        playerController.SetState(PlayerController.PlayerState.Idle);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!IsOwner) return;

        BaseInteractable interactable = collision.GetComponent<BaseInteractable>();
        if (interactable != null && !interactable.isCompleted.Value)
        {
            currentInteractable = interactable;
            Debug.Log($"<color=yellow>[PISTA]:</color> Presioná [{interactKey}] para interactuar con {interactable.ObjectName}.");
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!IsOwner) return;

        BaseInteractable interactable = collision.GetComponent<BaseInteractable>();
        if (interactable != null && !interactable.isCompleted.Value)
        {
            currentInteractable = null;
            Debug.Log("<color=grey>[PISTA]:</color> Te alejaste del objeto.");
        }
    }
}

