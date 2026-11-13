using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;


public class NPCGuardAI : NetworkBehaviour
{
    public enum GuardState { Patrol, Inspecting, Chasing }

    [Header("Objetos Sagrados (Puntos de Patrulla)")]
    [Tooltip("Lista de objetos sagrados a los que irá el guardia")]
    public Transform[] objetosSagrados;
    public float waitTimeAtObject = 3f;

    [Header("Parámetros de Steering Movement")]
    public float maxSpeed = 4.5f;
    public float chaseSpeed = 6.5f;
    public float maxForce = 12f;
    public float slowingRadius = 2.5f;
    public float arrivalThreshold = 0.6f;

    [Header("Detección del Ladrón")]
    public float viewRadius = 8f;
    [Range(0, 360)] public float viewAngle = 90f;
    public LayerMask playerMask;
    public LayerMask obstacleMask;
    public float catchDistance = 1.2f;

    // Control Interno
    private GuardState currentState = GuardState.Patrol;
    private int currentWaypointIndex = 0;
    private Vector3 velocity;
    private float inspectTimer = 0f;
    private Transform currentTargetPlayer;

    public override void OnNetworkSpawn()
    {
        // La IA y la física solo deben correr en el Servidor / Host
        if (!IsServer)
        {
            enabled = false;
            return;
        }

        if (objetosSagrados == null || objetosSagrados.Length == 0)
        {
            Debug.LogError($"[NetworkGuardAI] {gameObject.name} no tiene Objetos Sagrados asignados.");
        }
    }

    private void Update()
    {
        if (!IsServer) return;

        // 1. Escaneo constante de jugadores en red
        DetectPlayers();

        // 2. Máquina de Estados de la IA
        switch (currentState)
        {
            case GuardState.Patrol:
                PatrolBehavior();
                break;

            case GuardState.Inspecting:
                InspectBehavior();
                break;

            case GuardState.Chasing:
                ChaseBehavior();
                break;
        }
    }

    #region Lógica de Estados

    private void PatrolBehavior()
    {
        if (objetosSagrados.Length == 0) return;

        Transform targetSpot = objetosSagrados[currentWaypointIndex];
        float distanceToSpot = Vector3.Distance(transform.position, targetSpot.position);

        // Si está llegando al objeto sagrado, desacelera (Arrive)
        Vector3 steering = Arrive(targetSpot.position, maxSpeed);
        ApplySteering(steering, maxSpeed);

        // Al alcanzar el objeto sagrado, cambia a estado de inspección
        if (distanceToSpot <= arrivalThreshold)
        {
            currentState = GuardState.Inspecting;
            inspectTimer = waitTimeAtObject;
            velocity = Vector3.zero; // Detener movimiento durante la espera
        }
    }

    private void InspectBehavior()
    {
        inspectTimer -= Time.deltaTime;

        // Transición: pasar al siguiente objeto sagrado tras esperar
        if (inspectTimer <= 0f)
        {
            currentWaypointIndex = (currentWaypointIndex + 1) % objetosSagrados.Length;
            currentState = GuardState.Patrol;
        }
    }

    private void ChaseBehavior()
    {
        if (currentTargetPlayer == null)
        {
            currentState = GuardState.Patrol;
            return;
        }

        float distToPlayer = Vector3.Distance(transform.position, currentTargetPlayer.position);

        // Persecución agresiva (Seek continuo a velocidad máxima)
        Vector3 steering = Seek(currentTargetPlayer.position, chaseSpeed);
        ApplySteering(steering, chaseSpeed);

        // Condición de captura / derrota
        if (distToPlayer <= catchDistance)
        {
            TriggerGameOverClientRpc(currentTargetPlayer.GetComponent<NetworkObject>().OwnerClientId);
            enabled = false; // Desactivar la IA tras atrapar al jugador
        }
    }

    #endregion

    #region Steering Behaviors Core

    private Vector3 Seek(Vector3 targetPosition, float speed)
    {
        Vector3 desiredVelocity = (targetPosition - transform.position).normalized * speed;
        return desiredVelocity - velocity;
    }

    private Vector3 Arrive(Vector3 targetPosition, float speed)
    {
        Vector3 desiredVelocity = targetPosition - transform.position;
        float distance = desiredVelocity.magnitude;

        if (distance < slowingRadius)
        {
            desiredVelocity = desiredVelocity.normalized * speed * (distance / slowingRadius);
        }
        else
        {
            desiredVelocity = desiredVelocity.normalized * speed;
        }

        return desiredVelocity - velocity;
    }

    private void ApplySteering(Vector3 steeringForce, float currentMaxSpeed)
    {
        steeringForce = Vector3.ClampMagnitude(steeringForce, maxForce);
        velocity = Vector3.ClampMagnitude(velocity + steeringForce * Time.deltaTime, currentMaxSpeed);

        transform.position += velocity * Time.deltaTime;

        // Evita calcular rotación si el movimiento es imperceptible o insignificante
        if (velocity.sqrMagnitude > 0.1f)
        {
            Vector3 flatVelocity = new Vector3(velocity.x, 0, velocity.z); // Ignorar inclinación en Y
            if (flatVelocity != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(flatVelocity);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
            }
        }
    }

    #endregion

    #region Sistema de Visión y Red

    private void DetectPlayers()
    {
        // Buscar todos los objetos de jugadores dentro del radio de visión
        Collider[] targetsInRadius = Physics.OverlapSphere(transform.position, viewRadius, playerMask);

        foreach (Collider target in targetsInRadius)
        {
            Vector3 dirToTarget = (target.transform.position - transform.position).normalized;
            float distToTarget = Vector3.Distance(transform.position, target.transform.position);

            // 1. Validar Ángulo de Visión (Cono)
            if (Vector3.Angle(transform.forward, dirToTarget) < viewAngle / 2f)
            {
                // 2. Raycast para evitar ver a través de paredes o estanterías
                if (!Physics.Raycast(transform.position, dirToTarget, distToTarget, obstacleMask))
                {
                    // Ladrón detectado
                    currentTargetPlayer = target.transform;
                    currentState = GuardState.Chasing;
                    return;
                }
            }
        }

        // Perder de vista al jugador si se aleja demasiado o se oculta
        if (currentState == GuardState.Chasing && currentTargetPlayer != null)
        {
            float distToCurrent = Vector3.Distance(transform.position, currentTargetPlayer.position);
            Vector3 dirToCurrent = (currentTargetPlayer.position - transform.position).normalized;

            if (distToCurrent > viewRadius * 1.3f || Physics.Raycast(transform.position, dirToCurrent, distToCurrent, obstacleMask))
            {
                currentTargetPlayer = null;
                currentState = GuardState.Patrol;
            }
        }
    }

    [ClientRpc]
    private void TriggerGameOverClientRpc(ulong capturedClientId)
    {
        Debug.Log($"[Game Over] El jugador con ID {capturedClientId} fue capturado por el guardia.");

        if (NetworkManager.Singleton.LocalClientId == capturedClientId)
        {
            // Lógica local de derrota
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Dibujar Cono de Visión en la vista de escena
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, viewRadius);

        Vector3 viewAngleA = Quaternion.Euler(0, -viewAngle / 2f, 0) * transform.forward;
        Vector3 viewAngleB = Quaternion.Euler(0, viewAngle / 2f, 0) * transform.forward;

        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, transform.position + viewAngleA * viewRadius);
        Gizmos.DrawLine(transform.position, transform.position + viewAngleB * viewRadius);
    }

    #endregion
}
