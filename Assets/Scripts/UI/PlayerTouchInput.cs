using UnityEngine;
using Unity.Netcode;

public class PlayerTouchInput : NetworkBehaviour
{
    [Header("Configuración de Tap")]
    [SerializeField] private float doubleTapTimeThreshold = 0.3f;
    private float lastTapTime = -10f;

    [Header("Zona del doble toque (fraccion de la pantalla)")]
    [Tooltip("El doble toque solo cuenta dentro de este rectangulo (a la derecha, centro). Las fracciones se miden desde la izquierda y desde arriba.")]
    [SerializeField] private float zonaXMin = 0.735f;
    [SerializeField] private float zonaYDesdeArriba = 0.40f;
    [SerializeField] private float zonaYHastaArriba = 0.70f;

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
        // 1. Mobile: se revisan todos los dedos, no solo el primero (el primero puede ser el del joystick)
        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch touch = Input.GetTouch(i);
            if (touch.phase == TouchPhase.Began)
            {
                CheckDoubleTap(touch.position, touch.fingerId);
            }
        }

        // 2. PC (click izquierdo para pruebas)
        if (Input.touchCount == 0 && Input.GetMouseButtonDown(0))
        {
            CheckDoubleTap(Input.mousePosition, -1);
        }
    }

    // El doble toque solo cuenta en el rectangulo de la derecha, para no chocar con el joystick, el boton de pausa ni el boton de accion
    private bool IsInEmojiZone(Vector2 screenPos)
    {
        float fx = screenPos.x / Screen.width;
        float fyArriba = 1f - screenPos.y / Screen.height; // 0 = arriba de la pantalla, 1 = abajo
        return fx >= zonaXMin && fyArriba >= zonaYDesdeArriba && fyArriba <= zonaYHastaArriba;
    }

    private void CheckDoubleTap(Vector2 screenPos, int fingerId)
    {
        if (!IsInEmojiZone(screenPos))
        {
            lastTapTime = -10f;
            return;
        }

        float timeSinceLastTap = Time.time - lastTapTime;

        if (timeSinceLastTap <= doubleTapTimeThreshold)
        {
            lastTapTime = -10f;
            OnDoubleTapDetected(fingerId);
        }
        else
        {
            lastTapTime = Time.time;
        }
    }

    private void OnDoubleTapDetected(int fingerId)
    {
        Debug.Log("<color=yellow>[INPUT]:</color> ¡Doble Tap detectado!");

        if (emojiWheelUI != null)
        {
            emojiWheelUI.ToggleWheel(fingerId);
        }
        else
        {
            // Re-intento de búsqueda de seguridad si falló al inicio
            emojiWheelUI = FindAnyObjectByType<EmojiWheelUI>(FindObjectsInactive.Include);
            if (emojiWheelUI != null)
            {
                emojiWheelUI.ToggleWheel(fingerId);
            }
            else
            {
                Debug.LogError("<color=red>[INPUT ERROR]:</color> No se encontró EmojiWheelUI en la escena.");
            }
        }
    }

}
