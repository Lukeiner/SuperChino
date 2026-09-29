using UnityEngine;

public class PlayerInteraction : MonoBehaviour
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
        if (currentInteractable != null && Input.GetKeyDown(interactKey))
        {
            TryInteractWithCurrentObject();
        }
    }

    private void TryInteractWithCurrentObject()
    {
        bool canInteract = currentInteractable.CanPlayerInteract(playerIdentity);

        // Obtenemos el diálogo configurado en el Inspector según la validación de rol
        string dialogMessage = currentInteractable.GetDialog(canInteract);

        if (canInteract)
        {
            playerController.SetState(PlayerController.PlayerState.Interacting);

            // Imprime el diálogo de éxito en consola
            Debug.Log($"<color=green>[DIÁLOGO DE {currentInteractable.ObjectName.ToUpper()}]:</color> {dialogMessage}");

            currentInteractable.Interact(playerIdentity);

            playerController.SetState(PlayerController.PlayerState.Idle);
        }
        else
        {
            Debug.Log($"<color=red>[DIÁLOGO DE {currentInteractable.ObjectName.ToUpper()}]:</color> {dialogMessage}");
        }
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        ObjectInteraction interactable = collision.GetComponent<ObjectInteraction>();
        if (interactable != null)
        {
            currentInteractable = interactable;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        ObjectInteraction interactable = collision.GetComponent<ObjectInteraction>();
        if (interactable != null && interactable == currentInteractable)
        {
            currentInteractable = null;
        }
    }
}

