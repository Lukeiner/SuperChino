using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

/// menu de pausa (UI Toolkit). Un solo script reemplaza a PauseManager mas PauseMenuUI.
/// es un overlay, no frena la partida los demas jugadores siguen jugando
/// Va en el mismo GameObject que el UIDocument (UIPauseDocument).

[RequireComponent(typeof(UIDocument))]
public class PauseMenuUI : MonoBehaviour
{
    private const string IdCodeScene = "IDCode";
    private const string VolumeKey = "GameVolume";

    // opacidad del fondo oscuro detras del menu
    private const float DimOpacity = 0.6f;

    private VisualElement dimLayer;
    private VisualElement pauseContainer;
    private VisualElement optionsContainer;

    private Button resumeButton;
    private Button optionsButton;
    private Button quitButton;
    private Button backButton;

    private Slider volumeSlider;

    // animaciones del portapapeles
    private PausaAnimaciones animaciones;

    private void OnEnable()
    {
        VisualElement root = GetComponent<UIDocument>().rootVisualElement;

        CreateDimLayer(root);
        animaciones = GetComponent<PausaAnimaciones>();

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

        ShowPanel(null);// al arrancar, todo oculto y sin animación
    }

    private void OnDisable()
    {
        Unhook(resumeButton, HideAll);
        Unhook(optionsButton, OpenOptions);
        Unhook(quitButton, LeaveMatch);
        Unhook(backButton, ShowPauseMenu);

        if (volumeSlider != null)
            volumeSlider.UnregisterValueChangedCallback(OnVolumeChanged);

        // Se quita para que, si el objeto se vuelve a activar, no se agregue una segunda capa.
        dimLayer?.RemoveFromHierarchy();
        dimLayer = null;

        PlayerPrefs.Save();
    }

    private void Update()
    {
        // En Android, botón para abrir y cerrar el menu de pausa
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
        // si hay sesión de red (host o cliente), se cierra antes de salir.
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            NetworkManager.Singleton.Shutdown();

        SceneManager.LoadScene(IdCodeScene);
    }

    private void OnVolumeChanged(ChangeEvent<float> evt)
    {
        PlayerPrefs.SetFloat(VolumeKey, evt.newValue);
        AudioListener.volume = evt.newValue;
    }

    // da vuelta el portapapeles animacion
    private void OpenOptions()
    {
        if (animaciones == null) { ShowPanel(optionsContainer); return; }
        if (animaciones.Ocupado) return;
        animaciones.Girar(() => ShowPanel(optionsContainer));
    }

    // usa el boton de pausa del hud
    public void ShowPauseMenu()
    {
        if (animaciones == null) { ShowPanel(pauseContainer); return; }
        if (animaciones.Ocupado || IsPauseOpen) return;

        if (IsOptionsOpen)
            animaciones.Girar(() => ShowPanel(pauseContainer));
        else
        {
            ShowPanel(pauseContainer);
            animaciones.Entrar();
        }
    }

    public void HideAll()
    {
        bool abierto = IsPauseOpen || IsOptionsOpen;
        if (animaciones == null || !abierto) { ShowPanel(null); return; }
        if (animaciones.Ocupado) return;
        animaciones.Salir(() => ShowPanel(null));
    }

    private void ShowPanel(VisualElement panelToShow)
    {
        SetVisible(pauseContainer, panelToShow == pauseContainer);
        SetVisible(optionsContainer, panelToShow == optionsContainer);

        // el fondo oscuro se ve mientras haya cualquier panel de la pausa abierto.
        SetVisible(dimLayer, panelToShow != null);
    }

    // capa negra semitransparente a pantalla completa, detras de los paneles
    // se crea por código para no tocar PauseMenu.uxml
    // de los menus se puede pasar a un elemento del UXML con su clase en el USS
    private void CreateDimLayer(VisualElement root)
    {
        dimLayer = new VisualElement { name = "PauseDimLayer" };
        dimLayer.style.position = Position.Absolute;
        dimLayer.style.left = 0;
        dimLayer.style.right = 0;
        dimLayer.style.top = 0;
        dimLayer.style.bottom = 0;
        dimLayer.style.backgroundColor = new Color(0f, 0f, 0f, DimOpacity);

        // position: la capa agarra los toques, asi no llegan al HUD de abajo
        // mientras la pausa está abierta.
        dimLayer.pickingMode = PickingMode.Position;

        // indice 0 = al fondo: queda detras de los paneles del UXML
        root.Insert(0, dimLayer);
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