using System.Collections;
using Unity.Netcode;
using TMPro;
using UnityEngine;

/// <summary>
/// Muestra el emoji que manda un jugador dentro de un globo de dialogo arriba de su cabeza (lo ven todos).
/// El globo y los emojis salen de Assets/Resources/Emojis (globo.png y emoji_[nombre].png).
/// Si llega un nombre sin imagen, se muestra como texto como antes.
/// </summary>
public class PlayerEmojiHandler : NetworkBehaviour
{
    [Header("Referencias Visuales")]
    [SerializeField] private GameObject emojiBubbleObject; // Globo sobre la cabeza
    [SerializeField] private TMP_Text emojiText;           // Respaldo de texto si no hay imagen
    [SerializeField] private float displayDuration = 2.5f;

    [Header("Globo")]
    [SerializeField] private float anchoGlobo = 0.9f;      // ancho del globo en unidades del mundo
    [SerializeField] private float tamanoEmoji = 0.56f;    // ancho del emoji respecto del globo
    [SerializeField] private int ordenDibujo = 1000;       // para que quede por encima de todo
    [SerializeField] private Vector2 posicionGlobo = new Vector2(0.38f, -0.6f); // corrimiento desde EmojiBubble: a la derecha y abajo, para que la colita salga de la cabeza

    private Coroutine currentEmojiCoroutine;
    private Transform bubbleTransform;
    private SpriteRenderer globo, icono;
    private float anchoSpriteGlobo = 1f;

    private void Awake()
    {
        if (emojiBubbleObject != null)
        {
            bubbleTransform = emojiBubbleObject.transform;
            CrearGlobo();
            emojiBubbleObject.SetActive(false);
        }
    }

    private void CrearGlobo()
    {
        Sprite sGlobo = Resources.Load<Sprite>("Emojis/globo");
        if (sGlobo == null) return;

        anchoSpriteGlobo = sGlobo.bounds.size.x;

        GameObject goGlobo = new GameObject("Globo");
        goGlobo.transform.SetParent(bubbleTransform, false);
        goGlobo.transform.localPosition = posicionGlobo;
        goGlobo.transform.localScale = Vector3.one * (anchoGlobo / anchoSpriteGlobo);
        globo = goGlobo.AddComponent<SpriteRenderer>();
        globo.sprite = sGlobo;
        globo.sortingOrder = ordenDibujo;

        GameObject goIcono = new GameObject("Emoji");
        goIcono.transform.SetParent(goGlobo.transform, false);
        // el circulo del globo esta un poco mas arriba que el centro de la imagen (abajo esta la colita)
        goIcono.transform.localPosition = new Vector3(0f, anchoSpriteGlobo * 0.095f, 0f);
        icono = goIcono.AddComponent<SpriteRenderer>();
        icono.sortingOrder = ordenDibujo + 1;
    }

    [Rpc(SendTo.Everyone)]
    public void SendEmojiRpc(string emojiSymbol)
    {
        if (emojiBubbleObject == null) return;

        if (currentEmojiCoroutine != null)
        {
            StopCoroutine(currentEmojiCoroutine);
        }

        currentEmojiCoroutine = StartCoroutine(ShowEmojiRoutine(emojiSymbol));
    }

    private void PrepararContenido(string emoji)
    {
        Sprite s = Resources.Load<Sprite>("Emojis/emoji_" + emoji);
        bool conImagen = s != null && globo != null;

        if (globo != null) globo.gameObject.SetActive(conImagen);
        if (conImagen)
        {
            icono.sprite = s;
            float escala = anchoSpriteGlobo * tamanoEmoji / s.bounds.size.x;
            icono.transform.localScale = Vector3.one * escala;
        }

        if (emojiText != null) emojiText.text = conImagen ? "" : emoji;
    }

    private IEnumerator ShowEmojiRoutine(string emojiSymbol)
    {
        PrepararContenido(emojiSymbol);
        emojiBubbleObject.SetActive(true);

        // 1. Pop-Up de entrada (Escalar de 0 a 1.2 rápido)
        float popupTime = 0.15f;
        float elapsed = 0f;
        while (elapsed < popupTime)
        {
            elapsed += Time.deltaTime;
            float scale = Mathf.Lerp(0f, 1.2f, elapsed / popupTime); // Pequeño sobreimpulso (bouncy effect)
            bubbleTransform.localScale = new Vector3(scale, scale, scale);
            yield return null;
        }

        // Estabilizar a tamaño normal 1.0
        bubbleTransform.localScale = Vector3.one;

        // 2. Mantener visible durante el tiempo configurado
        yield return new WaitForSeconds(displayDuration);

        // 3. Animación de salida (Encoger hasta 0)
        elapsed = 0f;
        while (elapsed < popupTime)
        {
            elapsed += Time.deltaTime;
            float scale = Mathf.Lerp(1f, 0f, elapsed / popupTime);
            bubbleTransform.localScale = new Vector3(scale, scale, scale);
            yield return null;
        }

        emojiBubbleObject.SetActive(false);
    }
}
