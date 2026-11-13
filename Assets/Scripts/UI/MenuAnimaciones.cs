using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

//Anima el menu principal. Va en el MISMO objeto que el UIDocument y el MainMenuUI.
//MainMenuUI maneja los botones; este script mueve cosas y hace las transiciones:
//radio pasa a Opciones, TV pasa a Creditos, puerta pasa a Jugar.
[RequireComponent(typeof(UIDocument))]
public class MenuAnimaciones : MonoBehaviour
{
    [Header("Balanceo (grados y segundos por ida y vuelta)")]
    [SerializeField] float anguloLinternas = 4f;
    [SerializeField] float periodoLinternas = 3.2f;
    [SerializeField] float anguloCartel = 2.2f;
    [SerializeField] float periodoCartel = 4.2f;

    [Header("Transiciones (zoom hacia un objeto)")]
    [SerializeField] float zoomRadio = 3.2f;
    [SerializeField] float zoomTV = 3.4f;
    [SerializeField] float zoomPuerta = 3.0f;
    [SerializeField] float duracionZoom = 0.7f;// segundos del acercamiento
    [SerializeField] float duracionFundido = 0.3f;// segundos que tarda en aparecer y desaparecer la pantalla nueva

    [Header("Creditos")]
    [SerializeField] float velocidadCreditos = 55f;// que tan rapido sube el texto

    const float AnchoFondo = 2576f, AltoFondo = 1438f;
    static readonly Vector2 CentroFondo = new Vector2(AnchoFondo / 2f, AltoFondo / 2f);
    // Puntos donde hace zoom (en pixeles del fondo)
    static readonly Vector2 CentroRadio  = new Vector2(750f, 1218f);
    static readonly Vector2 CentroTV     = new Vector2(1754f, 830f);
    static readonly Vector2 CentroPuerta = new Vector2(1255f, 960f);

    static readonly Color VerdeRadio = new Color(15 / 255f, 31 / 255f, 27 / 255f);
    static readonly Color NegroEspacio = new Color(6 / 255f, 5 / 255f, 12 / 255f);

    VisualElement root, contenedor, escenario, fundido;
    VisualElement linternaIzq, linternaDer, cartel, gato, tvLuz, radioLuz;
    float proximoParpadeo;

    // camara del menu
    float zoomT;// 0 = vista normal, 1 = pegado al objeto
    Vector2 focoDestino = CentroRadio;
    float zoomDestino = 3f;
    bool enTransicion;
    public bool EnTransicion => enTransicion;

    // opciones
    VisualElement opciones, radioEscenario, radioBrillo, rellenoVolumen;
    Slider volumen; Label porcentaje;

    // creditos
    VisualElement creditos, textoCreditos;
    Label introCreditos, logoCreditos;
    readonly List<VisualElement> lineas = new List<VisualElement>();
    float tiempoCreditos;

