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
        playerController.SetState(PlayerController.PlayerState.Interacting);
        targetObject.OnInteractionStarted(playerIdentity);

        //  Si es un objeto robable (o el jugador es Ladrón), activamos el hiperfoco 1.3x
        bool isStealing = targetObject is StealableObject;
        if (isStealing && CameraFollow.Instance != null)
        {
            CameraFollow.Instance.SetZoomMultiplier(1.3f);
        }

        // 2. Esperamos el tiempo de robo
        yield return new WaitForSeconds(targetObject.InteractionTime);

        // Restauramos el zoom normal de la cámara al terminar
        if (isStealing && CameraFollow.Instance != null)
        {
            CameraFollow.Instance.SetZoomMultiplier(1f);
        }

        // 3. Consumamos la interacción en el servidor y liberamos movimiento
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

