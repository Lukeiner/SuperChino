using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

///animaciones del menu de pausa.
///va en el mismo GameObject que PauseMenuUI y el UIDocument (UIPause).
///usa Time.unscaledDeltaTime para que funcione la animacion
[RequireComponent(typeof(UIDocument))]
public class PausaAnimaciones : MonoBehaviour
{
    [Header("Tamaño")]
    [SerializeField] float escalaTabla = 0.92f;

    [Header("Duraciones (segundos)")]
    [SerializeField] float duracionEntrada = 0.4f;
    [SerializeField] float duracionSalida = 0.25f;
    [SerializeField] float duracionGiro = 0.36f;// la vuelta completa (dos mitades)

    VisualElement root, tabla, fondo, rellenoVolumen;
    Label porcentaje;
    Slider volumen;

    //mientras dura una animación no se aceptan otros toques, evita dobles clics
    public bool Ocupado { get; private set; }

    void OnEnable()
    {
        root = GetComponent<UIDocument>().rootVisualElement;
        tabla = root.Q<VisualElement>("Tabla");
        porcentaje = root.Q<Label>("VolumenPorcentaje");
        volumen = root.Q<Slider>("volumeSlider");

        //relleno de la barra: un elemento propio adentro del tracker del Slider.
        var tracker = volumen?.Q<VisualElement>(className: "unity-base-slider__tracker");
        if (tracker != null)
        {
            rellenoVolumen = new VisualElement { pickingMode = PickingMode.Ignore };
            rellenoVolumen.AddToClassList("lapiz-relleno");
            tracker.Add(rellenoVolumen);
        }

        Ocupado = false;
        Poner(0f, 0f, 1f, 1f);
    }

    void OnDisable()
    {
        rellenoVolumen?.RemoveFromHierarchy();
        rellenoVolumen = null;
    }

    void Update()
    {
        //Relleno de la barra y numero de porcentaje
        if (volumen == null) return;
        float v = Mathf.InverseLerp(volumen.lowValue, volumen.highValue, volumen.value);
        if (rellenoVolumen != null) rellenoVolumen.style.width = Length.Percent(v * 100f);
        if (porcentaje != null) porcentaje.text = Mathf.RoundToInt(v * 100f) + "%";
    }

    public void Entrar() => StartCoroutine(EntrarRutina());
    public void Salir(Action alTerminar) => StartCoroutine(SalirRutina(alTerminar));
    public void Girar(Action cambiarCara) => StartCoroutine(GirarRutina(cambiarCara));

    IEnumerator EntrarRutina()
    {
        Ocupado = true;
        // Esto corre en el mismo frame en que se muestra la pausa: no llega a verse en el centro.
        Poner(1150f, 10f, 1f, 1f);
        SetFondo(0f);

        for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / duracionEntrada)
        {
            float e = ConRebote(t);
            Poner(Mathf.LerpUnclamped(1150f, 0f, e),
                  Mathf.LerpUnclamped(10f, 0f, e),
                  1f, 1f);
            SetFondo(Mathf.Clamp01(t * 2f));
            yield return null;
        }
        Poner(0f, 0f, 1f, 1f);
        SetFondo(1f);
        Ocupado = false;
    }

    IEnumerator SalirRutina(Action alTerminar)
    {
        Ocupado = true;
        for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / duracionSalida)
        {
            float e = t * t;
            Poner(Mathf.Lerp(0f, 1150f, e), Mathf.Lerp(0f, -8f, e), 1f, 1f);
            SetFondo(1f - t);
            yield return null;
        }
        alTerminar?.Invoke();
        Poner(0f, 0f, 1f, 1f);
        SetFondo(1f);
        Ocupado = false;
    }
    
    // para hacer el giro del menu es modificar el ancho, se achica hasta 0
    // ahi se cambia la imagen de frente por la de atrás, y el ancho vuelve a 1
    IEnumerator GirarRutina(Action cambiarCara)
    {
        Ocupado = true;
        float mitad = duracionGiro * 0.5f;

        for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / mitad)
        {
            float e = t * t;
            Poner(-30f * e, 4f * e, 1f - e, 1f + 0.05f * e);
            yield return null;
        }

        cambiarCara?.Invoke();

        for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / mitad)
        {
            float e = 1f - (1f - t) * (1f - t);
            Poner(-30f * (1f - e), 4f * (1f - e), e, 1f + 0.05f * (1f - e));
            yield return null;
        }
        Poner(0f, 0f, 1f, 1f);
        Ocupado = false;
    }

    // mueve, gira, achica la tabla. EscalaX y escalaY se multiplican
    void Poner(float y, float grados, float escalaX, float escalaY)
    {
        if (tabla == null) return;
        tabla.style.translate = new Translate(0f, y);
        tabla.style.rotate = new Rotate(Angle.Degrees(grados));
        tabla.style.scale = new Scale(new Vector3(escalaX * escalaTabla, escalaY * escalaTabla, 1f));
    }

    // la capa oscura la crea PauseMenuUI por código; se busca recién cuando hace falta.
    void SetFondo(float opacidad)
    {
        if (fondo == null) fondo = root?.Q<VisualElement>("PauseDimLayer");
        if (fondo != null) fondo.style.opacity = opacidad;
    }

    // "Ease out back": llega, se pasa un poco y vuelve. Da sensación de objeto con peso.
    static float ConRebote(float t)
    {
        const float c1 = 1.70158f, c3 = c1 + 1f;
        float u = t - 1f;
        return 1f + c3 * u * u * u + c1 * u * u;
    }
}
