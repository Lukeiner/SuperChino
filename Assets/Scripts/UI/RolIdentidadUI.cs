using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

/// <summary>
/// Pantalla de rol e identidad (credencial dibujada por Leo).
/// Al empezar la primera fase de tareas de la partida baja una credencial del Super Chino colgada de su cinta,
/// se balancea y muestra quien sos: EMPLEADO (cinta azul) o LADRON (cinta roja, antifaz y sello).
/// Queda unos segundos (o hasta tocar la pantalla) y vuelve a subir.
///
/// Se agrega sola al objeto del HUD cuando carga la escena: no hay que tocar la escena.
/// Imagenes en Assets/Resources/Rol (cinta_azul, cinta_roja, credencial, foto_fondo, antifaz, sello_ladron)
/// y la fuente a mano en Assets/Resources/Votacion/fuente_mano.
/// Medidas en px de la pantalla de referencia (1920x1080), sacadas del dibujo de Krita (grupo de 281x558).
/// </summary>
public class RolIdentidadUI : MonoBehaviour
{
    public static RolIdentidadUI Instance { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Instalar()
    {
        SceneManager.sceneLoaded -= AlCargarEscena;
        SceneManager.sceneLoaded += AlCargarEscena;
        AlCargarEscena(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    private static void AlCargarEscena(Scene escena, LoadSceneMode modo)
    {
        HUDUI hud = FindAnyObjectByType<HUDUI>();
        if (hud != null && hud.GetComponent<RolIdentidadUI>() == null) hud.gameObject.AddComponent<RolIdentidadUI>();
    }

    [Header("Tamaño y tiempos")]
    [SerializeField] private float escala = 1.8f;           // el dibujo original es de 281x558 px
    [SerializeField] private float arriba = -150f;          // donde queda la punta de la cinta (negativo: sale de arriba de la pantalla)
    [SerializeField] private float tiempoVisible = 5f;      // segundos antes de subir solo
    [SerializeField] private float duracionBajada = 0.6f;
    [SerializeField] private float duracionSubida = 0.45f;
    [SerializeField] private float anguloBalanceo = 7f;
    [SerializeField] private float velocidadBalanceo = 7f;
    [SerializeField] private float frenadoBalanceo = 1.6f;

    [Header("Pruebas (solo en el editor / development build)")]
    [SerializeField] private bool teclasDePrueba = true;    // F11 empleado, F12 ladron

    // medidas del dibujo, relativas al grupo (cinta + credencial)
    const float GrupoAncho = 281f, GrupoAlto = 558f;
    const float FotoX = 81f, FotoY = 342f, FotoAncho = 132f, FotoAlto = 113f;
    const float CapaAncho = 286f, CapaAlto = 232f;

    static readonly Color Tinta = new Color32(45, 35, 40, 255);
    static readonly Color Azul = new Color32(35, 45, 120, 255);
    static readonly Color Crema = new Color32(250, 238, 210, 255);

    private enum Etapa { Escondido, Bajando, Visible, Subiendo }
    private Etapa etapa = Etapa.Escondido;
    private float t, tBalanceo;
    private bool yaMostrado;
    private float esperaIdentidad = -1f;

    private VisualElement capa, grupo, cinta, foto, antifaz, sello;
    private Label nombre, puesto, ladoIzq1, ladoIzq2, ladoDer1, ladoDer2;
    private Font fuente;

    private void OnEnable()
    {
        Instance = this;
        fuente = Resources.Load<Font>("Votacion/fuente_mano");
        Armar();
    }

    private void Update()
    {
        if (teclasDePrueba && (Application.isEditor || Debug.isDebugBuild))
        {
            if (Input.GetKeyDown(KeyCode.F11)) Mostrar(false, RoleType.Repositor, 0);
            if (Input.GetKeyDown(KeyCode.F12)) Mostrar(true, RoleType.Cajero, 1);
        }

        // Normalmente la abre el RoundManager (MostrarMiIdentidadAhora) apenas reparte los roles.
        // Si por algo no llego ese aviso, se muestra igual al arrancar la primera fase de tareas.
        RoundManager rm = RoundManager.Instance;
        if (!yaMostrado && rm != null && rm.currentGameState.Value == RoundManager.GameState.TaskPhase)
            MostrarMiIdentidadAhora();
        if (esperaIdentidad >= 0f)
        {
            esperaIdentidad -= Time.unscaledDeltaTime;
            if (esperaIdentidad < 0f) MostrarMiIdentidad();
        }

        Animar(Time.unscaledDeltaTime);
    }

    /// <summary>La llama el RoundManager en todos apenas se reparten los roles.</summary>
    public void MostrarMiIdentidadAhora()
    {
        if (yaMostrado) return;
        yaMostrado = true;
        esperaIdentidad = 0.4f; // un respiro para que llegue la identidad por red
    }

    private void MostrarMiIdentidad()
    {
        var nm = NetworkManager.Singleton;
        NetworkObject yo = nm != null && nm.LocalClient != null ? nm.LocalClient.PlayerObject : null;
        PlayerIdentity id = yo != null ? yo.GetComponent<PlayerIdentity>() : null;
        if (id == null) return;
        Mostrar(id.CurrentFaction == Faction.Ladron, id.CurrentRole, nm.LocalClientId);
    }

    private void OnDestroy() { if (Instance == this) Instance = null; }

    // ================= contenido =================

    /// <summary>Muestra la credencial. Se puede llamar desde otro script si hace falta.</summary>
    public void Mostrar(bool ladron, RoleType rol, ulong miId)
    {
        if (capa == null && !Armar()) return;
        string ropa = rol.ToString().ToLowerInvariant();

        cinta.style.backgroundImage = new StyleBackground(Tex(ladron ? "cinta_roja" : "cinta_azul"));
        PonerPersonaje(ropa);
        antifaz.style.display = ladron ? DisplayStyle.Flex : DisplayStyle.None;
        sello.style.display = DisplayStyle.None;

        nombre.text = $"JUGADOR {miId + 1}";
        puesto.text = rol.ToString().ToUpperInvariant();

        if (ladron)
        {
            ladoIzq1.text = "Sos el LADRÓN";
            ladoIzq2.text = $"Disfrazado de {rol.ToString().ToUpperInvariant()}";
            ladoDer1.text = "Tu credencial es falsa";
            ladoDer2.text = "Robá los objetos sagrados sin que te descubran";
            ladoIzq1.style.color = (Color)new Color32(255, 120, 110, 255);
        }
        else
        {
            ladoIzq1.text = "Sos EMPLEADO";
            ladoIzq2.text = "Descubrí al ladrón";
            ladoDer1.text = "Tu tarea";
            ladoDer2.text = Tarea(rol);
            ladoIzq1.style.color = (Color)new Color32(150, 190, 255, 255);
        }

        capa.style.display = DisplayStyle.Flex;
        capa.BringToFront();
        etapa = Etapa.Bajando; t = 0f; tBalanceo = 0f;
        Aplicar(-1f, 0f);
        if (ladron) capa.schedule.Execute(GolpeDeSello).StartingIn((long)((duracionBajada + 0.5f) * 1000));
    }

    private static string Tarea(RoleType rol)
    {
        switch (rol)
        {
            case RoleType.Limpieza: return "Limpiá los charcos del súper";
            case RoleType.Repositor: return "Reponé la mercadería en las góndolas";
            case RoleType.Cajero: return "Atendé la caja y vigilá";
            default: return "Trabajá y vigilá el súper";
        }
    }

    private void PonerPersonaje(string ropa)
    {
        foto.Clear();
        float k = FotoAncho * escala / CapaAncho * 1.15f;
        float cw = CapaAncho * k, ch = CapaAlto * k;
        float ox = (FotoAncho * escala - cw) / 2f, oy = (FotoAlto * escala - ch) / 2f + 4f * escala;
        foreach (string n in new[] { "pies", "cuerpo", $"ropa_{ropa}_frente", "cara_1", $"gorro_{ropa}" })
        {
            Texture2D tx = Resources.Load<Texture2D>("Personaje/" + n);
            if (tx == null) continue;
            VisualElement c = Elem(ox, oy, cw, ch);
            c.style.backgroundImage = new StyleBackground(tx);
            foto.Add(c);
        }
        // antifaz sobre los ojos (coordenadas de las capas del personaje)
        antifaz = Elem(ox + 64f * k, oy + 67f * k, 150f * k, 60f * k);
        antifaz.style.backgroundImage = new StyleBackground(Tex("antifaz"));
        foto.Add(antifaz);
    }

    private void GolpeDeSello()
    {
        if (etapa == Etapa.Escondido || etapa == Etapa.Subiendo) return;
        sello.style.display = DisplayStyle.Flex;
        sello.style.transitionDuration = new System.Collections.Generic.List<TimeValue> { new TimeValue(0f) };
        sello.style.scale = new Scale(new Vector2(1.7f, 1.7f));
        sello.style.opacity = 0f;
        sello.schedule.Execute(() =>
        {
            sello.style.transitionProperty = new System.Collections.Generic.List<StylePropertyName> { new StylePropertyName("scale"), new StylePropertyName("opacity") };
            sello.style.transitionDuration = new System.Collections.Generic.List<TimeValue> { new TimeValue(0.18f), new TimeValue(0.12f) };
            sello.style.scale = new Scale(Vector2.one);
            sello.style.opacity = 1f;
        }).StartingIn(30);
    }

    // ================= animacion =================

    private void Animar(float dt)
    {
        if (etapa == Etapa.Escondido || grupo == null) return;
        t += dt; tBalanceo += dt;
        float giro = anguloBalanceo * Mathf.Exp(-frenadoBalanceo * tBalanceo) * Mathf.Sin(velocidadBalanceo * tBalanceo);

        switch (etapa)
        {
            case Etapa.Bajando:
            {
                float p = Mathf.Clamp01(t / duracionBajada);
                Aplicar(-1f + SalidaConRebote(p), giro);
                if (p >= 1f) { etapa = Etapa.Visible; t = 0f; }
                break;
            }
            case Etapa.Visible:
                Aplicar(0f, giro);
                if (t >= tiempoVisible) Subir();
                break;
            case Etapa.Subiendo:
            {
                float p = Mathf.Clamp01(t / duracionSubida);
                Aplicar(-p * p, giro);
                if (p >= 1f) { etapa = Etapa.Escondido; capa.style.display = DisplayStyle.None; }
                break;
            }
        }
    }

    private void Subir()
    {
        if (etapa == Etapa.Visible || etapa == Etapa.Bajando) { etapa = Etapa.Subiendo; t = 0f; }
    }

    // desplazamiento: 0 = colgada en su lugar, -1 = escondida arriba de la pantalla
    private void Aplicar(float desplazamiento, float giro)
    {
        float alto = (GrupoAlto * escala + arriba + 40f);
        grupo.style.translate = new Translate(0, desplazamiento * alto);
        grupo.style.rotate = new Rotate(giro);
        float a = Mathf.Clamp01(1f + desplazamiento * 1.5f);
        foreach (Label l in new[] { ladoIzq1, ladoIzq2, ladoDer1, ladoDer2 }) l.style.opacity = a;
        capa.style.backgroundColor = new Color(0f, 0f, 0f, 0.55f * a);
    }

    private static float SalidaConRebote(float x)
    {
        const float c1 = 1.4f, c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(x - 1f, 3) + c1 * Mathf.Pow(x - 1f, 2);
    }

    // ================= armado =================

    private bool Armar()
    {
        if (capa != null) return true;
        UIDocument doc = GetComponent<UIDocument>();
        if (doc == null || doc.rootVisualElement == null) return false;
        float e = escala;

        capa = new VisualElement { name = "RolIdentidad" };
        capa.style.position = Position.Absolute;
        capa.style.left = 0; capa.style.right = 0; capa.style.top = 0; capa.style.bottom = 0;
        capa.pickingMode = PickingMode.Position; // tapa los toques mientras se muestra
        capa.style.display = DisplayStyle.None;
        capa.RegisterCallback<PointerDownEvent>(_ => { if (etapa == Etapa.Visible && t > 1.2f) Subir(); });
        doc.rootVisualElement.Add(capa);

        grupo = new VisualElement { pickingMode = PickingMode.Ignore };
        grupo.style.position = Position.Absolute;
        grupo.style.width = GrupoAncho * e; grupo.style.height = GrupoAlto * e;
        grupo.style.left = Length.Percent(50); grupo.style.marginLeft = -GrupoAncho * e / 2f;
        grupo.style.top = arriba;
        grupo.style.transformOrigin = new TransformOrigin(Length.Percent(50), 0);
        capa.Add(grupo);

        cinta = Elem(0, 0, GrupoAncho * e, GrupoAlto * e); grupo.Add(cinta);
        VisualElement cred = Elem(0, 0, GrupoAncho * e, GrupoAlto * e);
        cred.style.backgroundImage = new StyleBackground(Tex("credencial")); grupo.Add(cred);

        Label marca = Texto("SUPER CHINO", 30 * e, Color.white); Abs(marca, 24 * e, 282 * e, 246 * e, 46 * e); grupo.Add(marca);

        VisualElement fondoFoto = Elem(0, 0, GrupoAncho * e, GrupoAlto * e);
        fondoFoto.style.backgroundImage = new StyleBackground(Tex("foto_fondo")); grupo.Add(fondoFoto);
        foto = Elem(FotoX * e, FotoY * e, FotoAncho * e, FotoAlto * e);
        foto.style.overflow = Overflow.Hidden; grupo.Add(foto);

        nombre = Texto("", 26 * e, Tinta); Abs(nombre, 24 * e, 458 * e, 246 * e, 34 * e); grupo.Add(nombre);
        Label et = Texto("PUESTO:", 17 * e, new Color32(120, 100, 95, 255)); Abs(et, 24 * e, 490 * e, 246 * e, 22 * e); grupo.Add(et);
        puesto = Texto("", 34 * e, Azul); Abs(puesto, 24 * e, 508 * e, 246 * e, 42 * e); grupo.Add(puesto);

        Texture2D st = Tex("sello_ladron");
        float sw = (st != null ? st.width : 500) * 0.30f * e, sh = (st != null ? st.height : 190) * 0.30f * e;
        sello = Elem(185f * e - sw / 2f, 440f * e - sh / 2f, sw, sh);
        sello.style.backgroundImage = new StyleBackground(st);
        sello.style.rotate = new Rotate(-14f);
        sello.style.display = DisplayStyle.None;
        grupo.Add(sello);

        // textos a los costados
        float medio = arriba + 420f * e;
        ladoIzq1 = Lado(Crema, 58, true, medio - 60f); ladoIzq2 = Lado(Crema, 40, true, medio + 10f);
        ladoDer1 = Lado(Crema, 40, false, medio - 60f); ladoDer2 = Lado(Crema, 44, false, medio - 10f);
        return true;
    }

    private Label Lado(Color c, float tam, bool izquierda, float y)
    {
        Label l = Texto("", tam, c);
        l.style.position = Position.Absolute;
        l.style.top = y; l.style.width = 520f;
        l.style.whiteSpace = WhiteSpace.Normal;
        l.style.left = Length.Percent(50);
        l.style.marginLeft = izquierda ? -(GrupoAncho * escala / 2f + 40f + 520f) : (GrupoAncho * escala / 2f + 40f);
        l.style.unityTextAlign = izquierda ? TextAnchor.UpperRight : TextAnchor.UpperLeft;
        l.style.textShadow = new TextShadow { offset = new Vector2(3, 3), blurRadius = 2, color = new Color(0, 0, 0, 0.8f) };
        capa.Add(l);
        return l;
    }

    private Texture2D Tex(string n)
    {
        Texture2D t2 = Resources.Load<Texture2D>("Rol/" + n);
        if (t2 == null) Debug.LogWarning($"[RolIdentidadUI] Falta Resources/Rol/{n}.png", this);
        return t2;
    }

    private static VisualElement Elem(float x, float y, float w, float h)
    {
        VisualElement v = new VisualElement { pickingMode = PickingMode.Ignore };
        Abs(v, x, y, w, h);
        return v;
    }

    private static void Abs(VisualElement v, float x, float y, float w, float h)
    {
        v.style.position = Position.Absolute;
        v.style.left = x; v.style.top = y; v.style.width = w; v.style.height = h;
    }

    private Label Texto(string s, float tam, Color c)
    {
        Label l = new Label(s) { pickingMode = PickingMode.Ignore };
        l.style.fontSize = tam; l.style.color = c;
        l.style.unityTextAlign = TextAnchor.MiddleCenter;
        l.style.marginLeft = 0; l.style.marginRight = 0; l.style.paddingLeft = 0; l.style.paddingRight = 0;
        l.style.marginTop = 0; l.style.marginBottom = 0; l.style.paddingTop = 0; l.style.paddingBottom = 0;
        if (fuente != null) l.style.unityFontDefinition = FontDefinition.FromFont(fuente);
        return l;
    }
}
