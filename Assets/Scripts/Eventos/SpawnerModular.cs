using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Services.Authentication.Server;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;

public class SpawnerModular : NetworkBehaviour
{
    [SerializeField] private GameObject objetoPrefab;//Por aqui desde el inspector asigno que objetos serán los que spawnearan
    [SerializeField] private Transform[] puntosSpawn;//Aca desde el inspector asigno los puntos de spawn que yo determine desde la gerarquia
    [SerializeField] private int cantidadAlIniciar;//Aca es el lugar para poner desde el inspector cuantos objetos quiero que spawneen de entrada
    [SerializeField] private int limiteMaxDeObjetosEnEscena;//Determinamos un limite de cantidad de objetos spawneados en el escenario, es más que nada para las Karens (bots), trabaja con otra variable que da la cantidad spawneada hasta el momento *
    [SerializeField] private float tiempoDeEsperaDeSpawn;//Tiempo de espera para spawnear, asi no es un delirio de objetos de una, da orden y lo hace más orgánico.
    private List<Transform> puntosLibres; //Para que revise la maquina donde tiene posibilidades de spawnear
    private int cantidadDeObjetosSpawneadosActualmente; //Variable deSeguimiento para verificar que las Karens no invadan todo el Chino *

    public override void OnNetworkSpawn()//Hereda esta función de NetworkBehaviour en lugar de Start()
    {
        base.OnNetworkSpawn();//Ejecuta la lógica de base del paquete NetworkBehaviour, Netcore registre el componente en la red correctamente

        if (!IsServer) return;//Aqui se consulta si es el servidor, si se trat de un cliente lo retournea asi no ejecuta el codico generando caos

        //Seguimos por aca, de ahora en más todo esto solo se ejecutara en el Servidor/Host...

        puntosLibres = new List<Transform>(puntosSpawn);//Aca creé una lista dinámica con todos los puntos de spawn que cargamos en el array "puntosSpawn"


        for (int i = 0; i < cantidadAlIniciar; i++)//Al comenzar a ejecutarce el script, primero se debe revizar si ya deben haber objetos spawneados 
        {
            SpawnearObjetos();
        }

        StartCoroutine(RutinaDeSpawn());//Esta corrutina nos va a servir para que no spawneen automaticamente sino que cada x tiempo que nosotros determinemos

    }

    private void SpawnearObjetos()
    {

        if (puntosLibres.Count == 0) return;//Si llegamos a ocupar todos los puntos de spawn detiene la logíca aca
        if (cantidadDeObjetosSpawneadosActualmente >= limiteMaxDeObjetosEnEscena) return;//Frena el procedimiento si es que ya se ocupo el maximo de lulagres

        int indiceAleatorioDeSpawn = Random.Range(0, puntosLibres.Count);//Creamos un indice aleatorio para luego elegir un punto de spawn de la lsita dinamica, de forma random *

        Transform puntoElegido = puntosLibres[indiceAleatorioDeSpawn];

        GameObject nuevoObjeto = Instantiate(objetoPrefab, puntoElegido.position, puntoElegido.rotation);//Obtengo su netwokobject y lo instancio para que se replique en todos los clientes
        nuevoObjeto.GetComponent<NetworkObject>().Spawn();//Aqui hacemos que seactulicen en los clientes también
        puntosLibres.RemoveAt(indiceAleatorioDeSpawn); //Por ultimo aca eliminamos la posicion que estaba almacenada en el indice que nos dió

        cantidadDeObjetosSpawneadosActualmente++;//Vamos contando cuantos lugares se van ocupando
    }

    private IEnumerator RutinaDeSpawn()
    {
        while (true)//Creamos un bucle
        {
            yield return new WaitForSeconds(tiempoDeEsperaDeSpawn);//Aca detenemos por el entero que hayamos asignado a la variable

            SpawnearObjetos();//y al cumplir ese tiempo de espera, spawneamos objeto nuevo

        }
    }

    public void LiberarPunto(Transform puntoADevolver)
    {
        if (!IsServer) return;

        puntosLibres.Add(puntoADevolver);

        cantidadDeObjetosSpawneadosActualmente--;

    }
}