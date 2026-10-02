using UnityEngine;

public class ControladorDeSpawn : MonoBehaviour
{
    public GameObject objeto;
    public GameObject[] puntosSpawn;
    private int numeroAleatoreo;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

        if (Input.GetKeyDown(KeyCode.K))
        {
            numeroAleatoreo = Random.Range(0, puntosSpawn.Length);
            Instantiate(objeto, puntosSpawn[numeroAleatoreo].transform.position,
                puntosSpawn[numeroAleatoreo].transform.rotation);

        }
    }
}
