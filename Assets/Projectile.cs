using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Projectile : MonoBehaviour
{
    public float speed = 15f;
    private Vector2 direction;

    public Transform collisionsParent;

    public GameObject hitParticlePrefab;
    public GameObject extraParticlePrefab;

    [Header("UI")]
    public Text scoreText;
    public TMP_Text tmpScoreText;

    private static int killCount = 0;
    private Rigidbody2D rb;

    [Header("Collision settings")]
    public string targetTag = "Enemy";           // objetivo que este proyectil debe dañar
    public float spawnInvulnerability = 0.05f;  // evitar colisiones justo al instanciar
    private float spawnTime;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
            Debug.LogWarning("Rigidbody2D no encontrado en el proyectil.");

        if (scoreText != null) scoreText.text = killCount.ToString();
        if (tmpScoreText != null) tmpScoreText.text = killCount.ToString();

        spawnTime = Time.time;
    }

    public static void ResetKillCount() => killCount = 0;

    public void SetDirection(Vector2 dir) => direction = dir.normalized;

    void FixedUpdate()
    {
        if (rb != null) rb.linearVelocity = direction * speed;
        else transform.position += (Vector3)(direction * speed * Time.fixedDeltaTime);
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (Time.time - spawnTime < spawnInvulnerability) return;

        // Ignorar si el otro es cualquier proyectil (este o enemigo) incluso si está en parent/child
        if (IsOtherProjectile(collision))
        {
            Debug.Log("Projectile: ignorando colisión con otro proyectil -> " + collision.gameObject.name);
            return;
        }

        Debug.Log($"Projectile tocó: {collision.gameObject.name}, Tag: {collision.tag}");

        if (collision.CompareTag(targetTag))
        {
            Debug.Log("Impacto en enemigo (targetTag matched)");
            DestroyTargetAndSelf(collision.gameObject);
            return;
        }

        if (collisionsParent != null && collision.transform.IsChildOf(collisionsParent))
        {
            Debug.Log("Impacto en colisión del entorno -> destruir proyectil.");
            Destroy(gameObject);
            return;
        }
    }

    bool IsOtherProjectile(Collider2D col)
    {
        // Busca ambos tipos en self/parents/children
        if (col.GetComponent<Projectile>() != null) return true;
        if (col.GetComponentInParent<Projectile>() != null) return true;
        if (col.GetComponentInChildren<Projectile>() != null) return true;

        // EnemyProjectile también debe ignorarse
        var enemyProj = col.GetComponentInParent<EnemyProjectile>();
        if (enemyProj != null) return true;
        enemyProj = col.GetComponentInChildren<EnemyProjectile>();
        if (enemyProj != null) return true;
        if (col.GetComponent<EnemyProjectile>() != null) return true;

        return false;
    }

    void DestroyTargetAndSelf(GameObject target)
    {
        if (hitParticlePrefab != null)
        {
            GameObject particles = Instantiate(hitParticlePrefab, target.transform.position, Quaternion.identity);
            Destroy(particles, 0.3f);
        }
        if (extraParticlePrefab != null)
        {
            GameObject extraParticles = Instantiate(extraParticlePrefab, target.transform.position, Quaternion.identity);
            Destroy(extraParticles, 0.7f);
        }

        killCount++;
        if (scoreText != null) scoreText.text = killCount.ToString();
        if (tmpScoreText != null) tmpScoreText.text = killCount.ToString();

        Destroy(target); // idealmente reemplazar por ApplyDamage a un Health component
        Destroy(gameObject);
    }

    public void SetSpeed(float newSpeed) => speed = newSpeed;
}
