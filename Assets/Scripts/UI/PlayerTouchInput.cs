using UnityEngine;
using Unity.Netcode;

public class PlayerTouchInput : NetworkBehaviour
{
    [Header("Configuración de Tap")]
    [SerializeField] private float doubleTapTimeThreshold = 0.3f;
    private float lastTapTime = 0f;

    [Header("Referencias")]
    [SerializeField] private EmojiWheelUI emojiWheelUI;

    private void Start()
    {
        if (emojiWheelUI == null)
        {
            // El parámetro 'FindObjectsInactive.Include' permite encontrar la UI aunque arranque oculta en la escena
            emojiWheelUI = FindAnyObjectByType<EmojiWheelUI>(FindObjectsInactive.Include);
        }
    }

    private void Update()
    {
        // Solo el jugador dueño de esta pantalla detecta sus toques
        if (!IsOwner) return;

        DetectDoubleTap();
    }

    private void DetectDoubleTap()
    {
        // 1. Mobile (Pantalla Táctil)
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                CheckDoubleTap();
            }
        }
        // 2. PC (Click Izquierdo para pruebas)
        else if (Input.GetMouseButtonDown(0))
        {
            CheckDoubleTap();
        }
    }

    private void CheckDoubleTap()
    {
        float timeSinceLastTap = Time.time - lastTapTime;

        if (timeSinceLastTap <= doubleTapTimeThreshold)
        {
            OnDoubleTapDetected();
        }

        lastTapTime = Time.time;
    }

    private void OnDoubleTapDetected()
    {
        Debug.Log("<color=yellow>[INPUT]:</color> ¡Doble Tap detectado!");

        if (emojiWheelUI != null)
        {
            emojiWheelUI.ToggleWheel();
        }
        else
        {
            // Re-intento de búsqueda de seguridad si falló al inicio
            emojiWheelUI = FindAnyObjectByType<EmojiWheelUI>(FindObjectsInactive.Include);
            if (emojiWheelUI != null)
            {
                emojiWheelUI.OpenWheelWithPopup();
            }
            else
            {
                Debug.LogError("<color=red>[INPUT ERROR]:</color> No se encontró EmojiWheelUI en la escena.");
            }
        }
    }

}
