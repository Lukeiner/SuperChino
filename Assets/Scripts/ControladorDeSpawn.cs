using UnityEngine;
using Unity.Netcode;


public class ControladorDeSpawn : MonoBehaviour
{
    public GameObject objeto;
    public GameObject[] puntosSpawn;
    public Sprite[] sprites;

    private bool[] puntosOcupados;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        puntosOcupados = new bool[puntosSpawn.Length];

        for (int i = 0; i < 5; i++)
        {
            SpawnearObjeto(i);
        }
    }

    void SpawnearObjeto(int i)
    {
        int numeroAleatorio;

        do
        {

            numeroAleatorio = Random.Range(0, puntosSpawn.Length);

        } while (puntosOcupados[numeroAleatorio] == true);

        puntosOcupados[numeroAleatorio] = true;

        GameObject nuevoObjeto = Instantiate(
            objeto,
            puntosSpawn[numeroAleatorio].transform.position,
            puntosSpawn[numeroAleatorio].transform.rotation
            );

        SpriteRenderer spriteRenderer = nuevoObjeto.GetComponent<SpriteRenderer>();

        spriteRenderer.sprite = sprites[i];


    }

    // Update is called once per frame
    

}
