using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

/// <summary>
/// Pantalla de victoria / derrota con forma de portada de diario ("EL DIARIO"). La portada es una imagen por resultado.
/// Va en el mismo GameObject que un UIDocument con Diario.uxml. Arranca oculta.
///
/// COMO CONECTARLA al fin de la partida (cuando exista la ronda de debate):
///     DiarioUI.Instance.Mostrar(true);   // el super se salvo (ganan los empleados)
///     DiarioUI.Instance.Mostrar(false);  // el super fue saqueado (gana el ladron)
/// Mostrar(...) se llama en cada cliente (por ejemplo desde NotifyGameEndClientRpc en GameManager).
///
/// PARA PROBAR sin partida: en el Editor, con el juego en Play, tecla F5 = victoria, F6 = derrota, F7 = ocultar.
/// Tambien se puede usar el menu contextual del componente (los tres "Probar ...").
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class DiarioUI : MonoBehaviour
{
    public static DiarioUI Instance { get; private set; }

    /// <summary>Se dispara cuando el jugador toca VOLVER AL MENU (antes de salir de la escena).</summary>
    public event Action OnVolverAlMenu;

    private const string IdCodeScene = "IDCode";

    // tiempos de la animacion (segundos)
    private const float DuracionEntrada = 0.9f;
    private const float RetrasoBoton = 1.1f;
    private const float DuracionBoton = 0.35f;
    private const float VueltasEntrada = 1.5f;   // cuantas vueltas da la hoja al entrar
    private const float EscalaInicial = 0.05f;

    // grados finales de inclinacion de la hoja
    private const float InclinacionVictoria = 1.6f;
    private const float InclinacionDerrota = -1.6f;

    private VisualElement root, hoja;
    private Button botonVolver;

    // estado de la animacion
    private bool animando;
    private float tiempo;
    private float inclinacionFinal;
    private bool esperandoBoton;

    private void Awake()
    {
        Instance = this;
    }

    private void OnEnable()
    {
        Instance = this;
        VisualElement doc = GetComponent<UIDocument>().rootVisualElement;

        root = Find<VisualElement>(doc, "DiarioRoot");
        hoja = Find<VisualElement>(doc, "Hoja");
        botonVolver = Find<Button>(doc, "BotonVolver");

        if (botonVolver != null) botonVolver.clicked += VolverAlMenu;

        Ocultar();
    }

    private void OnDisable()
    {
        if (botonVolver != null) botonVolver.clicked -= VolverAlMenu;
        if (Instance == this) Instance = null;
    }

    /// <param name="victoria">true = ganan los empleados (El super esta a salvo); false = gana el ladron (Super saqueado).</param>
    public void Mostrar(bool victoria)
    {
        if (root == null) return;

        // la imagen del diario cambia por clase de USS (Diario.uss)
        hoja.EnableInClassList("hoja-victoria", victoria);
        hoja.EnableInClassList("hoja-derrota", !victoria);

        inclinacionFinal = victoria ? InclinacionVictoria : InclinacionDerrota;
        root.style.display = DisplayStyle.Flex;
        root.style.opacity = 0f;
        botonVolver.style.opacity = 0f;
        botonVolver.SetEnabled(false);

        tiempo = 0f;
        animando = true;
        esperandoBoton = true;
        AplicarPose(0f);
    }

    public void Ocultar()
    {
        animando = false;
        if (root != null)
        {
            root.style.display = DisplayStyle.None;
            root.style.opacity = 0f;
        }
    }

    private void Update()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (Input.GetKeyDown(KeyCode.F5)) Mostrar(true);
        if (Input.GetKeyDown(KeyCode.F6)) Mostrar(false);
        if (Input.GetKeyDown(KeyCode.F7)) Ocultar();
#endif
        if (!animando) return;

        tiempo += Time.unscaledDeltaTime;

        float p = Mathf.Clamp01(tiempo / DuracionEntrada);
        AplicarPose(p);

        if (esperandoBoton && tiempo >= RetrasoBoton)
        {
            float b = Mathf.Clamp01((tiempo - RetrasoBoton) / DuracionBoton);
            botonVolver.style.opacity = b;
            if (b >= 1f)
            {
                botonVolver.SetEnabled(true);
                esperandoBoton = false;
            }
        }

        if (p >= 1f && !esperandoBoton) animando = false;
    }

    // p = 0..1. La hoja entra girando y agrandandose (efecto de diario viejo) y se asienta con un pequeno rebote.
    private void AplicarPose(float p)
    {
        root.style.opacity = Mathf.Clamp01(p * 4f);

        float giro = (1f - EaseOutCubic(p)) * 360f * VueltasEntrada;
        float escala = Mathf.LerpUnclamped(EscalaInicial, 1f, EaseOutBack(p));

        hoja.style.rotate = new Rotate(new Angle(inclinacionFinal + giro, AngleUnit.Degree));
        hoja.style.scale = new Scale(new Vector2(escala, escala));
    }

    private static float EaseOutCubic(float x) { x = 1f - x; return 1f - x * x * x; }

    private static float EaseOutBack(float x)
    {
        const float c1 = 1.20158f, c3 = c1 + 1f;
        float y = x - 1f;
        return 1f + c3 * y * y * y + c1 * y * y;
    }

    private void VolverAlMenu()
    {
        OnVolverAlMenu?.Invoke();

        // igual que en el menu de pausa: se cierra la sesion de red antes de salir
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            NetworkManager.Singleton.Shutdown();

        SceneManager.LoadScene(IdCodeScene);
    }

    [ContextMenu("Probar victoria")] private void ProbarVictoria() { Mostrar(true); }
    [ContextMenu("Probar derrota")] private void ProbarDerrota() { Mostrar(false); }
    [ContextMenu("Ocultar")] private void ProbarOcultar() { Ocultar(); }

    private T Find<T>(VisualElement r, string elementName) where T : VisualElement
    {
        T element = r.Q<T>(elementName);
        if (element == null)
            Debug.LogError($"[DiarioUI] No se encontro '{elementName}' en el UXML. Revisa el nombre.", this);
        return element;
    }
}
