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

        bool canInteract = currentInteractable.CanPlayerInteract(playerIdentity);
        string dialogMessage = currentInteractable.GetDialog(canInteract);

        playerController.SetState(PlayerController.PlayerState.Interacting);


        if (canInteract)
        {
            Debug.Log($"<color=green>[ÉXITO - {currentInteractable.ObjectName.ToUpper()}]:</color> {dialogMessage}");
            currentInteractable.RequestInteractRpc(OwnerClientId, true);
            currentInteractable = null;
        }
        else
        {
            Debug.LogWarning($"<color=red>[RECHAZADO - {currentInteractable.ObjectName.ToUpper()}]:</color> {dialogMessage}");
            currentInteractable.RequestInteractRpc(OwnerClientId, false);
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

