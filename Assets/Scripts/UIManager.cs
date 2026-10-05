using UnityEngine;

public class UIManager : MonoBehaviour
{
    [Header("UI References")]
    //[SerializeField] private MainMenuUI mainMenuUI;
    [SerializeField] private PauseMenuUI pauseMenuUI;

    private bool isGamePaused = false;
    private bool isInGameState = false;

    private void OnEnable()
    {
        //if (mainMenuUI != null)
        //{
        //mainMenuUI.OnPlayClicked += HandlePlayClicked;
        //mainMenuUI.OnQuitClicked += HandleQuitClicked;
        //}

        if (pauseMenuUI != null)
        {
            pauseMenuUI.OnResumeClicked += ResumeGame;
            pauseMenuUI.OnQuitToMenuClicked += ReturnToMainMenu;
        }
    }

    private void OnDisable()
    {
        //if (mainMenuUI != null)
        //{
        //mainMenuUI.OnPlayClicked -= HandlePlayClicked;
        // mainMenuUI.OnQuitClicked -= HandleQuitClicked;
        //}

        if (pauseMenuUI != null)
        {
            pauseMenuUI.OnResumeClicked -= ResumeGame;
            pauseMenuUI.OnQuitToMenuClicked -= ReturnToMainMenu;
        }
    }

    private void Start()
    {
        //ReturnToMainMenu();
    }

    private void Update()
    {
        if (isInGameState && Input.GetKeyDown(KeyCode.Escape))
        {
            if (isGamePaused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }
    }


    private void HandlePlayClicked()
    {
        // mainMenuUI.HideAll();

        isInGameState = true;
        isGamePaused = false;
    }

    private void PauseGame()
    {
        isGamePaused = true;
        pauseMenuUI.ShowPauseMenu();
    }

    private void ResumeGame()
    {
        isGamePaused = false;
        pauseMenuUI.HideAll();
    }

    private void ReturnToMainMenu()
    {
        isInGameState = false;
        isGamePaused = false;

        pauseMenuUI.HideAll();
        //mainMenuUI.ShowMainMenu();
    }

    private void HandleQuitClicked()
    {
        Application.Quit();
    }
}