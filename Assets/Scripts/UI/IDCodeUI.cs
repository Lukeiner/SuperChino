using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class IDCodeUI : MonoBehaviour
{
    private const string MainMenuScene = "MainMenu";

    private Button hostButton;
    private Button joinButton;
    private Button backButton;
    private TextField joinCodeInput;
    private Label codeDisplayText;
    private Label statusLabel;

    private void OnEnable()
    {
        VisualElement root = GetComponent<UIDocument>().rootVisualElement;

        hostButton = Find<Button>(root, "hostButton");
        joinButton = Find<Button>(root, "joinButton");
        backButton = Find<Button>(root, "backButton");
        joinCodeInput = Find<TextField>(root, "joinCodeField");
        codeDisplayText = Find<Label>(root, "codeLabel");
        statusLabel = Find<Label>(root, "statusLabel");

        Hook(hostButton, OnCreateHostClicked);
        Hook(joinButton, OnJoinClientClicked);
        Hook(backButton, OnBackClicked);

        SetText(codeDisplayText, "");
        SetText(statusLabel, "");
    }

    private void OnDisable()
    {
        Unhook(hostButton, OnCreateHostClicked);
        Unhook(joinButton, OnJoinClientClicked);
        Unhook(backButton, OnBackClicked);
    }


    private async void OnCreateHostClicked()
    {
        SetText(codeDisplayText, "");
        SetText(statusLabel, "Generando código...");

        string joinCode = await RelayManager.Instance.CreateRelay(4);

        if (!string.IsNullOrEmpty(joinCode))
        {
            SetText(codeDisplayText, joinCode);
            SetText(statusLabel, "Comparte el código. La partida empieza en 10 segundos...");

            await Task.Delay(10000);

            if (this == null) return;

            NetworkManager.Singleton.SceneManager.LoadScene("SampleScene", LoadSceneMode.Single);
        }
        else
        {
            SetText(statusLabel, "Error al crear la sala.");
        }
    }

    private async void OnJoinClientClicked()
    {
        string codeToJoin = (joinCodeInput?.value ?? "").Trim();

        if (string.IsNullOrEmpty(codeToJoin))
        {
            Debug.LogWarning("[UI]: Por favor, ingresa un código válido en la casilla.");
            SetText(statusLabel, "Ingresa un código válido.");
            return;
        }

        bool success = await RelayManager.Instance.JoinRelay(codeToJoin);

        if (success)
        {
            Debug.Log("[UI]: Unido con éxito a la sala.");
            SetText(statusLabel, "Unido con éxito. Esperando que empiece la partida...");
        }
        else
        {
            Debug.LogError("[UI]: El código ingresado es incorrecto o la sala no existe.");
            SetText(statusLabel, "El código es incorrecto o la sala no existe.");
        }
    }


    private void OnBackClicked()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            NetworkManager.Singleton.Shutdown();

        SceneManager.LoadScene(MainMenuScene);
    }


    private static void SetText(Label label, string text)
    {
        if (label != null) label.text = text;
    }

    private T Find<T>(VisualElement root, string elementName) where T : VisualElement
    {
        T element = root.Q<T>(elementName);
        if (element == null)
            Debug.LogError($"[IDCodeUI] No se encontró '{elementName}' en el UXML. Revisa el nombre.", this);
        return element;
    }

    private static void Hook(Button button, System.Action action)
    {
        if (button != null) button.clicked += action;
    }

    private static void Unhook(Button button, System.Action action)
    {
        if (button != null) button.clicked -= action;
    }
}
