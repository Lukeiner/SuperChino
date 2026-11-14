using Unity.Netcode;
using UnityEngine;

public abstract class BaseInteractable : NetworkBehaviour
{
    [Header("Configuración Base")]
    [SerializeField] protected string objectName = "Objeto Interactivo";
    [SerializeField] protected float baseInteractionTime = 4f;

    [Header("Feedback Visual")]
    [SerializeField] protected SpriteRenderer objectSpriteRenderer;
    [SerializeField] protected Color successColor = Color.green;
    [SerializeField] protected Color failColor = Color.red;

    // Sincronización de estado en red[cite: 3]
    public NetworkVariable<bool> isCompleted = new NetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server
    );

    public string ObjectName => objectName;

    // Virtual para que el ladrón pueda modificarlo
    public virtual float InteractionTime => baseInteractionTime;

    private void Awake()
    {
        if (objectSpriteRenderer == null) objectSpriteRenderer = GetComponent<SpriteRenderer>();
    }

    public override void OnNetworkSpawn()
    {
        isCompleted.OnValueChanged += OnCompletedStateChanged;
        if (isCompleted.Value && objectSpriteRenderer != null) objectSpriteRenderer.color = successColor;
    }

    public override void OnNetworkDespawn()
    {
        isCompleted.OnValueChanged -= OnCompletedStateChanged;
    }

    private void OnCompletedStateChanged(bool prev, bool current)
    {
        if (current && objectSpriteRenderer != null) objectSpriteRenderer.color = successColor;
    }

    // Método abstracto: Las clases hijas están obligadas a definir su propia validación
    public abstract bool CanPlayerInteract(PlayerIdentity playerIdentity);

    // Método virtual para emitir señales al inicio de la interacción (vacío por defecto)
    public virtual void OnInteractionStarted(PlayerIdentity playerIdentity) { }
    protected virtual void OnInteractionCompletedServer(PlayerIdentity playerIdentity) { }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void RequestInteractRpc(ulong clientId)
    {
        if (!IsServer || !IsSpawned || isCompleted.Value) return;

        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
        {
            var playerIdentity = client.PlayerObject.GetComponent<PlayerIdentity>();

            if (CanPlayerInteract(playerIdentity))
            {
                // La tarea es válida. En lugar de cambiar color, la marcamos y destruimos.
                isCompleted.Value = true;
                Debug.Log($"[SERVIDOR] Despawnear {ObjectName}");
                OnInteractionCompletedServer(playerIdentity);
                // Le decimos a Netcode que borre este objeto de todas las pantallas
                NetworkObject.Despawn(true);
            }
            else
            {
                // Si falla, sí lo pintamos de rojo para dar feedback de error
          
            }
        }
    }

    [Rpc(SendTo.Everyone)]
    public void SetColorClientRpc(Color targetColor)
    {
        if (objectSpriteRenderer != null) objectSpriteRenderer.color = targetColor;
    }
}
