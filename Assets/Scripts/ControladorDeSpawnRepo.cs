using UnityEngine;
using Unity.Netcode;
using System.Collections;
using UnityEditor.AdaptivePerformance.Editor;

public class ControladorDeSpawnRepo : MonoBehaviour
{
    public GameObject objeto;
    public GameObject[] puntosSpawn;
    public Sprite[] sprites;

    private bool[] puntosOcupados;
    private int cantidadPuntosOcupados = 0;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        puntosOcupados = new bool[puntosSpawn.Length];
        

        StartCoroutine(SpawnAutomatico());

        //rimero spawnea 3 objetos para reponer
        for (int i = 0; i < 3; i++)
        {
            SpawnearObjeto();
        }

    }
    IEnumerator SpawnAutomatico()
    {
        while (true)
        {
            if (cantidadPuntosOcupados < puntosSpawn.Length)
            {
                yield return new WaitForSeconds(15f);
                SpawnearObjeto();
            }
            else
            {
                yield return new WaitForSeconds(5f);
            }
        }
    }

    void SpawnearObjeto()
    {   
        int numeroAleatorio;

        do
        {

            numeroAleatorio = Random.Range(0, puntosSpawn.Length);


        } while (puntosOcupados[numeroAleatorio] == true);

        puntosOcupados[numeroAleatorio] = true;
        cantidadPuntosOcupados++;

        GameObject nuevoObjeto = Instantiate(
            objeto,
            puntosSpawn[numeroAleatorio].transform.position,
            puntosSpawn[numeroAleatorio].transform.rotation
            );

        SpriteRenderer spriteRenderer = nuevoObjeto.GetComponent<SpriteRenderer>();

        int spriteAleatorio = Random.Range(0, sprites.Length);

        spriteRenderer.sprite = sprites[spriteAleatorio];
        

    }
    //bool HayPuntoLibre()
    //{
        //return cantidadPuntosOcupados < puntosSpawn.Length;
    //}

    // Update is called once per frame


}
