using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;

public class PauseManager : MonoBehaviour
{
    public PauseMenuUI menuPausaReferencia;

    private void Start()
    {
        if (menuPausaReferencia != null)
        {
            menuPausaReferencia.OnResumeClicked += ReanudarJuego;
            menuPausaReferencia.OnQuitToMenuClicked += VolverAlMenuPrincipal;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (menuPausaReferencia != null)
            {
                menuPausaReferencia.ShowPauseMenu();
            }
        }
    }

    private void ReanudarJuego()
    {
        if (menuPausaReferencia != null)
        {
            menuPausaReferencia.HideAll();
        }
    }

    private void VolverAlMenuPrincipal()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.Shutdown();
        }

        SceneManager.LoadScene("MainMenu");
    }

    private void OnDestroy()
    {
        if (menuPausaReferencia != null)
        {
            menuPausaReferencia.OnResumeClicked -= ReanudarJuego;
            menuPausaReferencia.OnQuitToMenuClicked -= VolverAlMenuPrincipal;
        }
    }
}