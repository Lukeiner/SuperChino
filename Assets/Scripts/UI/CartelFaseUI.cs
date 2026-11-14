using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Cartel colgante de supermercado que anuncia la fase de la partida.
/// Cuando cambia la fase, el cartel baja desde arriba, se balancea, queda unos segundos y vuelve a subir.
///   Fase de tareas  -> cartel azul    (carrito)
///   Fase de debate  -> cartel rojo    (megafono)
///   Fin de ronda    -> banderin amarillo (ticket)
/// Lee el estado del RoundManager; no hace falta conectarlo a nada mas.
/// Va en el mismo GameObject que el UIDocument del HUD.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class CartelFaseUI : MonoBehaviour
{
    [Header("Tiempos (segundos)")]
    [SerializeField] private float duracionBajada = 0.55f;
    [SerializeField] private float tiempoVisible = 2.2f;
    [SerializeField] private float duracionSubida = 0.4f;

    [Header("Balanceo")]
    [SerializeField] private float anguloBalanceo = 6f;     // grados del primer vaiven
    [SerializeField] private float velocidadBalanceo = 9f;  // que tan rapido va y viene
    [SerializeField] private float frenadoBalanceo = 2.2f;  // que tan rapido se queda quieto

    [Header("Posicion")]
    [SerializeField] private float alturaEscondido = -700f; // px hacia arriba cuando no se ve

    [Header("Pruebas (solo en el editor / development build)")]
    [SerializeField] private bool teclasDePrueba = true;    // F8 tareas, F9 debate, F10 fin de ronda

    private enum Etapa { Escondido, Bajando, Visible, Subiendo }

    private VisualElement cartel;
    private string claseActual;
    private Etapa etapa = Etapa.Escondido;
    private float tiempoEtapa;
    private float tiempoBalanceo;
    private int ultimoEstado = -1;

    private void OnEnable()
    {
        VisualElement root = GetComponent<UIDocument>().rootVisualElement;
        cartel = root.Q<VisualElement>("CartelFase");
        if (cartel == null)
        {
            Debug.LogError("[CartelFaseUI] No se encontro 'CartelFase' en HUD.uxml.", this);
            return;
        }
        etapa = Etapa.Escondido;
        ultimoEstado = -1;
        Aplicar(alturaEscondido, 0f);
    }

    /// <summary>Muestra el cartel de una fase. Se puede llamar desde otro script si hace falta.</summary>
    public void Mostrar(RoundManager.GameState estado)
    {
        string clase = ClaseDe(estado);
        if (cartel == null || clase == null) return;

        if (claseActual != null) cartel.RemoveFromClassList(claseActual);
        cartel.AddToClassList(clase);
        claseActual = clase;

        etapa = Etapa.Bajando;
        tiempoEtapa = 0f;
        tiempoBalanceo = 0f;
    }

    private void Update()
    {
        if (cartel == null) return;

        RevisarFase();
        if (teclasDePrueba && Debug.isDebugBuild) TeclasDePrueba();
        Animar(Time.unscaledDeltaTime);
    }

    private void RevisarFase()
    {
        RoundManager manager = RoundManager.Instance;
        if (manager == null) { ultimoEstado = -1; return; }

        RoundManager.GameState estado = manager.currentGameState.Value;
        if ((int)estado == ultimoEstado) return;

        ultimoEstado = (int)estado;
        Mostrar(estado);
    }

    private void TeclasDePrueba()
    {
        if (Input.GetKeyDown(KeyCode.F8)) Mostrar(RoundManager.GameState.TaskPhase);
        if (Input.GetKeyDown(KeyCode.F9)) Mostrar(RoundManager.GameState.DebatePhase);
        if (Input.GetKeyDown(KeyCode.F10)) Mostrar(RoundManager.GameState.RoundEnd);
    }

    private void Animar(float dt)
    {
        if (etapa == Etapa.Escondido) return;

        tiempoEtapa += dt;
        tiempoBalanceo += dt;

        float y = 0f;
        switch (etapa)
        {
            case Etapa.Bajando:
                float tb = Mathf.Clamp01(tiempoEtapa / duracionBajada);
                y = Mathf.LerpUnclamped(alturaEscondido, 0f, EaseOutBack(tb));
                if (tb >= 1f) Siguiente(Etapa.Visible);
                break;

            case Etapa.Visible:
                if (tiempoEtapa >= tiempoVisible) Siguiente(Etapa.Subiendo);
                break;

            case Etapa.Subiendo:
                float ts = Mathf.Clamp01(tiempoEtapa / duracionSubida);
                y = Mathf.Lerp(0f, alturaEscondido, ts * ts);
                if (ts >= 1f) { etapa = Etapa.Escondido; y = alturaEscondido; }
                break;
        }

        // vaiven que se va frenando, como un cartel colgado de dos cables
        float angulo = anguloBalanceo * Mathf.Exp(-tiempoBalanceo * frenadoBalanceo) * Mathf.Sin(tiempoBalanceo * velocidadBalanceo);
        Aplicar(y, etapa == Etapa.Escondido ? 0f : angulo);
    }

    private void Siguiente(Etapa nueva)
    {
        etapa = nueva;
        tiempoEtapa = 0f;
    }

    private void Aplicar(float y, float angulo)
    {
        cartel.style.translate = new Translate(0, y);
        cartel.style.rotate = new Rotate(angulo);
        cartel.style.visibility = etapa == Etapa.Escondido ? Visibility.Hidden : Visibility.Visible;
    }

    private static string ClaseDe(RoundManager.GameState estado)
    {
        switch (estado)
        {
            case RoundManager.GameState.TaskPhase: return "cartel-tareas";
            case RoundManager.GameState.DebatePhase: return "cartel-debate";
            case RoundManager.GameState.RoundEnd: return "cartel-fin-ronda";
            default: return null; // esperando y fin de partida no tienen cartel (el fin usa el diario)
        }
    }

    private static float EaseOutBack(float t)
    {
        const float c1 = 1.4f, c3 = c1 + 1f;
        float u = t - 1f;
        return 1f + c3 * u * u * u + c1 * u * u;
    }
}