    void OnEnable()
    {
        root = GetComponent<UIDocument>().rootVisualElement;
        contenedor  = root.Q("MenuContainer");
        escenario   = root.Q("Escenario");
        fundido     = root.Q("Fundido");
        linternaIzq = root.Q("LinternaIzq");
        linternaDer = root.Q("LinternaDer");
        cartel      = root.Q("Cartel");
        gato        = root.Q("Gato");
        tvLuz       = root.Q("TVLuz");
        radioLuz    = root.Q("RadioLuz");

        opciones       = root.Q("OptionsContainer");
        radioEscenario = root.Q("RadioEscenario");
        radioBrillo    = root.Q("RadioBrillo");
        volumen        = root.Q<Slider>("volumeSlider");
        porcentaje     = root.Q<Label>("VolumenPorcentaje");

        creditos      = root.Q("CreditsContainer");
        textoCreditos = root.Q("CreditosTexto");
        introCreditos = root.Q<Label>("CreditosIntro");
        logoCreditos  = root.Q<Label>("CreditosLogo");
        lineas.Clear();
        if (textoCreditos != null) lineas.AddRange(textoCreditos.Children());

        // Relleno verde de la barra de volumen
        var tracker = volumen?.Q(className: "unity-base-slider__tracker");
        if (tracker != null && rellenoVolumen == null)
        {
            rellenoVolumen = new VisualElement { pickingMode = PickingMode.Ignore };
            rellenoVolumen.AddToClassList("lcd-relleno");
            tracker.Add(rellenoVolumen);
        }

        zoomT = 0f; enTransicion = false;
        if (fundido != null) fundido.style.opacity = 0f;
        contenedor?.RegisterCallback<GeometryChangedEvent>(_ => AplicarCamara());
    }
    void AplicarCamara()
    {
        if (contenedor == null || escenario == null) return;
        float w = contenedor.resolvedStyle.width, h = contenedor.resolvedStyle.height;
        if (float.IsNaN(w) || w <= 0 || h <= 0) return;

        float e = Suave(zoomT);
        float escala = EscalaCubrir(w, h) * Mathf.Lerp(1f, zoomDestino, e);
        Vector2 foco = Vector2.Lerp(CentroFondo, focoDestino, e);
        // pantalla = traslado + escala * puntoDelFondo   (el foco queda en el centro de la pantalla)
        Vector2 traslado = new Vector2(w / 2f, h / 2f) - escala * foco;
        escenario.style.scale = new Scale(new Vector3(escala, escala, 1f));
        escenario.style.translate = new Translate(traslado.x, traslado.y);
    }

    static float EscalaCubrir(float w, float h) => Mathf.Max(w / AnchoFondo, h / AltoFondo);

    // Escala y centra un "escenario" de 2576x1438 para que cubra su contenedor (lo usa la radio de Opciones)
    static void Cubrir(VisualElement padre, VisualElement hijo)
    {
        if (padre == null || hijo == null) return;
        float w = padre.resolvedStyle.width, h = padre.resolvedStyle.height;
        if (float.IsNaN(w) || w <= 0 || h <= 0) return;
        float s = EscalaCubrir(w, h);
        hijo.style.scale = new Scale(new Vector3(s, s, 1f));
        hijo.style.translate = new Translate((w - AnchoFondo * s) / 2f, (h - AltoFondo * s) / 2f);
    }

    static float Suave(float t) => t * t * (3f - 2f * t);   // arranca y frena suave
    public void EntrarARadio(Action mostrarOpciones) =>
        Entrar(CentroRadio, zoomRadio, VerdeRadio, opciones, mostrarOpciones);
    public void SalirDeRadio(Action mostrarMenu) =>
        Salir(CentroRadio, zoomRadio, VerdeRadio, opciones, mostrarMenu);

    public void EntrarATV(Action mostrarCreditos) =>
        Entrar(CentroTV, zoomTV, NegroEspacio, creditos, () => { tiempoCreditos = 0f; mostrarCreditos?.Invoke(); });
    public void SalirDeTV(Action mostrarMenu) =>
        Salir(CentroTV, zoomTV, NegroEspacio, creditos, mostrarMenu);

    // Puerta: zoom y transicion a negro, despues carga la escena
    public void EntrarAPuerta(Action cargarEscena) =>
        Entrar(CentroPuerta, zoomPuerta, Color.black, null, cargarEscena);

    void Entrar(Vector2 foco, float zoom, Color color, VisualElement pantalla, Action mostrar)
    {
        if (!enTransicion) StartCoroutine(EntrarRutina(foco, zoom, color, pantalla, mostrar));
    }

    void Salir(Vector2 foco, float zoom, Color color, VisualElement pantalla, Action mostrarMenu)
    {
        if (!enTransicion) StartCoroutine(SalirRutina(foco, zoom, color, pantalla, mostrarMenu));
    }

