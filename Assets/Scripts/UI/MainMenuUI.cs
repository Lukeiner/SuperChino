using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

/// <summary>
/// Menú de inicio (UI Toolkit).
/// Un solo script: busca los elementos del UXML, reacciona a los botones
/// y ejecuta la acción (cambiar de escena, salir, volumen).
/// Va en el mismo GameObject que el UIDocument.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class MainMenuUI : MonoBehaviour
{
    // La escena debe estar en Build Profiles con este nombre exacto.
    private const string IdCodeScene = "IDCode";
    private const string VolumeKey = "GameVolume";

    private VisualElement mainContainer;
    private VisualElement optionsContainer;
    private VisualElement creditsContainer;

    private Button playButton;
    private Button optionsButton;
    private Button creditsButton;
    private Button quitButton;
    private Button backOptionsButton;
    private Button backCreditsButton;

    private Slider volumeSlider;

    private void OnEnable()
    {
        VisualElement root = GetComponent<UIDocument>().rootVisualElement;

        // Los nombres deben coincidir con el campo "name" de cada elemento en el UXML.
        mainContainer = Find<VisualElement>(root, "MenuContainer");
        optionsContainer = Find<VisualElement>(root, "OptionsContainer");
        creditsContainer = Find<VisualElement>(root, "CreditsContainer");

        playButton = Find<Button>(root, "playButton");
        optionsButton = Find<Button>(root, "settingsButton");
        creditsButton = Find<Button>(root, "creditsButton");
        quitButton = Find<Button>(root, "quitButton");
        backOptionsButton = Find<Button>(root, "backOptionsButton");
        backCreditsButton = Find<Button>(root, "backCreditsButton");

        volumeSlider = Find<Slider>(root, "volumeSlider");

        Hook(playButton, PlayGame);
        Hook(optionsButton, OpenOptions);
        Hook(creditsButton, OpenCredits);
        Hook(quitButton, QuitGame);
        Hook(backOptionsButton, ShowMainMenu);
        Hook(backCreditsButton, ShowMainMenu);

        if (volumeSlider != null)
        {
            // AudioListener.volume va de 0 a 1, así que fijamos el rango del slider igual.
            volumeSlider.lowValue = 0f;
            volumeSlider.highValue = 1f;

            float saved = PlayerPrefs.GetFloat(VolumeKey, 1f);
            volumeSlider.SetValueWithoutNotify(saved);
            AudioListener.volume = saved;

            volumeSlider.RegisterValueChangedCallback(OnVolumeChanged);
        }

        ShowMainMenu();
    }

    private void OnDisable()
    {
        Unhook(playButton, PlayGame);
        Unhook(optionsButton, OpenOptions);
        Unhook(creditsButton, OpenCredits);
        Unhook(quitButton, QuitGame);
        Unhook(backOptionsButton, ShowMainMenu);
        Unhook(backCreditsButton, ShowMainMenu);

        if (volumeSlider != null)
            volumeSlider.UnregisterValueChangedCallback(OnVolumeChanged);

        PlayerPrefs.Save();
    }

    // ---------- Acciones ----------

    private void PlayGame()
    {
        // Todavía no hay sesión de red, así que se usa el SceneManager normal.
        SceneManager.LoadScene(IdCodeScene);
    }

    private void QuitGame()
    {
#if UNITY_EDITOR
        // Application.Quit() no hace nada dentro del editor.
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void OnVolumeChanged(ChangeEvent<float> evt)
    {
        PlayerPrefs.SetFloat(VolumeKey, evt.newValue);
        AudioListener.volume = evt.newValue;
    }

    // ---------- Paneles ----------

    private void OpenOptions() => ShowPanel(optionsContainer);
    private void OpenCredits() => ShowPanel(creditsContainer);

    public void ShowMainMenu() => ShowPanel(mainContainer);
    public void HideAll() => ShowPanel(null);

    private void ShowPanel(VisualElement panelToShow)
    {
        SetVisible(mainContainer, panelToShow == mainContainer);
        SetVisible(optionsContainer, panelToShow == optionsContainer);
        SetVisible(creditsContainer, panelToShow == creditsContainer);
    }

    private static void SetVisible(VisualElement element, bool visible)
    {
        if (element != null)
            element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
    }

    // ---------- Utilidades ----------

    // Si un nombre no coincide con el UXML, avisa en la Console en lugar de fallar en silencio.
    private T Find<T>(VisualElement root, string elementName) where T : VisualElement
    {
        T element = root.Q<T>(elementName);
        if (element == null)
            Debug.LogError($"[MainMenuUI] No se encontró '{elementName}' en el UXML. Revisa el nombre.", this);
        return element;
    }

    private static void Hook(Button button, Action action)
    {
        if (button != null) button.clicked += action;
    }

    private static void Unhook(Button button, Action action)
    {
        if (button != null) button.clicked -= action;
    }
}