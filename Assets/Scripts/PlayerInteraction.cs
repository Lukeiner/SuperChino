using UnityEngine;
using Unity.Netcode;

public class PlayerInteraction : NetworkBehaviour
{
    [Header("Referencias")]
    [SerializeField] private PlayerIdentity playerIdentity;
    [SerializeField] private PlayerController playerController;

    [Header("Teclas")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;

    private ObjectInteraction currentInteractable;

    private void Awake()
    {
        if (playerIdentity == null) playerIdentity = GetComponent<PlayerIdentity>();
        if (playerController == null) playerController = GetComponent<PlayerController>();
    }
    private void Update()
    {
        if (!IsOwner) return;

        if (currentInteractable != null && Input.GetKeyDown(interactKey))
        {
            TryInteractWithCurrentObject();
        }
    }

    private void TryInteractWithCurrentObject()
    {
        if (currentInteractable == null || currentInteractable.isCompleted.Value) return;

        // Guardamos la referencia local por seguridad
        ObjectInteraction targetObject = currentInteractable;

        bool canInteract = targetObject.CanPlayerInteract(playerIdentity);
        string dialogMessage = targetObject.GetDialog(canInteract);

        playerController.SetState(PlayerController.PlayerState.Interacting);

        if (canInteract)
        {
            Debug.Log($"<color=green>[ÉXITO - {targetObject.ObjectName.ToUpper()}]:</color> {dialogMessage}");

            // Limpiamos la referencia del disparador para no re-evaluarlo localmente
            currentInteractable = null;

            // Enviamos la solicitud autoritativa al Servidor
            targetObject.RequestInteractRpc(OwnerClientId);
        }
        else
        {
            Debug.LogWarning($"<color=red>[RECHAZADO - {targetObject.ObjectName.ToUpper()}]:</color> {dialogMessage}");

            // Si falló por rol, le notificamos al servidor para que pinte en rojo
            targetObject.RequestInteractRpc(OwnerClientId);
        }

        playerController.SetState(PlayerController.PlayerState.Idle);
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!IsOwner) return;

        ObjectInteraction interactable = collision.GetComponent<ObjectInteraction>();
        if (interactable != null && !interactable.isCompleted.Value)
        {
            currentInteractable = interactable;
            Debug.Log($"<color=yellow>[PISTA]:</color> Presioná [{interactKey}] para interactuar con {interactable.ObjectName}.");
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!IsOwner) return;

        ObjectInteraction interactable = collision.GetComponent<ObjectInteraction>();
        if (interactable != null && interactable == currentInteractable)
        {
            currentInteractable = null;
            Debug.Log("<color=grey>[PISTA]:</color> Te alejaste del objeto.");
        }
    }
}

