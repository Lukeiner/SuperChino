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
        bool canInteract = currentInteractable.CanPlayerInteract(playerIdentity);
        string dialogMessage = currentInteractable.GetDialog(canInteract);

        if (canInteract)
        {
            playerController.SetState(PlayerController.PlayerState.Interacting);

            Debug.Log($"<color=green>[ÉXITO - {currentInteractable.ObjectName.ToUpper()}]:</color> {dialogMessage}");

   
            currentInteractable.RequestInteractRpc(OwnerClientId);

            // Cortamos la referencia inmediatamente para evitar re-entradas/bucle
            currentInteractable = null;

            playerController.SetState(PlayerController.PlayerState.Idle);
        }
        else
        {
            Debug.LogWarning($"<color=red>[RECHAZADO - {currentInteractable.ObjectName.ToUpper()}]:</color> {dialogMessage}");
        }
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

