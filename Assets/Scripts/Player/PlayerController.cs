using UnityEngine;
using Unity.Netcode;
public class PlayerController : NetworkBehaviour
{
    [Header("Configuración de Movimiento")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private AudioListener playerAudioListener;

    [Header("Estado del jugador")]
    public PlayerState currentState = PlayerState.Idle;

    public enum PlayerState { Idle, Moving, Interacting, Frozen}

    private Vector2 moveInput;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (!IsOwner)
        {
            if (playerAudioListener != null) playerAudioListener.enabled = false;
         
        }

    }
    private void Update()
    {
        if (!IsOwner) return;

        ReadInput();
    }

    private void FixedUpdate()
    {
        if (!IsOwner) return;

        MovePlayer();
    }

    private void ReadInput()
    {
        //Reemplacé las dos lineas de moveX y moveY en una sola y abajo está el touch para el tactil
        Vector2 keyboard = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        Vector2 touch = HUDUI.MoveInput;

        // Si se usas el joystick, manda él (la velocidad depende de cuánto lo inclinas), si no, el teclado
        moveInput = touch.sqrMagnitude > 0f ? touch : keyboard.normalized;

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
