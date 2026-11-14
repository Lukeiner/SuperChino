using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Reloj de ronda del HUD (despertador digital). Muestra el tiempo que queda (MM:SS) y la ronda actual / total.
/// Lee las variables del RoundManager; no hace falta conectarlo a nada mas.
/// En los ultimos segundos la pantalla del reloj parpadea en rojo.
/// Va en el mismo GameObject que el UIDocument del HUD.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class RelojRondaUI : MonoBehaviour
{
    [Header("Alerta de poco tiempo")]
    [SerializeField] private float segundosAlerta = 5f;
    [SerializeField] private float parpadeosPorSegundo = 2f;

    private VisualElement reloj, alerta;
    private readonly VisualElement[] tiempo = new VisualElement[4]; // M M S S
    private readonly VisualElement[] ronda = new VisualElement[2];  // actual / total
    private readonly int[] ultimoTiempo = { -1, -1, -1, -1 };
    private readonly int[] ultimaRonda = { -1, -1 };

    private void OnEnable()
    {
        VisualElement root = GetComponent<UIDocument>().rootVisualElement;

        reloj = Find(root, "RelojRoot");
        alerta = Find(root, "RelojAlerta");
        for (int i = 0; i < tiempo.Length; i++) tiempo[i] = Find(root, "T" + i);
        for (int i = 0; i < ronda.Length; i++) ronda[i] = Find(root, "R" + i);

        // se muestra recien cuando existe el RoundManager (es decir, ya en partida)
        if (reloj != null) reloj.style.display = DisplayStyle.None;
        for (int i = 0; i < ultimoTiempo.Length; i++) ultimoTiempo[i] = -1;
        for (int i = 0; i < ultimaRonda.Length; i++) ultimaRonda[i] = -1;
    }

    private void Update()
    {
        if (reloj == null) return;

        RoundManager manager = RoundManager.Instance;
        if (manager == null)
        {
            reloj.style.display = DisplayStyle.None;
            return;
        }

        reloj.style.display = DisplayStyle.Flex;

        int segundos = Mathf.Max(0, Mathf.CeilToInt(manager.timeRemaining.Value));
        int minutos = Mathf.Min(99, segundos / 60);
        int resto = segundos % 60;

        PonerDigito(tiempo[0], 0, minutos / 10, ultimoTiempo);
        PonerDigito(tiempo[1], 1, minutos % 10, ultimoTiempo);
        PonerDigito(tiempo[2], 2, resto / 10, ultimoTiempo);
        PonerDigito(tiempo[3], 3, resto % 10, ultimoTiempo);

        PonerDigito(ronda[0], 0, Mathf.Clamp(manager.currentRound.Value, 0, 9), ultimaRonda);
        PonerDigito(ronda[1], 1, Mathf.Clamp(manager.totalRounds.Value, 0, 9), ultimaRonda);

        // parpadeo de la pantalla en los ultimos segundos de una fase con cuenta regresiva
        RoundManager.GameState estado = manager.currentGameState.Value;
        bool contando = estado == RoundManager.GameState.TaskPhase || estado == RoundManager.GameState.DebatePhase;
        bool poco = contando && segundos > 0 && segundos <= segundosAlerta;
        bool encendido = poco && ((int)(Time.unscaledTime * parpadeosPorSegundo * 2f) % 2 == 0);

        if (alerta != null)
            alerta.style.display = encendido ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private static void PonerDigito(VisualElement elemento, int indice, int valor, int[] cache)
    {
        if (elemento == null || cache[indice] == valor) return;

        if (cache[indice] >= 0) elemento.RemoveFromClassList("d" + cache[indice]);
        elemento.AddToClassList("d" + valor);
        cache[indice] = valor;
    }

    private VisualElement Find(VisualElement root, string elementName)
    {
        VisualElement element = root.Q<VisualElement>(elementName);
        if (element == null)
            Debug.LogError($"[RelojRondaUI] No se encontro '{elementName}' en HUD.uxml.", this);
        return element;
    }
}
