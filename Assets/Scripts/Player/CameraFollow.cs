using Unity.Netcode;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public static CameraFollow Instance;

    [Header("Configuración de Seguimiento")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 0f, -10f);
    [SerializeField] private float smoothSpeed = 5f;

    [Header("Configuración de Hiperfoco (Zoom)")]
    [SerializeField] private float zoomSpeed = 6f;
    private Camera cam;
    private float defaultOrthoSize;
    private float targetOrthoSize;

    private Transform targetTransform;

    private void Awake()
    {
        
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        cam = GetComponent<Camera>();
        if (cam != null)
        {
            defaultOrthoSize = cam.orthographicSize;
            targetOrthoSize = defaultOrthoSize;
        }
    }

    // Método que llamará el personaje local cuando nazca en la red
    public void SetTarget(Transform target)
    {
        targetTransform = target;
    }
    public void SetZoomMultiplier(float multiplier)
    {
        if (cam == null) return;
        targetOrthoSize = defaultOrthoSize / multiplier;
    }

    private void LateUpdate()
    {
        if (targetTransform == null)
        {
            FindLocalPlayer();
            return;
        }

        // Seguimiento de posición
        Vector3 desiredPosition = targetTransform.position + offset;
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);

        // Interpolación suave del Zoom de hiperfoco
        if (cam != null)
        {
            cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetOrthoSize, zoomSpeed * Time.deltaTime);
        }
    }

    private void FindLocalPlayer()
    {
        // Si el NetworkManager ya está activo, buscamos el objeto local del jugador
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.LocalClient != null)
        {
            var localPlayerObject = NetworkManager.Singleton.LocalClient.PlayerObject;
            if (localPlayerObject != null)
            {
                targetTransform = localPlayerObject.transform;
            }
        }
    }
}
