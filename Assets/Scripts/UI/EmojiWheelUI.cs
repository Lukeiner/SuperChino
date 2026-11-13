using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class EmojiWheelUI : MonoBehaviour
{
    [Header("Paneles de UI")]
    [SerializeField] private GameObject wheelPanel;
    [SerializeField] private RectTransform wheelRectTransform;

    [Header("Configuración de Animación")]
    [SerializeField] private float popupDuration = 0.15f;

    [Header("Lista de Botones de la Ruleta")]
    [SerializeField] private List<EmojiButtonData> emojiButtons = new List<EmojiButtonData>();

    [System.Serializable]
    public class EmojiButtonData
    {
        public Image buttonImage;
        public string emojiSymbol;
        public Color normalColor = Color.white;
        public Color highlightColor = Color.yellow;
    }

    private Coroutine scaleCoroutine;
    private int currentlySelectedIdx = -1;
    private bool isHoldingWheel = false;

    private void Awake()
    {
        if (wheelPanel == null)
            wheelPanel = gameObject;

        if (wheelRectTransform == null)
            wheelRectTransform = wheelPanel.GetComponent<RectTransform>();

        wheelPanel.SetActive(false);
    }

    private void Update()
    {
        // Si la ruleta está abierta y el jugador mantiene presionado el dedo / clic
        if (isHoldingWheel && wheelPanel.activeSelf)
        {
            ProcessDragSelection();

            // Detectar cuando se suelta el dedo/clic (PointerUp)
            if (Input.GetMouseButtonUp(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Ended))
            {
                ConfirmSelectionAndClose();
            }
        }
    }

    //  1. POP-UP ANIMADO (Abre la ruleta con animación de escala)
    public void OpenWheelWithPopup()
    {
        wheelPanel.SetActive(true);
        isHoldingWheel = true;
        currentlySelectedIdx = -1;

        ResetButtonHighlights();

        if (scaleCoroutine != null) StopCoroutine(scaleCoroutine);
        scaleCoroutine = StartCoroutine(AnimateScale(Vector3.zero, Vector3.one));
    }

    public void ToggleWheel()
    {
        if (wheelPanel.activeSelf)
        {
            CloseWheel();
        }
        else
        {
            OpenWheelWithPopup();
        }
    }

    public void CloseWheel()
    {
        isHoldingWheel = false;
        if (scaleCoroutine != null) StopCoroutine(scaleCoroutine);
        scaleCoroutine = StartCoroutine(AnimateScale(wheelRectTransform.localScale, Vector3.zero, () =>
        {
            wheelPanel.SetActive(false);
        }));
    }

    // 2. ARRASTRE Y SELECCIÓN (Hold & Drag)
    private void ProcessDragSelection()
    {
        Vector2 inputPosition = Input.mousePosition;
        if (Input.touchCount > 0)
        {
            inputPosition = Input.GetTouch(0).position;
        }

        Vector2 centerPos = RectTransformUtility.WorldToScreenPoint(null, wheelRectTransform.position);
        Vector2 dir = inputPosition - centerPos;

        // Si el dedo está demasiado cerca del centro, no selecciona ningún emoji todavía
        if (dir.magnitude < 40f)
        {
            if (currentlySelectedIdx != -1)
            {
                currentlySelectedIdx = -1;
                ResetButtonHighlights();
            }
            return;
        }

        // Calcular el ángulo del arrastre (0 a 360 grados)
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        if (angle < 0) angle += 360f;

        // Determinar qué botón está en esa dirección
        int newIndex = GetClosestButtonIndex(angle);

        if (newIndex != currentlySelectedIdx)
        {
            currentlySelectedIdx = newIndex;
            HighlightSelectedButton(currentlySelectedIdx);
        }
    }

    private int GetClosestButtonIndex(float angle)
    {
        if (emojiButtons.Count == 0) return -1;
        float step = 360f / emojiButtons.Count;

        // Ajustamos el desfase según la posición de los 4 botones
        for (int i = 0; i < emojiButtons.Count; i++)
        {
            float targetAngle = i * step;
            float diff = Mathf.Abs(Mathf.DeltaAngle(angle, targetAngle));
            if (diff <= step / 2f)
            {
                return i;
            }
        }
        return 0;
    }

    private void HighlightSelectedButton(int selectedIdx)
    {
        for (int i = 0; i < emojiButtons.Count; i++)
        {
            if (emojiButtons[i].buttonImage != null)
            {
                emojiButtons[i].buttonImage.color = (i == selectedIdx)
                    ? emojiButtons[i].highlightColor
                    : emojiButtons[i].normalColor;
            }
        }
    }

    private void ResetButtonHighlights()
    {
        foreach (var b in emojiButtons)
        {
            if (b.buttonImage != null) b.buttonImage.color = b.normalColor;
        }
    }

    //  3. CONFIRMACIÓN Y ENVÍO AL SOLTAR EL DEDO
    private void ConfirmSelectionAndClose()
    {
        if (currentlySelectedIdx >= 0 && currentlySelectedIdx < emojiButtons.Count)
        {
            string selectedEmoji = emojiButtons[currentlySelectedIdx].emojiSymbol;
            Debug.Log($"<color=green>[SELECCIONADO]:</color> Soltaste sobre: {selectedEmoji}");

            SendEmojiToNetwork(selectedEmoji);
        }

        CloseWheel();
    }

    private void SendEmojiToNetwork(string emojiSymbol)
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.LocalClient != null)
        {
            var localPlayer = NetworkManager.Singleton.LocalClient.PlayerObject;
            if (localPlayer != null)
            {
                var playerEmojiHandler = localPlayer.GetComponent<PlayerEmojiHandler>();
                if (playerEmojiHandler != null)
                {
                    playerEmojiHandler.SendEmojiRpc(emojiSymbol);
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
            float t = elapsed / popupDuration;
            // Curva elástica / Smoothstep para efecto pop de apertura
            t = Mathf.Sin(t * Mathf.PI * 0.5f);
            wheelRectTransform.localScale = Vector3.Lerp(from, to, t);
            yield return null;
        }
        wheelRectTransform.localScale = to;
        onComplete?.Invoke();
    }
}
