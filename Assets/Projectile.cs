using UnityEngine;
using UnityEngine.UI;        // Para UI clásico
using TMPro;                // Para TMP (si usas TMP_Text)

public class Projectile : MonoBehaviour
{
    public float speed = 15f;
    private Vector2 direction;

    public Transform collisionsParent;

    public GameObject hitParticlePrefab;
    public GameObject extraParticlePrefab;

    [Header("UI")]
    public Text scoreText;             // ✅ Para UI clásico (opcional)
    public TMP_Text tmpScoreText;      // ✅ Para TMP (más moderno)

    private static int killCount = 0;  // ✅ Contador estático para acumular kills

    private Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
            Debug.LogWarning("Rigidbody2D no encontrado en el proyectil.");
    }

    public void SetDirection(Vector2 dir)
    {
        direction = dir.normalized;
    }

    void FixedUpdate()
    {
        if (rb != null)
        {
            rb.linearVelocity = direction * speed;
        }
        else
        {
            transform.position += (Vector3)(direction * speed * Time.fixedDeltaTime);
        }
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        Debug.Log($"Proyectil tocó: {collision.gameObject.name}, Tag: {collision.tag}");

        if (collision.CompareTag("Enemy"))
        {
            Debug.Log("Impacto en enemigo");
            DestroyEnemyAndProjectile(collision.gameObject);
        }
        else if (collisionsParent != null && collision.transform.IsChildOf(collisionsParent))
        {
            Debug.Log("Impacto en colisión");
            Destroy(gameObject);
        }
    }

    void DestroyEnemyAndProjectile(GameObject enemy)
    {
        if (hitParticlePrefab != null)
        {
            GameObject particles = Instantiate(hitParticlePrefab, enemy.transform.position, Quaternion.identity);
            Destroy(particles, 0.3f);
        }

        if (extraParticlePrefab != null)
        {
            GameObject extraParticles = Instantiate(extraParticlePrefab, enemy.transform.position, Quaternion.identity);
            Destroy(extraParticles, 0.7f);
        }

        // ✅ Incrementar contador y actualizar texto
        killCount++;

        if (scoreText != null)
            scoreText.text = "" + killCount;

        if (tmpScoreText != null)
            tmpScoreText.text = "" + killCount;

        Destroy(enemy);
        Destroy(gameObject);
    }

    public void SetSpeed(float newSpeed)
    {
        speed = newSpeed;
    }
}
