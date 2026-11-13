using UnityEngine;
using Unity.Netcode;

public enum EstadoNPC
{
    CaminarACaja,//Avanza a caja si no hay NPC enfrente
    EnFila,//Se detiene si detecta otro NPC en frente
    EsperandoAtencion,//Llega a la caja y espera ser atendido
    Yendose//Ya atnedido avanza hacia su autodestruccion al pasar el area del escenario F
}

public class NPCCliente : NetworkBehaviour
{
    [Header("Configuracion de Sensor")]
    [SerializeField] private float distanciaSensor = 0.8f;
    [SerializeField] private LayerMask capaNPCs; //Dibujo el sector del NPC, esto nos va a servir para que otro NPC lo detecte


    [Header("Estado Actual (Lectura en Inspector)")] // 🦜 No estoy muy segura de esto luego consulto si es correcto
    [SerializeField] private EstadoNPC estadoActual = EstadoNPC.CaminarACaja;

    private Rigidbody2D rb;

    // Propiedades públicas para el Árbol de Comportamiento
    public EstadoNPC EstadoActual
    {
        get => estadoActual;
        set => estadoActual = value;
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    //Esta funcion me sirve para el arbol de comportamiento como condicion
    public bool DetectarNPCAdelante(Vector2 direccion)
    {
        // Si el NPC está frenado, no lanzamos el rayo o usamos la última dirección
        if (direccion == Vector2.zero) return false;

        Vector2 origenRayo = (Vector2)transform.position + direccion.normalized * 0.4f; //Hacemos que el rayo nazca fuera del area del mismo NPC para que no se buge con el mismo y entre en crisis existencial
        RaycastHit2D hit = Physics2D.Raycast(origenRayo, direccion.normalized, distanciaSensor, capaNPCs);

        // Dibuja el rayo en la vista de Scene para poder depurar visualmente en Unity
        Debug.DrawRay(transform.position, direccion.normalized * distanciaSensor, Color.red); return hit.collider != null;
    }
}