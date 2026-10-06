using UnityEngine;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private MainMenuUI mainMenuUI;

    private void OnEnable()
    {
        if (mainMenuUI != null)
        {
            mainMenuUI.OnPlayClicked += HandlePlayClicked;
            mainMenuUI.OnQuitClicked += HandleQuitClicked;
        }
    }

    private void OnDisable()
    {
        if (mainMenuUI != null)
        {
            mainMenuUI.OnPlayClicked -= HandlePlayClicked;
            mainMenuUI.OnQuitClicked -= HandleQuitClicked;
        }
    }

    private void HandlePlayClicked()
    {

        SceneManager.LoadScene(1);
    }

    private void HandleQuitClicked()
    {
        Application.Quit();
    }
}