using System;
using UnityEngine;
using UnityEngine.UIElements;

public class PauseMenuUI : MonoBehaviour
{
    public event Action OnResumeClicked;
    public event Action OnQuitToMenuClicked;
    public event Action<float> OnVolumeChanged;

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

        pauseContainer = root.Q<VisualElement>("PauseContainer");
        optionsContainer = root.Q<VisualElement>("PauseOptionsContainer");

        resumeButton = root.Q<Button>("resumeButton");
        optionsButton = root.Q<Button>("optionsButton");
        quitButton = root.Q<Button>("quitButton");
        backButton = root.Q<Button>("backButton");
        volumeSlider = root.Q<Slider>("volumeSlider");

        if (resumeButton != null) resumeButton.clicked += Resume;
        if (optionsButton != null) optionsButton.clicked += OpenOptions;
        if (quitButton != null) quitButton.clicked += QuitToMenu;
        if (backButton != null) backButton.clicked += CloseOptions;

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

        HideAll();
    }

    private void OnDisable()
    {
        if (resumeButton != null) resumeButton.clicked -= Resume;
        if (optionsButton != null) optionsButton.clicked -= OpenOptions;
        if (quitButton != null) quitButton.clicked -= QuitToMenu;
        if (backButton != null) backButton.clicked -= CloseOptions;
    }

    private void Resume()
    {
        OnResumeClicked?.Invoke();
    }

    private void QuitToMenu()
    {
        OnQuitToMenuClicked?.Invoke();
    }

    private void OpenOptions()
    {
        if (pauseContainer != null) pauseContainer.style.display = DisplayStyle.None;
        if (optionsContainer != null) optionsContainer.style.display = DisplayStyle.Flex;
    }

    private void CloseOptions()
    {
        ShowPauseMenu();
    }

    public void ShowPauseMenu()
    {
        if (pauseContainer != null) pauseContainer.style.display = DisplayStyle.Flex;
        if (optionsContainer != null) optionsContainer.style.display = DisplayStyle.None;
    }

    public void HideAll()
    {
        if (pauseContainer != null) pauseContainer.style.display = DisplayStyle.None;
        if (optionsContainer != null) optionsContainer.style.display = DisplayStyle.None;
    }
}