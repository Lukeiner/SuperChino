using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [Header("UI Host")]
    [SerializeField] private Button createHostButton;
    [SerializeField] private TMP_Text codeDisplayText; 

    [Header("UI Client")]
    [SerializeField] private TMP_InputField joinCodeInput;
    [SerializeField] private Button joinClientButton;

    [Header("Paneles (Opcional)")]
    [SerializeField] private GameObject lobbyPanel;

    private void Start()
    {
        // Enganchar eventos a los botones
        if (createHostButton != null)
            createHostButton.onClick.AddListener(OnCreateHostClicked);

        if (joinClientButton != null)
            joinClientButton.onClick.AddListener(OnJoinClientClicked);
    }
    private async void OnCreateHostClicked()
    {
        if (codeDisplayText != null) codeDisplayText.text = "Creando sala...";

        // 1. Llamar a RelayManager para crear la sala y obtener el código
        string code = await RelayManager.Instance.CreateRelay(4);

        if (!string.IsNullOrEmpty(code))
        {
            // 2. Mostrar el Join Code en pantalla para que se lo pase al compañero
            if (codeDisplayText != null)
            {
                codeDisplayText.text = $"CÓDIGO DE SALA:\n<color=yellow>{code}</color>";
            }

            // Ocultar menú de conexión si lo desean
            if (lobbyPanel != null) lobbyPanel.SetActive(false);
        }
        else
        {
            if (codeDisplayText != null) codeDisplayText.text = "Error al crear la sala.";
        }
    }

    private async void OnJoinClientClicked()
    {
        if (joinCodeInput == null || string.IsNullOrEmpty(joinCodeInput.text))
        {
            Debug.LogWarning("[UI]: Ingresá un código válido en el campo de texto.");
            return;
        }

        string codeToJoin = joinCodeInput.text.Trim();

        // 1. Intentar unirse a la sala con el código tipeado
        bool success = await RelayManager.Instance.JoinRelay(codeToJoin);

        if (success)
        {
            Debug.Log("[UI]: Conectado a la sala con éxito.");
            if (lobbyPanel != null) lobbyPanel.SetActive(false);
        }
        else
        {
            Debug.LogError("[UI]: Código inválido o falló la conexión.");
        }
    }
}
