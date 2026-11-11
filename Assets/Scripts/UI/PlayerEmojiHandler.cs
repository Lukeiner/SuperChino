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

        yield return new WaitForSeconds(displayDuration);

        emojiBubbleObject.SetActive(false);
    }
}
