using UnityEngine;
using Unity.Netcode;
public class ControladorDeSpawn : NetworkBehaviour
{
    [Header("Prefabs y Referencias")]
    public GameObject objeto;
    public GameObject[] puntosSpawn;
    public Sprite[] sprites;
    private bool[] puntosOcupados;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer)
        {
            puntosOcupados = new bool[puntosSpawn.Length];

            for (int i = 0; i < 5; i++)
            {
                SpawnearObjeto(i);
            }
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

        NetworkObject netObj = nuevoObjeto.GetComponent<NetworkObject>();
        if (netObj != null)
        {
            netObj.Spawn();
        }

        SincronizarSpriteClientRpc(netObj.NetworkObjectId, i);
    }

    [ClientRpc]
    private void SincronizarSpriteClientRpc(ulong networkObjectId, int indexSprite)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(networkObjectId, out NetworkObject netObj))
        {
            SpriteRenderer spriteRenderer = netObj.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null && indexSprite < sprites.Length)
            {
                spriteRenderer.sprite = sprites[indexSprite];
            }
        }
    }
}

