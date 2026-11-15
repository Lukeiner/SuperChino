using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

/// <summary>
/// Rueda de emojis (torta). Se abre con doble toque (PlayerTouchInput), se apunta arrastrando el dedo
/// y al soltar se manda el emoji elegido a todos los jugadores (PlayerEmojiHandler).
/// La rueda se arma sola al iniciar con las imagenes de Assets/Resources/Emojis:
///   rueda_fondo, rueda_resaltado, rueda_lineas y emoji_[nombre] por cada emoji de la lista.
/// rueda_lineas (las lineas punteadas) y rueda_resaltado (la porcion roja) estan dibujadas para 5 porciones:
/// si se cambia la cantidad de emojis hay que volver a dibujar esas dos imagenes.
/// </summary>
public class EmojiWheelUI : MonoBehaviour
{
    [Header("Paneles de UI")]
    [SerializeField] private GameObject wheelPanel;
    [SerializeField] private RectTransform wheelRectTransform;

    [Header("Configuración de Animación")]
    [SerializeField] private float popupDuration = 0.15f;

    [Header("Emojis (en orden: el primero a la derecha y sigue girando hacia arriba)")]
    [SerializeField] private string[] emojis = { "alerta", "susto", "risa", "sospecha", "sombrero" };

    [Header("Tamaños (fraccion del tamaño de la rueda)")]
    [SerializeField] private float distanciaIconos = 0.29f;   // que tan lejos del centro van los emojis
    [SerializeField] private float tamanoIcono = 0.22f;
    [SerializeField] private float tamanoSeleccionado = 0.27f;
    [SerializeField] private float zonaMuerta = 40f;          // px desde el centro donde no se elige nada

    private const string Carpeta = "Emojis/";

    private Image resaltado;
    private RectTransform[] iconos;
    private float lado;

    private Coroutine scaleCoroutine;
    private int currentlySelectedIdx = -1;
    private bool isHoldingWheel = false;
    private int trackedFingerId = -1; // dedo que abrio la rueda (-1 = mouse, para probar en el editor)

    private void Awake()
    {
        if (wheelPanel == null)
            wheelPanel = gameObject;

        if (wheelRectTransform == null)
            wheelRectTransform = wheelPanel.GetComponent<RectTransform>();

        ConstruirRueda();
        wheelPanel.SetActive(false);
    }

    // ---------- armado de la rueda ----------

    private void ConstruirRueda()
    {
        // apaga los botones viejos (Emoji1..4) que quedaron en la escena
        foreach (Transform hijo in wheelRectTransform)
            hijo.gameObject.SetActive(false);

        lado = Mathf.Min(wheelRectTransform.rect.width, wheelRectTransform.rect.height);

        Image fondo = wheelPanel.GetComponent<Image>();
        if (fondo != null)
        {
            Sprite s = Cargar("rueda_fondo");
            if (s != null) fondo.sprite = s;
            fondo.preserveAspect = true;
            fondo.color = Color.white;
        }

        // porcion roja: viene dibujada apuntando a la derecha y se gira hacia el emoji elegido
        resaltado = CrearImagen("Resaltado", Cargar("rueda_resaltado"), lado);
        resaltado.enabled = false;

        CrearImagen("Lineas", Cargar("rueda_lineas"), lado);

        iconos = new RectTransform[emojis.Length];
        float paso = emojis.Length > 0 ? 360f / emojis.Length : 0f;
        for (int i = 0; i < emojis.Length; i++)
        {
            Image icono = CrearImagen("Emoji_" + emojis[i], Cargar("emoji_" + emojis[i]), lado * tamanoIcono);
            float a = i * paso * Mathf.Deg2Rad;
            icono.rectTransform.anchoredPosition = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * (lado * distanciaIconos);
            iconos[i] = icono.rectTransform;
        }
    }

