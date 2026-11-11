using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class EmojiWheelUI : MonoBehaviour
{
    [Header("Paneles de UI")]
    [SerializeField] private GameObject wheelPanel;

    private void Awake()
    {
        // Si no se asignó en el Inspector, usa este mismo GameObject o busca el hijo
        if (wheelPanel == null)
        {
            wheelPanel = gameObject;
        }

        // Arranca oculta la ruleta
        wheelPanel.SetActive(false);
    }

    public void ToggleWheel()
    {
        if (wheelPanel != null)
        {
            bool nextState = !wheelPanel.activeSelf;
            wheelPanel.SetActive(nextState);
            Debug.Log($"<color=cyan>[RUEDA UI]:</color> Visibilidad cambiada a: {nextState}");
        }
    }

    public void HideWheel()
    {
        if (wheelPanel != null)
            wheelPanel.SetActive(false);
    }

    // Método que llaman los botones de la ruleta al tocar un emoji
    public void SelectEmoji(string emojiSymbol)
    {
        Debug.Log($"<color=green>[EMOJI SELECCIONADO]:</color> Presionaste {emojiSymbol}");

        // Ocultamos la ruleta inmediatamente
        HideWheel();

        // Buscamos al jugador local en la red Netcode
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.LocalClient != null)
        {
            var localPlayerObject = NetworkManager.Singleton.LocalClient.PlayerObject;
            if (localPlayerObject != null)
            {
                var emojiHandler = localPlayerObject.GetComponent<PlayerEmojiHandler>();
                if (emojiHandler != null)
                {
                    // Transmitimos el emoji por red a todas las pantallas
                    emojiHandler.SendEmojiRpc(emojiSymbol);
                }
                else
                {
                    Debug.LogError("<color=red>[EMOJI ERROR]:</color> El PlayerPrefab no tiene pegado el script PlayerEmojiHandler.");
                }
            }
            else
            {
                Debug.LogWarning("<color=orange>[EMOJI WARN]:</color> No se encontró el LocalPlayerObject. ¿Estás ejecutando como Host/Client?");
            }
        }
    }
}