    IEnumerator EntrarRutina(Vector2 foco, float zoom, Color color, VisualElement pantalla, Action mostrar)
    {
        enTransicion = true;
        focoDestino = foco; zoomDestino = zoom;
        SetColorFundido(color);
        // zoom hacia el objeto; en el ultimo tramo todo se tapa con el color
        for (float t = 0; t < duracionZoom; t += Time.deltaTime)
        {
            zoomT = t / duracionZoom;
            SetFundido(Mathf.InverseLerp(0.55f, 1f, zoomT));
            AplicarCamara();
            yield return null;
        }
        zoomT = 1f; SetFundido(1f); AplicarCamara();

        // cambiamos de pantalla; la nueva aparece desde ese color
        if (pantalla != null) pantalla.style.opacity = 0f;
        mostrar?.Invoke();
        if (pantalla == null) yield break; // puerta: la escena nueva se encarga
        yield return Fundir(pantalla, 0f, 1f, 1.06f, 1f);
        enTransicion = false;
    }

    IEnumerator SalirRutina(Vector2 foco, float zoom, Color color, VisualElement pantalla, Action mostrarMenu)
    {
        enTransicion = true;
        focoDestino = foco; zoomDestino = zoom;
        SetColorFundido(color);
        // la pantalla actual se desvanece
        yield return Fundir(pantalla, 1f, 0f, 1f, 1.06f);

        // volvemos al menu y se aleja la camara
        mostrarMenu?.Invoke();
        zoomT = 1f; SetFundido(1f); AplicarCamara();
        for (float t = 0; t < duracionZoom; t += Time.deltaTime)
        {
            zoomT = 1f - t / duracionZoom;
            SetFundido(Mathf.InverseLerp(0.55f, 1f, zoomT));
            AplicarCamara();
            yield return null;
        }
        zoomT = 0f; SetFundido(0f); AplicarCamara();
        if (pantalla != null) { pantalla.style.opacity = 1f; pantalla.style.scale = new Scale(Vector3.one); }
        enTransicion = false;
    }

    IEnumerator Fundir(VisualElement e, float opDesde, float opHasta, float escDesde, float escHasta)
    {
        if (e == null) yield break;
        for (float t = 0; t < duracionFundido; t += Time.deltaTime)
        {
            float k = Suave(t / duracionFundido);
            e.style.opacity = Mathf.Lerp(opDesde, opHasta, k);
            float s = Mathf.Lerp(escDesde, escHasta, k);
            e.style.scale = new Scale(new Vector3(s, s, 1f));
            yield return null;
        }
        e.style.opacity = opHasta;
        e.style.scale = new Scale(new Vector3(escHasta, escHasta, 1f));
    }

    void SetFundido(float a) { if (fundido != null) fundido.style.opacity = a; }

    void SetColorFundido(Color c)
    {
        if (fundido != null) fundido.style.backgroundColor = c;
        if (root != null) root.style.backgroundColor = c;// lo que se ve detras mientras cambia la pantalla
    }

    static bool Visible(VisualElement e) => e != null && e.resolvedStyle.display == DisplayStyle.Flex;

    void Update()
    {
        float t = Time.time;

        if (Visible(contenedor)) AnimarMenu(t);
        if (Visible(opciones))   AnimarOpciones(t);
        if (Visible(creditos))   AnimarCreditos();
    }

    void AnimarMenu(float t)
    {
        // Balanceo: va de -1 a 1 suave, lo multiplicamos por el angulo maximo
        Girar(linternaIzq, anguloLinternas * Mathf.Sin(t * 2f * Mathf.PI / periodoLinternas));
        Girar(linternaDer, anguloLinternas * Mathf.Sin(t * 2f * Mathf.PI / (periodoLinternas * 1.12f) + 1.3f));
        Girar(cartel,      anguloCartel    * Mathf.Sin(t * 2f * Mathf.PI / periodoCartel + 0.6f));

        // Gato respirando
        if (gato != null)
        {
            float r = 0.5f + 0.5f * Mathf.Sin(t * 2f * Mathf.PI / 2.4f);
            gato.style.scale = new Scale(new Vector3(1f + 0.012f * r, 1f - 0.015f * r, 1f));
        }

        // Pantalla de la radio: brillo que sube y baja
        if (radioLuz != null)
            radioLuz.style.opacity = 0.45f + 0.55f * (0.5f + 0.5f * Mathf.Sin(t * 2f * Mathf.PI / 3.8f));

        // TV: parpadeo irregular
        if (tvLuz != null && t >= proximoParpadeo)
        {
            bool apagon = UnityEngine.Random.value < 0.2f;
            tvLuz.style.opacity = apagon ? 0.2f : 1f;
            proximoParpadeo = t + (apagon ? UnityEngine.Random.Range(0.05f, 0.12f) : UnityEngine.Random.Range(0.3f, 1.5f));
        }
    }

