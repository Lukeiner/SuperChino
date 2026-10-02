using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public static CameraFollow Instance;

    [Header("Configuración de Seguimiento")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 0f, -10f);
    [SerializeField] private float smoothSpeed = 5f;

    private Transform targetTransform;

    private void Awake()
    {
        // Singleton sencillo para que el player local pueda registrarse fácilmente
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Método que llamará el personaje local cuando nazca en la red
    public void SetTarget(Transform target)
    {
        targetTransform = target;
    }

    private void LateUpdate()
    {
        if (targetTransform == null) return;

        // Posición deseada con el offset
        Vector3 desiredPosition = targetTransform.position + offset;

        // Movimiento suave (Lerp)
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);

        transform.position = smoothedPosition;
    }
}
