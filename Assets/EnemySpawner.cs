using UnityEngine;
using System;
using System.Reflection;

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
    public float scaleLerpDuration = 70f; // 1:10 minutos

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

        float t = Mathf.Clamp01(elapsedTime / scaleLerpDuration);
        float curvedT = Mathf.Pow(t, 0.25f);

        spawnInterval = Mathf.Lerp(initialSpawnInterval, minSpawnInterval, curvedT);

        timer += Time.deltaTime;

        if (timer >= spawnInterval)
        {
            int enemiesToSpawn = UnityEngine.Random.Range(1, 4); // 1,2 o 3
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
            Debug.LogWarning("EnemySpawner: collisionsParent no configurado o vacío. No se spawnea.");
            return;
        }

        Transform spawnPoint = collisionsParent.GetChild(UnityEngine.Random.Range(0, collisionsParent.childCount));

        float t = Mathf.Clamp01(elapsedTime / scaleLerpDuration);
        float minScale = Mathf.Lerp(startScaleRange.x, endScaleRange.x, t);
        float maxScale = Mathf.Lerp(startScaleRange.y, endScaleRange.y, t);

        float randomScale = UnityEngine.Random.Range(minScale, maxScale);

        // Normalizado para color/velocidad (evita NaN si rangos inválidos)
        float denom = Mathf.Max(1e-6f, (startScaleRange.y - endScaleRange.x));
        float normScale = Mathf.InverseLerp(endScaleRange.x, startScaleRange.y, randomScale);

        GameObject prefabToSpawn;

        if (elapsedTime < scaleLerpDuration * 0.5f)
        {
            prefabToSpawn = enemyStage1Prefab;
        }
        else if (elapsedTime < scaleLerpDuration * 0.85f)
        {
            prefabToSpawn = (UnityEngine.Random.value < 0.6f) ? enemyStage1Prefab : enemyStage2Prefab;
        }
        else
        {
            float rand = UnityEngine.Random.value;
            if (rand < 0.4f)
                prefabToSpawn = enemyStage3Prefab;
            else if (rand < 0.7f)
                prefabToSpawn = enemyStage2Prefab;
            else
                prefabToSpawn = enemyStage1Prefab;
        }

        if (prefabToSpawn == null) return;

        GameObject clone = Instantiate(prefabToSpawn, spawnPoint.position, Quaternion.identity);
        clone.SetActive(true);
        clone.transform.localScale = new Vector3(randomScale, randomScale, 1f);

        SpriteRenderer sr = clone.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.color = Color.Lerp(Color.black, Color.red, normScale);
        }

        // --- Cambio: evitar dependencia de tipo HomingEnemy ---
        // Buscamos cualquier MonoBehaviour que tenga campos "maxSpeed" (float) y "collisionsParent" (Transform)
        MonoBehaviour[] monos = clone.GetComponents<MonoBehaviour>();
        foreach (MonoBehaviour mb in monos)
        {
            if (mb == null) continue;
            Type ttype = mb.GetType();

            // Buscamos campo o propiedad 'maxSpeed' (campo público o propiedad)
            FieldInfo fMax = ttype.GetField("maxSpeed", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            PropertyInfo pMax = fMax == null ? ttype.GetProperty("maxSpeed", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) : null;

            // Buscamos campo 'collisionsParent' (Transform)
            FieldInfo fParent = ttype.GetField("collisionsParent", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            PropertyInfo pParent = fParent == null ? ttype.GetProperty("collisionsParent", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) : null;

            if (fMax != null || pMax != null)
            {
                float computedSpeed = Mathf.Lerp(15f, 10f, normScale);
                try
                {
                    if (fMax != null && fMax.FieldType == typeof(float))
                        fMax.SetValue(mb, computedSpeed);
                    else if (pMax != null && pMax.PropertyType == typeof(float) && pMax.CanWrite)
                        pMax.SetValue(mb, computedSpeed, null);

                    if (fParent != null && fParent.FieldType == typeof(Transform))
                        fParent.SetValue(mb, collisionsParent);
                    else if (pParent != null && pParent.PropertyType == typeof(Transform) && pParent.CanWrite)
                        pParent.SetValue(mb, collisionsParent, null);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"EnemySpawner: error al asignar campos por reflexión a {ttype.Name}: {ex.Message}");
                }

                // rompemos porque ya encontramos un candidato válido
                break;
            }
        }
    }
}
