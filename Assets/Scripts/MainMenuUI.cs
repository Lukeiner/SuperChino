using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class MainMenuUI : MonoBehaviour
{
    private VisualElement mainContainer;
    private VisualElement optionsContainer;

    private Button playButton;
    private Button optionsButton;
    private Button quitButton;
    private Button backButton;
    private Slider volumeSlider;

    private void OnEnable()
    {
        VisualElement root = GetComponent<UIDocument>().rootVisualElement;


        mainContainer = root.Q<VisualElement>("MenuContainer");
        optionsContainer = root.Q<VisualElement>("OptionsContainer");


        playButton = root.Q<Button>("playButton");
        optionsButton = root.Q<Button>("settingsButton");
        quitButton = root.Q<Button>("quitButton");
        backButton = root.Q<Button>("backButton");
        volumeSlider = root.Q<Slider>("volumeSlider");


        if (playButton != null) playButton.clicked += PlayGame;
        if (optionsButton != null) optionsButton.clicked += OpenOptions;
        if (quitButton != null) quitButton.clicked += QuitGame;
        if (backButton != null) backButton.clicked += CloseOptions;


        if (volumeSlider != null)
        {

            float savedVolume = PlayerPrefs.GetFloat("GameVolume", 1f);
            volumeSlider.value = savedVolume;
            AudioListener.volume = savedVolume;


            volumeSlider.RegisterValueChangedCallback(evt => SetVolume(evt.newValue));
        }

        ShowMainManu();
    }

    private void OnDisable()
    {
        if (playButton != null) playButton.clicked -= PlayGame;
        if (optionsButton != null) optionsButton.clicked -= OpenOptions;
        if (quitButton != null) quitButton.clicked -= QuitGame;
        if (backButton != null) backButton.clicked -= CloseOptions;
    }

    private void PlayGame()
    {
        SceneManager.LoadScene(1);
    }

    private void OpenOptions()
    {
        if (mainContainer != null) mainContainer.style.display = DisplayStyle.None;
        if (optionsContainer != null) optionsContainer.style.display = DisplayStyle.Flex;
    }

    private void CloseOptions()
    {
        ShowMainManu();
    }

    private void ShowMainManu()
    {
        if (mainContainer != null) mainContainer.style.display = DisplayStyle.Flex;
        if (optionsContainer != null) optionsContainer.style.display = DisplayStyle.None;
    }

    private void SetVolume(float volume)
    {
        AudioListener.volume = volume;
        PlayerPrefs.SetFloat("GameVolume", volume);
    }

    private void QuitGame()
    {
        Debug.Log("Salir del juego");
        Application.Quit();
    }
}