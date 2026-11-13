using System.Collections;
using Unity.Netcode;
using TMPro;
using UnityEngine;

public class PlayerEmojiHandler : NetworkBehaviour
{
    [Header("Referencias Visuales")]
    [SerializeField] private GameObject emojiBubbleObject; // Bocadillos/Globo sobre la cabeza
    [SerializeField] private TMP_Text emojiText;           // Texto que mostrará el Emoji (Unicode)
    [SerializeField] private float displayDuration = 2.5f;

    private Coroutine currentEmojiCoroutine;
    private Transform bubbleTransform;

    private void Awake()
    {
        if (emojiBubbleObject != null)
        {
            bubbleTransform = emojiBubbleObject.transform;
            emojiBubbleObject.SetActive(false);
        }
    }

    [Rpc(SendTo.Everyone)]
    public void SendEmojiRpc(string emojiSymbol)
    {
        if (emojiBubbleObject == null || emojiText == null) return;

        if (currentEmojiCoroutine != null)
        {
            StopCoroutine(currentEmojiCoroutine);
        }

        currentEmojiCoroutine = StartCoroutine(ShowEmojiRoutine(emojiSymbol));
    }

    private IEnumerator ShowEmojiRoutine(string emojiSymbol)
    {
        emojiText.text = emojiSymbol;
        emojiBubbleObject.SetActive(true);

        // 1. Pop-Up de entrada (Escalar de 0 a 1 rápido)
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
