using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Prefab del enemigo etapa 1 (básico)")]
    public GameObject enemyStage1Prefab;

    [Tooltip("Prefab del enemigo etapa 2 (más difícil)")]
    public GameObject enemyStage2Prefab;

    [Tooltip("Prefab del enemigo etapa 3 (más duro)")]
    public GameObject enemyStage3Prefab;

    [Tooltip("Objeto vacío que contiene los hijos para spawn")]
    public Transform collisionsParent;

    [Header("Configuración de spawn")]
    [Tooltip("Tiempo inicial entre spawns en segundos")]
    public float initialSpawnInterval = 5f;

    [Tooltip("Tiempo mínimo entre spawns en segundos")]
    public float minSpawnInterval = 1f;

    [Tooltip("Duración (en segundos) para interpolar el spawn interval y tamaños")]
    public float scaleLerpDuration = 210f; // 3:30 minutos

    [Tooltip("Rango inicial de escala (min, max)")]
    public Vector2 startScaleRange = new Vector2(0.7f, 1f);

    [Tooltip("Rango final de escala (min, max)")]
    public Vector2 endScaleRange = new Vector2(0.2f, 0.35f);

    private float timer = 0f;
    private float elapsedTime = 0f;
    private float spawnInterval;

    void Start()
    {
        spawnInterval = initialSpawnInterval;

        if (enemyStage1Prefab == null)
            Debug.LogWarning("No has asignado el prefab del enemigo etapa 1 en el Inspector.");

        if (enemyStage2Prefab == null)
            Debug.LogWarning("No has asignado el prefab del enemigo etapa 2 en el Inspector.");

        if (enemyStage3Prefab == null)
            Debug.LogWarning("No has asignado el prefab del enemigo etapa 3 en el Inspector.");

        if (collisionsParent == null)
            Debug.LogWarning("No has asignado el objeto padre de colisiones para spawn.");
    }

    void Update()
    {
        elapsedTime += Time.deltaTime;

        // Interpolamos spawnInterval de forma más brusca:
        // Usamos una curva cuadrática para hacer que el cambio sea rápido al principio y lento después.
        float t = Mathf.Clamp01(elapsedTime / scaleLerpDuration);
        float curvedT = Mathf.Pow(t, 0.25f); // raíz cuarta para transición brusca inicial

        spawnInterval = Mathf.Lerp(initialSpawnInterval, minSpawnInterval, curvedT);

        timer += Time.deltaTime;

        if (timer >= spawnInterval)
        {
            // Spawnear entre 1 y 3 enemigos al mismo tiempo
            int enemiesToSpawn = Random.Range(1, 4); // 1, 2 o 3

            for (int i = 0; i < enemiesToSpawn; i++)
            {
                SpawnEnemy();
            }

            timer = 0f;
        }
    }

    void SpawnEnemy()
    {
        if (collisionsParent == null || collisionsParent.childCount == 0)
        {
            return; // No puede spawnear si falta algo
        }

        // Elegir un hijo aleatorio de collisionsParent para spawn
        Transform spawnPoint = collisionsParent.GetChild(Random.Range(0, collisionsParent.childCount));

        // Interpolación de tamaño según el tiempo transcurrido
        float t = Mathf.Clamp01(elapsedTime / scaleLerpDuration);
        float minScale = Mathf.Lerp(startScaleRange.x, endScaleRange.x, t);
        float maxScale = Mathf.Lerp(startScaleRange.y, endScaleRange.y, t);

        float randomScale = Random.Range(minScale, maxScale);

        // Calcular normalizado para el color y velocidad
        float normScale = Mathf.InverseLerp(endScaleRange.x, startScaleRange.y, randomScale);

        // Elegir qué tipo de enemigo spawnear según el tiempo
        GameObject prefabToSpawn;

        if (elapsedTime < scaleLerpDuration * 0.5f)
        {
            // Primer 50% del tiempo: solo etapa 1
            prefabToSpawn = enemyStage1Prefab;
        }
        else if (elapsedTime < scaleLerpDuration * 0.85f)
        {
            // Entre 50% y 85%: etapa 1 y etapa 2 mezclados
            prefabToSpawn = (Random.value < 0.6f) ? enemyStage1Prefab : enemyStage2Prefab;
        }
        else
        {
            // Últimos 15%: etapa 1, 2 y 3 mezclados, con más chance de etapa 3
            float rand = Random.value;
            if (rand < 0.4f)
                prefabToSpawn = enemyStage3Prefab;
            else if (rand < 0.7f)
                prefabToSpawn = enemyStage2Prefab;
            else
                prefabToSpawn = enemyStage1Prefab;
        }

        if (prefabToSpawn == null)
            return; // Evitar errores

        // Instanciar el clon
        GameObject clone = Instantiate(prefabToSpawn, spawnPoint.position, Quaternion.identity);

        // Asegurarse que el clon esté activo (visible)
        clone.SetActive(true);

        clone.transform.localScale = new Vector3(randomScale, randomScale, 1f);

        SpriteRenderer sr = clone.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            // Color del clone entre negro (pequeño) y rojo vivo (grande)
            sr.color = Color.Lerp(Color.black, Color.red, normScale);
        }

        HomingEnemy he = clone.GetComponent<HomingEnemy>();
        if (he != null)
        {
            // Ajusta la velocidad: más pequeño = más rápido
            he.maxSpeed = Mathf.Lerp(15f, 10f, normScale);
            he.collisionsParent = collisionsParent;
        }
    }
}
