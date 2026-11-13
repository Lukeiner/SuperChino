using UnityEngine;
using Unity.Netcode;
using System.Runtime.CompilerServices;



public class ObjetoRobable : NetworkBehaviour //Asi transformo el script de MonoBehaviour a NetworkBehaviour y deja de ser solo local
{
    [SerializeField] private Tipojugador tipoPermitido;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