    private Image CrearImagen(string nombre, Sprite sprite, float tamano)
    {
        GameObject go = new GameObject(nombre, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(wheelRectTransform, false);
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(tamano, tamano);
        rt.anchoredPosition = Vector2.zero;

        Image img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.preserveAspect = true;
        img.raycastTarget = false;
        return img;
    }

    private Sprite Cargar(string nombre)
    {
        Sprite s = Resources.Load<Sprite>(Carpeta + nombre);
        if (s == null) Debug.LogWarning($"[EmojiWheelUI] Falta la imagen Resources/{Carpeta}{nombre}.png", this);
        return s;
    }

    // ---------- abrir / cerrar ----------

    private void Update()
    {
        // Si la ruleta está abierta y el jugador mantiene presionado el dedo / clic
        if (isHoldingWheel && wheelPanel.activeSelf)
        {
            ProcessDragSelection();

            // Detectar cuando se suelta el dedo/clic (PointerUp)
            if (PointerReleased())
            {
                ConfirmSelectionAndClose();
            }
        }
    }

    public void OpenWheelWithPopup()
    {
        wheelPanel.SetActive(true);
        isHoldingWheel = true;
        currentlySelectedIdx = -1;

        Resaltar(-1);

        if (scaleCoroutine != null) StopCoroutine(scaleCoroutine);
        scaleCoroutine = StartCoroutine(AnimateScale(Vector3.zero, Vector3.one));
    }

    // Se usa desde el doble toque: la rueda sigue al dedo que la abrio, no al primer dedo de la pantalla
    // (el primer dedo puede ser el del joystick).
    public void ToggleWheel(int fingerId)
    {
        trackedFingerId = fingerId;
        ToggleWheel();
    }

    public void ToggleWheel()
    {
        if (wheelPanel.activeSelf) CloseWheel();
        else OpenWheelWithPopup();
    }

    public void CloseWheel()
    {
        isHoldingWheel = false;
        trackedFingerId = -1;
        if (scaleCoroutine != null) StopCoroutine(scaleCoroutine);
        scaleCoroutine = StartCoroutine(AnimateScale(wheelRectTransform.localScale, Vector3.zero, () =>
        {
            wheelPanel.SetActive(false);
        }));
    }

    private bool TryGetTrackedTouch(out Touch touch)
    {
        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch t = Input.GetTouch(i);
            if (t.fingerId == trackedFingerId)
            {
                touch = t;
                return true;
            }
        }
        touch = default;
        return false;
    }

    private Vector2 GetPointerPosition()
    {
        if (trackedFingerId >= 0 && TryGetTrackedTouch(out Touch touch))
            return touch.position;
        return Input.mousePosition;
    }

    private bool PointerReleased()
    {
        if (trackedFingerId >= 0)
        {
            if (TryGetTrackedTouch(out Touch touch))
                return touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled;
            return true; // el dedo ya no esta
        }
        return Input.GetMouseButtonUp(0);
    }

    // ---------- arrastre y selección ----------

    private void ProcessDragSelection()
    {
        Vector2 inputPosition = GetPointerPosition();
        Vector2 centerPos = RectTransformUtility.WorldToScreenPoint(null, wheelRectTransform.position);
        Vector2 dir = inputPosition - centerPos;

        // Si el dedo está demasiado cerca del centro, no selecciona ningún emoji todavía
        int nuevo = -1;
        if (dir.magnitude >= zonaMuerta && emojis.Length > 0)
        {
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            if (angle < 0) angle += 360f;
            float paso = 360f / emojis.Length;
            nuevo = Mathf.RoundToInt(angle / paso) % emojis.Length;
        }

        if (nuevo != currentlySelectedIdx)
        {
            currentlySelectedIdx = nuevo;
            Resaltar(nuevo);
        }
    }

    private void Resaltar(int indice)
    {
        if (resaltado != null)
        {
            resaltado.enabled = indice >= 0;
            if (indice >= 0)
            {
                float paso = 360f / emojis.Length;
                resaltado.rectTransform.localEulerAngles = new Vector3(0f, 0f, indice * paso);
            }
        }

        if (iconos == null) return;
        for (int i = 0; i < iconos.Length; i++)
        {
            float t = (i == indice) ? tamanoSeleccionado : tamanoIcono;
            iconos[i].sizeDelta = new Vector2(lado * t, lado * t);
        }
    }

    // ---------- confirmación y envío al soltar el dedo ----------

    private void ConfirmSelectionAndClose()
    {
        if (currentlySelectedIdx >= 0 && currentlySelectedIdx < emojis.Length)
        {
            string elegido = emojis[currentlySelectedIdx];
            Debug.Log($"<color=green>[SELECCIONADO]:</color> Soltaste sobre: {elegido}");
            SendEmojiToNetwork(elegido);
        }

        CloseWheel();
    }

    private void SendEmojiToNetwork(string emoji)
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.LocalClient != null)
        {
            var localPlayer = NetworkManager.Singleton.LocalClient.PlayerObject;
            if (localPlayer != null)
            {
                var playerEmojiHandler = localPlayer.GetComponent<PlayerEmojiHandler>();
                if (playerEmojiHandler != null)
                {
                    playerEmojiHandler.SendEmojiRpc(emoji);
                }
            }
        }
    }

    // Corrutina para el Pop-Up de escala suave
    private IEnumerator AnimateScale(Vector3 from, Vector3 to, System.Action onComplete = null)
    {
        float elapsed = 0f;
        while (elapsed < popupDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Sin(Mathf.Clamp01(elapsed / popupDuration) * Mathf.PI * 0.5f);
            wheelRectTransform.localScale = Vector3.Lerp(from, to, t);
            yield return null;
        }
        wheelRectTransform.localScale = to;
        onComplete?.Invoke();
    }
}
