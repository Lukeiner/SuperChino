using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Tablero de corcho de la votacion (dibujo de Leo) y pantalla de resultado.
/// Lo crea solo VotingUI la primera vez que hace falta: se agrega al GameObject del HUD
/// y dibuja encima del HUD con UI Toolkit. No hay que tocar la escena.
///
/// Imagenes y fuente en Assets/Resources/Votacion:
///   tablero, titulo (se estira a lo ancho para que entre el texto), polaroid, chinche, hilo,
///   circulo, boton, sello_ya_votaste, sello_despedido, sello_empate, sello_ladron, fuente_mano.
/// Las medidas estan en px de la pantalla de referencia (1920x1080) y salen del dibujo de Krita.
/// </summary>
public class TableroVotacionUI : MonoBehaviour
{
    public static TableroVotacionUI Instance { get; private set; }

    /// <summary>Devuelve el tablero; si no existe lo crea en el objeto del HUD.</summary>
    public static TableroVotacionUI Obtener()
    {
        if (Instance != null) return Instance;
        HUDUI hud = FindAnyObjectByType<HUDUI>();
        if (hud == null) return null;
        return hud.gameObject.AddComponent<TableroVotacionUI>();
    }

    public enum Resultado : byte { NadieDespedido = 0, Inocente = 1, Ladron = 2 }

    // ---- medidas del dibujo (px) ----
    const float TabAncho = 1418f, TabAlto = 768f, TabArriba = 255f;
    const float PolAncho = 255f, PolAlto = 306f, PolSeparacion = 45f, PolArriba = 190f, PolEscala = 1.1f;
    const float FotoX = 24f, FotoY = 33f, FotoAncho = 201f, FotoAlto = 173f;
    const float CirculoX = 18f, CirculoY = 19f, CirculoAncho = 216f, CirculoAlto = 272f;
    const float ChincheTam = 35f, HiloAlto = 13f;
    const float TituloAltura = 103f, TituloArriba = 50f, TituloMinAncho = 641f, TituloBorde = 70f;
    const float BotonAncho = 517f, BotonAlto = 87f, BotonArriba = 600f;
    // las capas del personaje (Resources/Personaje) miden 286x232
    const float CapaAncho = 286f, CapaAlto = 232f;

    static readonly Color TintaOscura = new Color32(45, 35, 40, 255);
    static readonly Color TintaAzul = new Color32(35, 45, 110, 255);

    [Header("Resultado")]
    [SerializeField] private float duracionResultado = 3.2f;

    private VisualElement capa, tablero, contenido, sello;
    private Label tituloTexto, mensaje;
    private VisualElement titulo, boton;
    private Label botonTexto;
    private Font fuente;
    private readonly Dictionary<string, Texture2D> cache = new Dictionary<string, Texture2D>();
    private readonly List<(ulong id, VisualElement circulo)> fotos = new List<(ulong, VisualElement)>();

