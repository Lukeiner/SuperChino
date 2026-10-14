using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class PauseMenuUI : MonoBehaviour
{
    private const string IdCodeScene = "IDCode";
    private const string VolumeKey = "GameVolume";

    private VisualElement pauseContainer;
    private VisualElement optionsContainer;

    private Button resumeButton;
    private Button optionsButton;
    private Button quitButton;
    private Button backButton;

    private Slider volumeSlider;

    private void OnEnable()
    {
        VisualElement root = GetComponent<UIDocument>().rootVisualElement;

        pauseContainer = Find<VisualElement>(root, "PauseContainer");
        optionsContainer = Find<VisualElement>(root, "PauseOptionsContainer");

        resumeButton = Find<Button>(root, "resumeButton");
        optionsButton = Find<Button>(root, "optionsButton");
        quitButton = Find<Button>(root, "quitButton");
        backButton = Find<Button>(root, "backButton");

        volumeSlider = Find<Slider>(root, "volumeSlider");

        Hook(resumeButton, HideAll);
        Hook(optionsButton, OpenOptions);
        Hook(quitButton, LeaveMatch);
        Hook(backButton, ShowPauseMenu);

        if (volumeSlider != null)
        {
            volumeSlider.lowValue = 0f;
            volumeSlider.highValue = 1f;

            float saved = PlayerPrefs.GetFloat(VolumeKey, 1f);
            volumeSlider.SetValueWithoutNotify(saved);

            volumeSlider.RegisterValueChangedCallback(OnVolumeChanged);
        }

        HideAll();
    }

    private void OnDisable()
    {
        Unhook(resumeButton, HideAll);
        Unhook(optionsButton, OpenOptions);
        Unhook(quitButton, LeaveMatch);
        Unhook(backButton, ShowPauseMenu);

        if (volumeSlider != null)
            volumeSlider.UnregisterValueChangedCallback(OnVolumeChanged);

        PlayerPrefs.Save();
    }

    private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Escape)) return;

        if (IsOptionsOpen)
            ShowPauseMenu();
        else if (IsPauseOpen)
            HideAll();
        else
            ShowPauseMenu();
    }

    private bool IsPauseOpen => IsShown(pauseContainer);
    private bool IsOptionsOpen => IsShown(optionsContainer);

    private static bool IsShown(VisualElement element)
        => element != null && element.style.display == DisplayStyle.Flex;

    private void LeaveMatch()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            NetworkManager.Singleton.Shutdown();

        SceneManager.LoadScene(IdCodeScene);
    }

    private void OnVolumeChanged(ChangeEvent<float> evt)
    {
        PlayerPrefs.SetFloat(VolumeKey, evt.newValue);
        AudioListener.volume = evt.newValue;
    }

    private void OpenOptions() => ShowPanel(optionsContainer);

    public void ShowPauseMenu() => ShowPanel(pauseContainer);
    public void HideAll() => ShowPanel(null);

    private void ShowPanel(VisualElement panelToShow)
    {
        SetVisible(pauseContainer, panelToShow == pauseContainer);
        SetVisible(optionsContainer, panelToShow == optionsContainer);
    }

    private static void SetVisible(VisualElement element, bool visible)
    {
        if (element != null)
            element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private T Find<T>(VisualElement root, string elementName) where T : VisualElement
    {
        T element = root.Q<T>(elementName);
        if (element == null)
            Debug.LogError($"[PauseMenuUI] No se encontró '{elementName}' en el UXML. Revisa el nombre.", this);
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