using System;
using UnityEngine;

public class StealableObject : BaseInteractable
{
    [Header("Reglas de Robo")]
    [SerializeField] private float stealExtraTime = 2f; // El Ladrón tarda más

    // Evento ciego: el objeto lo grita, no le importa quién lo escuche
    public static event Action<Transform, Transform> OnStealStarted;

    // Polimorfismo: Sumamos el tiempo extra al tiempo base
    public override float InteractionTime => baseInteractionTime + stealExtraTime;

    public override bool CanPlayerInteract(PlayerIdentity playerIdentity)
    {
        if (playerIdentity == null || isCompleted.Value) return false;

        // Validación exclusiva de Ladrón
        return playerIdentity.IsLadron();

    }

    public override void OnInteractionStarted(PlayerIdentity playerIdentity)
    {
        // Disparamos la señal al sistema en cuanto empieza el robo
        OnStealStarted?.Invoke(playerIdentity.transform, this.transform);
    }
}
