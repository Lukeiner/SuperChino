using UnityEngine;
using UnityEngine.Rendering;

/// Dibuja y anima al huevo. Va en el prefab Player (junto a PlayerIdentity).
///
/// El personaje es UNO solo y se arma por capas (todas con el mismo lienzo, asi encajan solas):
///   sombra, pies, cuerpo, ropa, cara, gorro y las dos manos.
/// Los pies y el cuerpo quedan fijos; solo se mueven las manos (como en South Park).
/// Los sprites se cargan por nombre desde Assets/Resources/Personaje/, asi que una ropa nueva
/// (tienda de cosmeticos) es agregar sus PNG con el nombre correcto y llamar a SetRopa("nombre").
///   ropa_<nombre>_frente.png, ropa_<nombre>_atras.png y gorro_<nombre>.png
///
/// No toca la red: el movimiento se deduce de cuanto cambia la posicion, asi funciona igual
/// para el jugador local y para los demas (NetworkTransform).
[DisallowMultipleComponent]
public class PersonajeVisual : MonoBehaviour
{
    public enum Expresion { Normal = 1, Sorpresa = 2, Nervioso = 3, Grito = 4, Risa = 5 }

    [Header("Manos al caminar")]
    [SerializeField] private float balanceoManos = 0.1f;      // cuanto suben y bajan (unidades)
    [SerializeField] private float giroManos = 14f;            // cuanto giran (grados)
    [SerializeField] private float frecuenciaMin = 1.6f;       // pasos por segundo yendo despacio
    [SerializeField] private float frecuenciaMax = 2.6f;       // pasos por segundo a toda velocidad

    [Header("Cuerpo entero (suave; en 0 queda totalmente fijo)")]
    [SerializeField] private float rebote = 0.02f;
    [SerializeField] private float inclinacion = 2f;

    [Header("Otros")]
    [SerializeField] private float velocidadMaxima = 6f;       // igual que moveSpeed de PlayerController
    [SerializeField] private bool mostrarSombra = true;

    // Medidas sacadas del dibujo (lienzo de 1144x928 px originales, 232 px por unidad)
    private const float AlturaPies = -0.4957f;                 // los pies estan a esta altura del centro
    private static readonly Vector3 Centro = new Vector3(0f, 0.4957f, 0f);   // centro del lienzo, relativo a los pies
    private static readonly Vector3 PosManoIzq = new Vector3(0.4289f, 0.5216f, 0f);   // hombro, relativo a los pies
    private static readonly Vector3 PosManoDer = new Vector3(-0.4289f, 0.5216f, 0f);
    // El sprite de cada mano usa el lienzo completo (pivote al centro); colgado del hombro,
    // su centro queda a esta distancia, asi la mano gira alrededor del hombro.
    private static readonly Vector3 DesdeHombroIzq = new Vector3(-0.4289f, -0.0259f, 0f);
    private static readonly Vector3 DesdeHombroDer = new Vector3(0.4289f, -0.0259f, 0f);

    private Transform raiz, manoIzq, manoDer;
    private SpriteRenderer rSombra, rPies, rCuerpo, rRopa, rCara, rGorro, rManoIzq, rManoDer;
    private PlayerIdentity identidad;

    private string ropa = "repositor";
    private bool deEspalda;
    private bool ropaManual;
    private Expresion expresion = Expresion.Normal;
    private RoleType rolMostrado = (RoleType)(-1);

    private Vector3 ultimaPos;
    private float tMuestra;
    private float velocidad;          // unidades por segundo, ya suavizada
    private float cuantoCamina;       // 0 = quieto, 1 = caminando (suavizado)
    private float fase;
    private Vector2 direccion = Vector2.down;

    private void Awake()
    {
        identidad = GetComponent<PlayerIdentity>();

        // El cuadrado de prueba que tenia el prefab ya no se usa.
        var viejo = GetComponent<SpriteRenderer>();
        Material material = viejo != null ? viejo.sharedMaterial : null;
        if (viejo != null) viejo.enabled = false;

        // "raiz" esta parada en los pies: el SortingGroup ordena a todo el personaje junto
        // segun la altura de los pies (por eso pasa por delante o por detras de las gondolas).
        raiz = new GameObject("Visual").transform;
        raiz.SetParent(transform, false);
        raiz.localPosition = new Vector3(0f, AlturaPies, 0f);
        raiz.gameObject.AddComponent<SortingGroup>();

        rSombra  = Capa("Sombra", 0, new Vector3(0f, 0.03f, 0f), material);
        rSombra.transform.localScale = new Vector3(0.95f, 0.5f, 1f);
        rSombra.color = new Color(1f, 1f, 1f, 0.3f);
        rSombra.enabled = mostrarSombra;
        rPies    = Capa("Pies", 1, Centro, material);
        rCuerpo  = Capa("Cuerpo", 2, Centro, material);
        rRopa    = Capa("Ropa", 3, Centro, material);
        rCara    = Capa("Cara", 4, Centro, material);
        rGorro   = Capa("Gorro", 5, Centro, material);
        manoIzq = Pivote("HombroIzquierdo", PosManoIzq);
        manoDer = Pivote("HombroDerecho", PosManoDer);
        rManoIzq = Capa("ManoIzquierda", 6, DesdeHombroIzq, material, manoIzq);
        rManoDer = Capa("ManoDerecha", 6, DesdeHombroDer, material, manoDer);

        rSombra.sprite  = Cargar("sombra");
        rPies.sprite    = Cargar("pies");
        rCuerpo.sprite  = Cargar("cuerpo");
        rManoIzq.sprite = Cargar("mano_izq");
        rManoDer.sprite = Cargar("mano_der");

        ultimaPos = transform.position;
        ActualizarCapas();
    }

