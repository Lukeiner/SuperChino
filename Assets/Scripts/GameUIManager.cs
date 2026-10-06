using UnityEngine;
using UnityEngine.SceneManagement;

public class GameUIManager : MonoBehaviour
{
    [SerializeField] private PauseMenuUI pauseMenuUI;
    private bool isGamePaused = false;

    private void OnEnable()
    {
        if (pauseMenuUI != null)
        {
            pauseMenuUI.OnResumeClicked += ResumeGame;
            pauseMenuUI.OnQuitToMenuClicked += ReturnToMainMenu;
        }
    }

    private void OnDisable()
    {
        if (pauseMenuUI != null)
        {
            pauseMenuUI.OnResumeClicked -= ResumeGame;
            pauseMenuUI.OnQuitToMenuClicked -= ReturnToMainMenu;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isGamePaused)
                ResumeGame();
            else
                PauseGame();
        }
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
        SceneManager.LoadScene(0);
    }
}