    private ulong? elegido;
    private bool yaVoto;
    private Action<ulong> alVotar;
    private IVisualElementScheduledItem cierreProgramado;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
        fuente = Resources.Load<Font>("Votacion/fuente_mano");
    }

    private void OnDestroy() { if (Instance == this) Instance = null; }

    // ================= votacion =================

    /// <summary>Abre el tablero con una foto por jugador vivo. alVotar se llama con el id elegido.</summary>
    public void AbrirVotacion(IList<ulong> vivos, ulong miId, Action<ulong> alVotar)
    {
        if (!Armar()) return;
        cierreProgramado?.Pause();
        this.alVotar = alVotar;
        elegido = null; yaVoto = false;
        Limpiar();
        PonerTitulo("¿Quién se robó el objeto?", TintaOscura);
        mensaje.style.display = DisplayStyle.None;

        float pw = PolAncho * PolEscala;
        float ancho = vivos.Count * pw + Mathf.Max(0, vivos.Count - 1) * PolSeparacion;
        float x0 = (TabAncho - ancho) / 2f;
        var chinches = new List<Vector2>();
        for (int i = 0; i < vivos.Count; i++)
        {
            ulong id = vivos[i];
            bool soyYo = id == miId;
            float x = x0 + i * (pw + PolSeparacion);
            VisualElement pol = CrearPolaroid(id, soyYo ? "Vos" : NombreDe(id), x, PolArriba, PolEscala, Inclinacion(id), out VisualElement circulo);
            if (soyYo) pol.style.opacity = 0.75f; // no te podes votar a vos mismo
            else
            {
                pol.RegisterCallback<PointerDownEvent>(_ => Elegir(id));
                fotos.Add((id, circulo));
            }
            chinches.Add(new Vector2(x + pw / 2f, PolArriba - 6f));
        }
        PonerHiloYChinches(chinches);

        boton.style.display = DisplayStyle.Flex;
        boton.BringToFront();
        ActualizarBoton();
        sello.style.display = DisplayStyle.None;
        Mostrar();
    }

    public void CerrarVotacion()
    {
        if (capa == null) return;
        // si ya hay un resultado en pantalla no lo tapamos
        if (mensaje != null && mensaje.style.display == DisplayStyle.Flex) return;
        Ocultar();
    }

    private void Elegir(ulong id)
    {
        if (yaVoto) return;
        elegido = id;
        foreach (var f in fotos) f.circulo.style.display = f.id == id ? DisplayStyle.Flex : DisplayStyle.None;
        ActualizarBoton();
    }

    private void Votar()
    {
        if (yaVoto || elegido == null) return;
        yaVoto = true;
        alVotar?.Invoke(elegido.Value);
        ActualizarBoton();
        boton.style.display = DisplayStyle.None; // el sello va donde estaba el boton
        PonerSello("sello_ya_votaste", new Vector2(TabAncho / 2f, BotonArriba + BotonAlto / 2f), -6f, 0.85f);
    }

    private void ActualizarBoton()
    {
        bool activo = !yaVoto && elegido != null;
        boton.SetEnabled(activo);
        boton.style.opacity = activo ? 1f : 0.55f;
        botonTexto.text = yaVoto ? "Esperando..." : "VOTAR";
    }

    // ================= resultado =================

    /// <summary>Muestra a todos quien fue despedido. Se cierra solo.</summary>
    public void MostrarResultado(Resultado tipo, ulong id, IList<ulong> vivos)
    {
        if (!Armar()) return;
        Limpiar();
        boton.style.display = DisplayStyle.None;
        sello.style.display = DisplayStyle.None;

        if (tipo == Resultado.NadieDespedido)
        {
            PonerTitulo("Nadie fue despedido", TintaOscura);
            int n = vivos != null ? vivos.Count : 0;
            float esc = 0.8f, w = PolAncho * esc, sep = 40f;
            float x0 = (TabAncho - (n * w + Mathf.Max(0, n - 1) * sep)) / 2f;
            var chinches = new List<Vector2>();
            for (int i = 0; i < n; i++)
            {
                float x = x0 + i * (w + sep);
                CrearPolaroid(vivos[i], NombreDe(vivos[i]), x, PolArriba + 10f, esc, Inclinacion(vivos[i]), out _);
                chinches.Add(new Vector2(x + w / 2f, PolArriba + 4f));
            }
            PonerHiloYChinches(chinches);
            PonerSello("sello_empate", new Vector2(TabAncho / 2f, 525f), -7f, 0.75f);
            PonerMensaje("Hubo empate o nadie votó. Sigue la próxima ronda.");
        }
        else
        {
            bool ladron = tipo == Resultado.Ladron;
            PonerTitulo(ladron ? "¡Atraparon al ladrón!" : NombreDe(id) + " fue despedido",
                        ladron ? new Color32(25, 105, 45, 255) : new Color32(170, 20, 20, 255));
            float esc = 1.15f;
            float x = (TabAncho - PolAncho * esc) / 2f;
            CrearPolaroid(id, NombreDe(id), x, PolArriba - 15f, esc, -3f, out _);
            PonerHiloYChinches(new List<Vector2> { new Vector2(TabAncho / 2f, PolArriba - 21f) });
            PonerSello(ladron ? "sello_ladron" : "sello_despedido", new Vector2(TabAncho / 2f, 330f), ladron ? -12f : 10f, 0.85f);
            PonerMensaje(ladron ? NombreDe(id) + " era el ladrón. ¡Ganan los empleados!"
                                : "...y NO era el ladrón. El ladrón sigue suelto.");
        }

        Mostrar();
        cierreProgramado?.Pause();
        cierreProgramado = capa.schedule.Execute(Ocultar).StartingIn((long)(duracionResultado * 1000));
    }

    // ================= armado =================

    private bool Armar()
    {
        if (capa != null) return true;
        UIDocument doc = GetComponent<UIDocument>();
        if (doc == null || doc.rootVisualElement == null) return false;

        capa = new VisualElement { name = "TableroVotacion" };
        capa.style.position = Position.Absolute;
        capa.style.left = 0; capa.style.right = 0; capa.style.top = 0; capa.style.bottom = 0;
        capa.style.backgroundColor = new Color(0f, 0f, 0f, 0.45f);
        capa.pickingMode = PickingMode.Position; // tapa los toques: mientras vota no se mueve el joystick
        capa.style.display = DisplayStyle.None;
        doc.rootVisualElement.Add(capa);

        tablero = Img("tablero", TabAncho, TabAlto);
        tablero.style.left = Length.Percent(50);
        tablero.style.marginLeft = -TabAncho / 2f;
        tablero.style.top = TabArriba;
        capa.Add(tablero);

        contenido = new VisualElement { pickingMode = PickingMode.Ignore };
        Abs(contenido, 0, 0, TabAncho, TabAlto);
        tablero.Add(contenido);

        // titulo de papel: se estira a lo ancho sin deformar las puntas (9-slice)
        titulo = new VisualElement { pickingMode = PickingMode.Ignore };
        titulo.style.position = Position.Absolute;
        titulo.style.top = TituloArriba;
        titulo.style.left = Length.Percent(50);
        titulo.style.translate = new Translate(Length.Percent(-50), 0);
        titulo.style.height = TituloAltura;
        titulo.style.minWidth = TituloMinAncho;
        titulo.style.backgroundImage = new StyleBackground(Tex("titulo"));
        titulo.style.unitySliceLeft = (int)TituloBorde; titulo.style.unitySliceRight = (int)TituloBorde;
        titulo.style.unitySliceTop = 0; titulo.style.unitySliceBottom = 0;
        titulo.style.paddingLeft = TituloBorde; titulo.style.paddingRight = TituloBorde;
        titulo.style.justifyContent = Justify.Center;
        tituloTexto = Texto("", 58, TintaOscura);
        tituloTexto.style.unityTextAlign = TextAnchor.MiddleCenter;
        tituloTexto.style.whiteSpace = WhiteSpace.NoWrap;
        titulo.Add(tituloTexto);
        tablero.Add(titulo);

        // boton votar
        boton = Img("boton", BotonAncho, BotonAlto);
        boton.style.left = (TabAncho - BotonAncho) / 2f; boton.style.top = BotonArriba;
        boton.style.justifyContent = Justify.Center;
        boton.pickingMode = PickingMode.Position;
        botonTexto = Texto("VOTAR", 60, Color.white);
        botonTexto.style.unityTextAlign = TextAnchor.MiddleCenter;
        boton.Add(botonTexto);
        boton.RegisterCallback<PointerDownEvent>(_ => { if (boton.enabledSelf) boton.style.scale = new Scale(new Vector2(0.95f, 0.95f)); });
        boton.RegisterCallback<PointerUpEvent>(_ => { boton.style.scale = new Scale(Vector2.one); Votar(); });
        boton.RegisterCallback<PointerLeaveEvent>(_ => boton.style.scale = new Scale(Vector2.one));
        tablero.Add(boton);

        mensaje = Texto("", 52, TintaOscura);
        Abs(mensaje, 60, 590, TabAncho - 120, 110);
        mensaje.style.unityTextAlign = TextAnchor.MiddleCenter;
        mensaje.style.whiteSpace = WhiteSpace.Normal;
        tablero.Add(mensaje);

        sello = new VisualElement { pickingMode = PickingMode.Ignore };
        sello.style.position = Position.Absolute;
        tablero.Add(sello);
        return true;
    }

    private VisualElement CrearPolaroid(ulong id, string nombre, float x, float y, float escala, float giro, out VisualElement circulo)
    {
        VisualElement pol = Img("polaroid", PolAncho * escala, PolAlto * escala);
        pol.style.left = x; pol.style.top = y;
        pol.style.rotate = new Rotate(giro);
        pol.pickingMode = PickingMode.Position;
        contenido.Add(pol);

        // foto: el personaje del jugador con la ropa de su puesto
        VisualElement foto = new VisualElement { pickingMode = PickingMode.Ignore };
        Abs(foto, FotoX * escala, FotoY * escala, FotoAncho * escala, FotoAlto * escala);
        foto.style.overflow = Overflow.Hidden;
        pol.Add(foto);
        float k = FotoAncho * escala / CapaAncho * 1.15f;
        float cw = CapaAncho * k, chh = CapaAlto * k;
        string ropa = RopaDe(id);
        foreach (string capaNombre in new[] { "pies", "cuerpo", $"ropa_{ropa}_frente", "cara_1", $"gorro_{ropa}" })
        {
            Texture2D t = Tex("../Personaje/" + capaNombre, "Personaje/" + capaNombre);
            if (t == null) continue;
            VisualElement c = new VisualElement { pickingMode = PickingMode.Ignore };
            Abs(c, (FotoAncho * escala - cw) / 2f, (FotoAlto * escala - chh) / 2f + 6f * escala, cw, chh);
            c.style.backgroundImage = new StyleBackground(t);
            foto.Add(c);
        }

        Label etiqueta = Texto(nombre, 40 * escala, TintaAzul);
        Abs(etiqueta, 0, 205 * escala, PolAncho * escala, 95 * escala);
        etiqueta.style.unityTextAlign = TextAnchor.MiddleCenter;
        pol.Add(etiqueta);

        circulo = Img("circulo", CirculoAncho * escala, CirculoAlto * escala);
        circulo.style.left = CirculoX * escala; circulo.style.top = CirculoY * escala;
        circulo.style.display = DisplayStyle.None;
        pol.Add(circulo);
        return pol;
    }

    private void PonerHiloYChinches(List<Vector2> puntos)
    {
        if (puntos.Count >= 2)
        {
            VisualElement hilo = Img("hilo", puntos[puntos.Count - 1].x - puntos[0].x + 30f, HiloAlto);
            hilo.style.left = puntos[0].x - 15f; hilo.style.top = puntos[0].y + ChincheTam / 2f - HiloAlto / 2f;
            contenido.Add(hilo);
        }
        foreach (Vector2 p in puntos)
        {
            VisualElement c = Img("chinche", ChincheTam, ChincheTam);
            c.style.left = p.x - ChincheTam / 2f; c.style.top = p.y;
            contenido.Add(c);
        }
    }

    private void PonerSello(string imagen, Vector2 centro, float giro, float escala)
    {
        Texture2D t = Tex(imagen);
        if (t == null) return;
        float w = t.width * escala, h = t.height * escala;
        sello.style.backgroundImage = new StyleBackground(t);
        sello.style.width = w; sello.style.height = h;
        sello.style.left = centro.x - w / 2f; sello.style.top = centro.y - h / 2f;
        sello.style.rotate = new Rotate(giro);
        sello.style.display = DisplayStyle.Flex;
        sello.BringToFront();
        // golpe del sello: entra grande y se asienta
        sello.style.scale = new Scale(new Vector2(1.6f, 1.6f));
        sello.style.opacity = 0f;
        sello.schedule.Execute(() =>
        {
            sello.style.transitionProperty = new List<StylePropertyName> { new StylePropertyName("scale"), new StylePropertyName("opacity") };
            sello.style.transitionDuration = new List<TimeValue> { new TimeValue(0.18f), new TimeValue(0.12f) };
            sello.style.scale = new Scale(Vector2.one);
            sello.style.opacity = 1f;
        }).StartingIn(30);
    }

    private void PonerTitulo(string texto, Color color)
    {
        tituloTexto.text = texto;
        tituloTexto.style.color = color;
    }

    private void PonerMensaje(string texto)
    {
        mensaje.text = texto;
        mensaje.style.display = DisplayStyle.Flex;
    }

    private void Limpiar()
    {
        contenido.Clear();
        fotos.Clear();
        if (sello != null) { sello.style.transitionDuration = new List<TimeValue> { new TimeValue(0f) }; }
        if (mensaje != null) mensaje.style.display = DisplayStyle.None;
    }

    private void Mostrar()
    {
        capa.style.display = DisplayStyle.Flex;
        capa.BringToFront();
    }

    private void Ocultar()
    {
        if (capa == null) return;
        capa.style.display = DisplayStyle.None;
        if (mensaje != null) mensaje.style.display = DisplayStyle.None;
    }

    // ================= ayudas =================

    private static string NombreDe(ulong id) => $"Jugador {id + 1}";

    private static float Inclinacion(ulong id) => ((int)(id * 37 % 7) - 3) * 1.2f;

    private static string RopaDe(ulong id)
    {
        var nm = NetworkManager.Singleton;
        NetworkObject obj = nm != null && nm.SpawnManager != null ? nm.SpawnManager.GetPlayerNetworkObject(id) : null;
        PlayerIdentity ident = obj != null ? obj.GetComponent<PlayerIdentity>() : null;
        return ident != null ? ident.CurrentRole.ToString().ToLowerInvariant() : "repositor";
    }

    private Texture2D Tex(string nombre, string rutaCompleta = null)
    {
        string ruta = rutaCompleta ?? "Votacion/" + nombre;
        if (cache.TryGetValue(ruta, out Texture2D t)) return t;
        t = Resources.Load<Texture2D>(ruta);
        if (t == null) Debug.LogWarning($"[TableroVotacionUI] Falta Resources/{ruta}.png", this);
        cache[ruta] = t;
        return t;
    }

    private VisualElement Img(string nombre, float w, float h)
    {
        VisualElement e = new VisualElement { pickingMode = PickingMode.Ignore };
        e.style.position = Position.Absolute;
        e.style.width = w; e.style.height = h;
        e.style.backgroundImage = new StyleBackground(Tex(nombre));
        return e;
    }

    private static void Abs(VisualElement e, float x, float y, float w, float h)
    {
        e.style.position = Position.Absolute;
        e.style.left = x; e.style.top = y; e.style.width = w; e.style.height = h;
    }

    private Label Texto(string t, float tam, Color color)
    {
        Label l = new Label(t) { pickingMode = PickingMode.Ignore };
        l.style.fontSize = tam;
        l.style.color = color;
        l.style.marginLeft = 0; l.style.marginRight = 0; l.style.paddingLeft = 0; l.style.paddingRight = 0;
        if (fuente != null) l.style.unityFontDefinition = FontDefinition.FromFont(fuente);
        return l;
    }
}
