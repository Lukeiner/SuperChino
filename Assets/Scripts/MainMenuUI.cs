using System;
using UnityEngine;
using UnityEngine.UIElements;

public class MainMenuUI : MonoBehaviour
{
    public event Action OnPlayClicked;
    public event Action OnQuitClicked;
    public event Action<float> OnVolumeChanged;

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

        mainContainer = root.Q<VisualElement>("MenuContainer");
        optionsContainer = root.Q<VisualElement>("OptionsContainer");
        creditsContainer = root.Q<VisualElement>("CreditsContainer");

        playButton = root.Q<Button>("playButton");
        optionsButton = root.Q<Button>("settingsButton");
        creditsButton = root.Q<Button>("creditsButton");
        quitButton = root.Q<Button>("quitButton");

        backOptionsButton = root.Q<Button>("backOptionsButton");
        backCreditsButton = root.Q<Button>("backCreditsButton");

        volumeSlider = root.Q<Slider>("volumeSlider");

        if (playButton != null) playButton.clicked += PlayGame;
        if (optionsButton != null) optionsButton.clicked += OpenOptions;
        if (creditsButton != null) creditsButton.clicked += OpenCredits;
        if (quitButton != null) quitButton.clicked += QuitGame;

        if (backOptionsButton != null) backOptionsButton.clicked += CloseSubMenu;
        if (backCreditsButton != null) backCreditsButton.clicked += CloseSubMenu;

        if (volumeSlider != null)
        {
            float savedVolume = PlayerPrefs.GetFloat("GameVolume", 1f);
            volumeSlider.value = savedVolume;

            volumeSlider.RegisterValueChangedCallback(evt =>
            {
                PlayerPrefs.SetFloat("GameVolume", evt.newValue);
                OnVolumeChanged?.Invoke(evt.newValue);
            });
        }

        ShowMainMenu();
    }

    private void OnDisable()
    {
        if (playButton != null) playButton.clicked -= PlayGame;
        if (optionsButton != null) optionsButton.clicked -= OpenOptions;
        if (creditsButton != null) creditsButton.clicked -= OpenCredits;
        if (quitButton != null) quitButton.clicked -= QuitGame;
        if (backOptionsButton != null) backOptionsButton.clicked -= CloseSubMenu;
        if (backCreditsButton != null) backCreditsButton.clicked -= CloseSubMenu;
    }

    private void PlayGame()
    {
        OnPlayClicked?.Invoke();
    }

    private void QuitGame()
    {
        OnQuitClicked?.Invoke();
    }


    private void OpenOptions()
    {
        if (mainContainer != null) mainContainer.style.display = DisplayStyle.None;
        if (optionsContainer != null) optionsContainer.style.display = DisplayStyle.Flex;
        if (creditsContainer != null) creditsContainer.style.display = DisplayStyle.None;
    }

    private void OpenCredits()
    {
        if (mainContainer != null) mainContainer.style.display = DisplayStyle.None;
        if (optionsContainer != null) optionsContainer.style.display = DisplayStyle.None;
        if (creditsContainer != null) creditsContainer.style.display = DisplayStyle.Flex;
    }

    private void CloseSubMenu()
    {
        ShowMainMenu();
    }

    public void ShowMainMenu()
    {
        if (mainContainer != null) mainContainer.style.display = DisplayStyle.Flex;
        if (optionsContainer != null) optionsContainer.style.display = DisplayStyle.None;
        if (creditsContainer != null) creditsContainer.style.display = DisplayStyle.None;
    }

    public void HideAll()
    {
        if (mainContainer != null) mainContainer.style.display = DisplayStyle.None;
        if (optionsContainer != null) optionsContainer.style.display = DisplayStyle.None;
        if (creditsContainer != null) creditsContainer.style.display = DisplayStyle.None;
    }
}