    void AnimarOpciones(float t)
    {
        Cubrir(opciones, radioEscenario);

        // la pantalla verde "respira" y cada tanto titila
        if (radioBrillo != null)
        {
            float b = 0.6f + 0.4f * Mathf.Sin(t * 2f * Mathf.PI / 3f);
            if (UnityEngine.Random.value < 0.01f) b = 0.2f;
            radioBrillo.style.opacity = b;
        }

        // relleno de la barra y numero de porcentaje
        if (volumen != null)
        {
            float v = Mathf.InverseLerp(volumen.lowValue, volumen.highValue, volumen.value);
            if (rellenoVolumen != null) rellenoVolumen.style.width = Length.Percent(v * 100f);
            if (porcentaje != null) porcentaje.text = Mathf.RoundToInt(v * 100f) + "%";
        }
    }

    // Creditos estilo Star Wars:
    //  0-5 s "Hace mucho tiempo..." en celeste
    //  5-12 s el logo SUPER CHINO se aleja
    //  6 s en adelante: el texto amarillo sube y se achica hacia el horizonte
    void AnimarCreditos()
    {
        tiempoCreditos += Time.deltaTime;
        float tc = tiempoCreditos;
        float H = creditos.resolvedStyle.height;
        if (float.IsNaN(H) || H <= 0) return;

        if (introCreditos != null)
            introCreditos.style.opacity = Mathf.Clamp01(Mathf.Min(tc / 1f, (5f - tc) / 1f));

        if (logoCreditos != null)
        {
            float k = Mathf.Clamp01((tc - 5f) / 7f);
            float s = Mathf.Lerp(1.6f, 0.05f, 1f - Mathf.Pow(1f - k, 1.6f));
            logoCreditos.style.scale = new Scale(new Vector3(s, s, 1f));
            logoCreditos.style.opacity = tc < 5f ? 0f : Mathf.Clamp01((12f - tc) / 1.5f);
        }

        // texto que sube con perspectiva fingida
        float recorrido = Mathf.Max(0f, tc - 6f) * velocidadCreditos;
        float horizonte = H * 0.12f, abajo = H * 1.05f;
        float desplazamiento = 0f;
        foreach (var linea in lineas)
        {
            float alto = linea.resolvedStyle.height;
            if (float.IsNaN(alto)) alto = 80f;
            // d = distancia "en el piso" desde el borde de abajo (0 = abajo, crece hacia el fondo)
            float d = (recorrido - desplazamiento) / H;
            desplazamiento += alto;
            if (d < 0f) { linea.style.opacity = 0f; continue; }

            float z = 1f + d * 2.4f; // mas lejos = mas chico
            float y = horizonte + (abajo - horizonte) / z;
            float esc = 1f / z;
            linea.style.top = y - alto / 2f;
            linea.style.scale = new Scale(new Vector3(esc, esc, 1f));
            linea.style.opacity = Mathf.Clamp01((esc - 0.12f) / 0.15f);
        }

        // cuando la ultima linea se perdio en el horizonte, vuelve a empezar
        if ((recorrido - desplazamiento) / H > 3.2f) tiempoCreditos = 0f;
    }

    static void Girar(VisualElement e, float grados)
    {
        if (e != null) e.style.rotate = new Rotate(new Angle(grados, AngleUnit.Degree));
    }
}