    /// Cambia la expresion de la cara (la espalda no tiene cara).
    public void SetExpresion(Expresion nueva)
    {
        expresion = nueva;
        ActualizarCapas();
    }

    /// Pone una ropa y gorro por nombre ("limpieza", "cajero", "repositor" o una nueva de la tienda).
    /// Despues de esto la ropa deja de seguir al rol de PlayerIdentity.
    public void SetRopa(string nombre)
    {
        ropa = nombre.ToLowerInvariant();
        ropaManual = true;
        ActualizarCapas();
    }

    // ---------------- Cada cuadro ----------------

    private void Update()
    {
        // Si no se eligio ropa a mano, sigue el rol del jugador
        if (!ropaManual && identidad != null && identidad.CurrentRole != rolMostrado)
        {
            rolMostrado = identidad.CurrentRole;
            ropa = rolMostrado.ToString().ToLowerInvariant();
            ActualizarCapas();
        }
    }

    private void LateUpdate()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        // Velocidad medida cada 0.1 s (suaviza los saltos de la fisica y de la red)
        tMuestra += dt;
        if (tMuestra >= 0.1f)
        {
            Vector3 delta = transform.position - ultimaPos;
            float v = delta.magnitude / tMuestra;
            velocidad = Mathf.Lerp(velocidad, v, 0.6f);
            if (v > 0.3f) direccion = ((Vector2)delta).normalized;
            ultimaPos = transform.position;
            tMuestra = 0f;
        }

        bool camina = velocidad > 0.4f;
        cuantoCamina = Mathf.MoveTowards(cuantoCamina, camina ? 1f : 0f, dt * 6f);

        // Frente o espalda segun hacia donde va (arriba = espalda)
        if (camina)
        {
            bool atras = direccion.y > 0.35f;
            if (atras != deEspalda) { deEspalda = atras; ActualizarCapas(); }
        }

        float rapido = Mathf.Clamp01(velocidad / Mathf.Max(0.01f, velocidadMaxima));
        fase += dt * Mathf.Lerp(frecuenciaMin, frecuenciaMax, rapido) * Mathf.PI * 2f * cuantoCamina
              + dt * 1.6f * (1f - cuantoCamina);             // quieto: respira despacio

        float s = Mathf.Sin(fase);
        // Quieto: las manos "respiran" juntas. Caminando: se balancean una contra la otra.
        float respira = Mathf.Sin(fase) * 0.012f * (1f - cuantoCamina);
        float suben = s * balanceoManos * cuantoCamina;
        // Las manos NO giran: se quedan en los costados y solo suben y bajan (brazos abiertos).

        manoIzq.localPosition = PosManoIzq + new Vector3(0f, respira + suben, 0f);
        manoDer.localPosition = PosManoDer + new Vector3(0f, respira - suben, 0f);
        manoIzq.localRotation = Quaternion.identity;
        manoDer.localRotation = Quaternion.identity;

        // Cuerpo entero: rebote y leve inclinacion, solo si camina
        float rebota = Mathf.Abs(Mathf.Sin(fase)) * rebote * cuantoCamina;
        raiz.localPosition = new Vector3(0f, AlturaPies + rebota, 0f);
        raiz.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(fase) * inclinacion * cuantoCamina);
    }

    // ---------------- Armado ----------------

    private void ActualizarCapas()
    {
        if (rRopa == null) return;

        string vista = deEspalda ? "atras" : "frente";
        rRopa.sprite = Cargar($"ropa_{ropa}_{vista}");
        rGorro.sprite = Cargar($"gorro_{ropa}");
        rCara.sprite = deEspalda ? null : Cargar($"cara_{(int)expresion}");
    }

    private Transform Pivote(string nombre, Vector3 pos)
    {
        var t = new GameObject(nombre).transform;
        t.SetParent(raiz, false);
        t.localPosition = pos;
        return t;
    }

    private SpriteRenderer Capa(string nombre, int orden, Vector3 pos, Material material, Transform padre = null)
    {
        var go = new GameObject(nombre);
        go.transform.SetParent(padre != null ? padre : raiz, false);
        go.transform.localPosition = pos;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sortingOrder = orden;
        if (material != null) sr.sharedMaterial = material;
        return sr;
    }

    private static Sprite Cargar(string nombre)
    {
        var sprite = Resources.Load<Sprite>("Personaje/" + nombre);
        if (sprite == null)
            Debug.LogWarning($"[PersonajeVisual] Falta el sprite 'Assets/Resources/Personaje/{nombre}.png'.");
        return sprite;
    }
}
