using UnityEngine;

public class SpawnManager2D : MonoBehaviour
{
    public GameObject[] objetosASpawnear; // Prefabs a spawnear
    public float tiempoEntreSpawns = 10f;
    public float radioDelMapa = 10f; // Radio del círculo

    private float tiempoSiguienteSpawn;

    void Start()
    {
        tiempoSiguienteSpawn = Time.time + tiempoEntreSpawns;
    }

    void Update()
    {
        if (Time.time >= tiempoSiguienteSpawn)
        {
            SpawnearObjetoAleatorio();
            tiempoSiguienteSpawn = Time.time + tiempoEntreSpawns;
        }
    }

    void SpawnearObjetoAleatorio()
    {
        if (objetosASpawnear.Length == 0)
        {
            Debug.LogWarning("No hay objetos para spawnear.");
            return;
        }

        // Elegimos un prefab aleatorio
        int indice = Random.Range(0, objetosASpawnear.Length);

        // Posición aleatoria dentro de un círculo en 2D
        Vector2 posicion2D = Random.insideUnitCircle * radioDelMapa;

        // Instanciamos en el plano 2D (X, Y)
        Instantiate(objetosASpawnear[indice], posicion2D, Quaternion.identity);
    }
}
