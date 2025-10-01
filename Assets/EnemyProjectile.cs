using UnityEngine;

public class EnemyProjectile : MonoBehaviour
{
    public float speed = 15f;
    private Vector2 direction;
    private Rigidbody2D rb;

    [Header("Collision settings")]
    public string playerTag = "Player"; // Tag del jugador
    public float spawnInvulnerability = 0.05f;
    private float spawnTime;

    [Header("Optional fallback DeathManager")]
    public DeathManager playerDeathManager;

    [Header("Parent whose children should destroy the projectile (but not the child)")]
    public GameObject parentTargetToDestroyChildren;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null) Debug.LogWarning("EnemyProjectile: Rigidbody2D no encontrado.");
        spawnTime = Time.time;
    }

    public void SetDirection(Vector2 dir) => direction = dir.normalized;
    public void SetSpeed(float newSpeed) => speed = newSpeed;

    void FixedUpdate()
    {
        if (rb != null) rb.linearVelocity = direction * speed;
        else transform.position += (Vector3)(direction * speed * Time.fixedDeltaTime);
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        // Respeta invulnerabilidad al spawn
        if (Time.time - spawnTime < spawnInvulnerability) return;

        // Ignorar otros proyectiles (siempre)
        if (collision.GetComponent<EnemyProjectile>() != null || collision.GetComponentInParent<EnemyProjectile>() != null)
            return;

        Transform hitT = collision.transform;

        // 1) Si es hijo del parentTargetToDestroyChildren -> DESTRUIR SÓLO LA BALA (NO destruir el hijo)
        if (parentTargetToDestroyChildren != null)
        {
            Transform parentT = parentTargetToDestroyChildren.transform;
            if (hitT != parentT && hitT.IsChildOf(parentT))
            {
                Debug.Log($"EnemyProjectile: Impacto en hijo {collision.name} de {parentTargetToDestroyChildren.name} -> destruyo SOLO la bala.");
                Destroy(gameObject); // solo la bala
                return;
            }
        }

        // 2) Si es el jugador -> ejecutar DeathManager (si existe) y destruir la bala
        if (collision.CompareTag(playerTag))
        {
            Debug.Log($"EnemyProjectile: Impacto en Player -> {collision.name}");
            DeathManager dm = collision.GetComponent<DeathManager>() ?? collision.GetComponentInParent<DeathManager>() ?? playerDeathManager;
            if (dm != null)
            {
                ForceExecuteDeathManager(dm);
            }
            else
            {
                Debug.LogWarning("EnemyProjectile: No se encontró DeathManager en el Player ni fallback en inspector.");
            }

            Destroy(gameObject);
            return;
        }

        // 3) Si llega aquí: no es ni Player ni hijo del prefab -> IGNORAR (la bala atraviesa)
        Debug.Log($"EnemyProjectile: Colisión ignorada con {collision.name} (tag={collision.tag}, layer={LayerMask.LayerToName(collision.gameObject.layer)})");
    }

    void ForceExecuteDeathManager(DeathManager dm)
    {
        var mb = dm as MonoBehaviour;
        if (mb == null)
        {
            Debug.LogWarning("EnemyProjectile: DeathManager no es un MonoBehaviour válido.");
            return;
        }

        bool wasEnabled = mb.enabled;
        mb.enabled = false;
        mb.enabled = true;
        Debug.Log("EnemyProjectile: Forzado ciclo de vida en DeathManager (toggle enabled). Estado previo: " + wasEnabled);
    }
}
