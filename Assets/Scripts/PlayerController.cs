using UnityEngine;
using Unity.Netcode;
public class PlayerController : NetworkBehaviour
{
    [Header("Configuración de Movimiento")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private Rigidbody2D rb;

    [Header("Estado del jugador")]
    public PlayerState currentState = PlayerState.Idle;

    public enum PlayerState { Idle, Moving, Interacting, Frozen}

    private Vector2 moveInput;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsOwner)
        {
            if (CameraFollow.Instance != null)
            {
                CameraFollow.Instance.SetTarget(transform);
            }
        }
    }
    private void Update()
    {
        if (!IsOwner) return;

        if (currentState == PlayerState.Interacting || currentState == PlayerState.Frozen)
        {
            moveInput = Vector2.zero;
            return;
        }

        ReadInput();
    }

    private void FixedUpdate()
    {
        if (!IsOwner) return;

        MovePlayer();
    }

    private void ReadInput()
    {
        
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveY = Input.GetAxisRaw("Vertical");

        moveInput = new Vector2(moveX, moveY).normalized;

        // Actualizamos estado básico
        if (moveInput != Vector2.zero)
        {
            currentState = PlayerState.Moving;
        }
        else
        {
            currentState = PlayerState.Idle;
        }
    }
    
    private void MovePlayer()
    {
        rb.MovePosition(rb.position + moveInput * (moveSpeed * Time.fixedDeltaTime));
    }

    public void SetState(PlayerState newState)
    {
        if (currentState == newState) return;
        currentState = newState;
    }

}
