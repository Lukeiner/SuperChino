using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class HUDUI : MonoBehaviour
{
    public static Vector2 MoveInput { get; private set; }

    [SerializeField] private PauseMenuUI pauseMenu;

    private const int NoPointer = -1;
    private const float DeadZone = 0.15f;
    private const float FallbackBaseSize = 260f;
    private const float FallbackKnobSize = 120f;

    private VisualElement joystickZone;
    private VisualElement joystickBase;
    private VisualElement joystickKnob;
    private Button pauseButton;

    private int activePointerId = NoPointer;
    private Vector2 origin;
    private float maxRadius = FallbackBaseSize * 0.5f;

    private void OnEnable()
    {
        VisualElement root = GetComponent<UIDocument>().rootVisualElement;

        joystickZone = Find<VisualElement>(root, "JoystickZone");
        joystickBase = Find<VisualElement>(root, "JoystickBase");
        joystickKnob = Find<VisualElement>(root, "JoystickKnob");
        pauseButton = Find<Button>(root, "pauseButton");

        if (pauseButton != null)
            pauseButton.clicked += OnPauseClicked;

        if (joystickZone != null)
        {
            joystickZone.RegisterCallback<PointerDownEvent>(OnPointerDown);
            joystickZone.RegisterCallback<PointerMoveEvent>(OnPointerMove);
            joystickZone.RegisterCallback<PointerUpEvent>(OnPointerUp);
            joystickZone.RegisterCallback<PointerCancelEvent>(OnPointerCancel);
        }

        ResetStick();
    }

    private void OnDisable()
    {
        if (pauseButton != null)
            pauseButton.clicked -= OnPauseClicked;

        if (joystickZone != null)
        {
            joystickZone.UnregisterCallback<PointerDownEvent>(OnPointerDown);
            joystickZone.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
            joystickZone.UnregisterCallback<PointerUpEvent>(OnPointerUp);
            joystickZone.UnregisterCallback<PointerCancelEvent>(OnPointerCancel);
        }

        ResetStick();
    }


    private void OnPauseClicked()
    {
        if (pauseMenu == null)
            pauseMenu = FindFirstObjectByType<PauseMenuUI>();

        if (pauseMenu != null)
            pauseMenu.ShowPauseMenu();
        else
            Debug.LogError("[HUDUI] No se encontró el PauseMenuUI en la escena.", this);
    }


    private void OnPointerDown(PointerDownEvent evt)
    {
        if (activePointerId != NoPointer) return;

        activePointerId = evt.pointerId;

        joystickZone.CapturePointer(evt.pointerId);

        origin = evt.localPosition;
        maxRadius = ReadSize(joystickBase.resolvedStyle.width, FallbackBaseSize) * 0.5f;

        PlaceCentered(joystickBase, origin, FallbackBaseSize);
        PlaceCentered(joystickKnob, origin, FallbackKnobSize);
        SetStickVisible(true);

        evt.StopPropagation();
    }

    private void OnPointerMove(PointerMoveEvent evt)
    {
        if (evt.pointerId != activePointerId) return;

        Vector2 delta = (Vector2)evt.localPosition - origin;
        Vector2 clamped = Vector2.ClampMagnitude(delta, maxRadius);

        PlaceCentered(joystickKnob, origin + clamped, FallbackKnobSize);

        Vector2 direction = new Vector2(clamped.x, -clamped.y) / maxRadius;
        MoveInput = direction.magnitude < DeadZone ? Vector2.zero : direction;

        evt.StopPropagation();
    }

    private void OnPointerUp(PointerUpEvent evt)
    {
        if (evt.pointerId != activePointerId) return;

        EndStick(evt.pointerId);
        evt.StopPropagation();
    }

    private void OnPointerCancel(PointerCancelEvent evt)
    {
        if (evt.pointerId != activePointerId) return;

        EndStick(evt.pointerId);
        evt.StopPropagation();
    }

    private void EndStick(int pointerId)
    {
        if (joystickZone != null && joystickZone.HasPointerCapture(pointerId))
            joystickZone.ReleasePointer(pointerId);

        ResetStick();
    }

    private void ResetStick()
    {
        activePointerId = NoPointer;
        MoveInput = Vector2.zero;
        SetStickVisible(false);
    }


    private void SetStickVisible(bool visible)
    {
        Visibility value = visible ? Visibility.Visible : Visibility.Hidden;
        if (joystickBase != null) joystickBase.style.visibility = value;
        if (joystickKnob != null) joystickKnob.style.visibility = value;
    }

    private static void PlaceCentered(VisualElement element, Vector2 center, float fallbackSize)
    {
        if (element == null) return;

        float width = ReadSize(element.resolvedStyle.width, fallbackSize);
        float height = ReadSize(element.resolvedStyle.height, fallbackSize);

        element.style.left = center.x - width * 0.5f;
        element.style.top = center.y - height * 0.5f;
    }

    private static float ReadSize(float value, float fallback)
    {
        return float.IsNaN(value) || value <= 0f ? fallback : value;
    }

    private T Find<T>(VisualElement root, string elementName) where T : VisualElement
    {
        T element = root.Q<T>(elementName);
        if (element == null)
            Debug.LogError($"[HUDUI] No se encontró '{elementName}' en el UXML. Revisa el nombre.", this);
        return element;
    }
}
