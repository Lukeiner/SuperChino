using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(
    name: "Buscar Punto Mas Cercano Por Tag",
    story: "[Self] buscar punto con tag [tagBuscada] mas cercano y guardar en [destinoTransform]",
    category: "Action/NPC"
)]
public partial class DetectarPuntoMasCercanoAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Self;
    [SerializeReference] public BlackboardVariable<string> tagBuscada;
    [SerializeReference] public BlackboardVariable<Transform> destinoTransform;

    protected override Status OnStart()
    {
        if (Self.Value == null) //Que este el objeto ejecutor
        {
            Debug.LogError("[BT Error] El objeto Self no esta asignado");
            return Status.Failure;

        }

        if (String.IsNullOrEmpty(tagBuscada.Value)) //Que este la Tag asignada
        {
            Debug.LogError("[BT Error] La tagBuscada esta vacia.");
            return Status.Failure;

        }

        GameObject[] objetosEncontrados = GameObject.FindGameObjectsWithTag(tagBuscada.Value);
        //Creo que esto no funciona porque los puntos de spawn no estan en el blackboard como Objetos sino como Transform quizas deba cambiar
        if (objetosEncontrados.Length == 0)
        {
            Debug.LogError("[BT Error] No existe objeto con esa Tag");
            return Status.Failure;

        }

        Vector3 posicionNPC = Self.Value.transform.position;
        GameObject objetoMasCercano = null;
        float menorDistancia = Mathf.Infinity;

        foreach (GameObject obj in objetosEncontrados)
        {

            float distancia = Vector2.Distance(posicionNPC, obj.transform.position);
            if (distancia < menorDistancia)
            {

                menorDistancia = distancia;
                objetoMasCercano = obj;

            }

        }

        if (objetoMasCercano != null)
        {
            destinoTransform.Value = objetoMasCercano.transform;
            return Status.Success;

        }

        return Status.Failure;

    }

    protected override Status OnUpdate()
    {
        return Status.Success;
    }

    protected override void OnEnd()
    {
        base.OnEnd();
    }



}
