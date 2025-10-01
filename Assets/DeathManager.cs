using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class DeathManager : MonoBehaviour
{
    public static DeathManager Instance { get; private set; }

    [Header("Configuración del efecto de muerte")]
    public GameObject deathEffectPrefab;
    public float effectDuration = 0.5f;

    [Header("Referencias a objetos adicionales a eliminar")]
    public GameObject additionalObjectToDestroy1;
    public GameObject additionalObjectToDestroy2;
    public GameObject additionalObjectToDestroy3;

    private bool playerIsDead = false;

    // --- Nueva propiedad pública para comprobar desde otros scripts ---
    public bool IsPlayerDead
    {
        get { return playerIsDead; }
    }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            Debug.Log("[DeathManager] Instance asignada a: " + gameObject.name);
        }
        else
        {
            Debug.LogWarning("[DeathManager] Ya existe una instancia. Destruyendo duplicado: " + gameObject.name);
            Destroy(gameObject);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (playerIsDead) return;

        GameObject otherObj = other.gameObject;
        bool byTag = otherObj.CompareTag("EnemyProjectile");
        bool byComponent = otherObj.GetComponent<EnemyProjectile>() != null || otherObj.GetComponentInParent<EnemyProjectile>() != null;
        bool byName = otherObj.name.Contains("BulletEnemy") || otherObj.name.Contains("Bullet") || otherObj.name.Contains("BulletEnemy(Clone)");

        if (byTag || byComponent || byName)
        {
            KillPlayer(gameObject);
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (playerIsDead) return;

        GameObject otherObj = collision.gameObject;
        bool byTag = otherObj.CompareTag("EnemyProjectile");
        bool byComponent = otherObj.GetComponent<EnemyProjectile>() != null || otherObj.GetComponentInParent<EnemyProjectile>() != null;
        bool byName = otherObj.name.Contains("BulletEnemy") || otherObj.name.Contains("Bullet") || otherObj.name.Contains("BulletEnemy(Clone)");

        if (byTag || byComponent || byName)
        {
            KillPlayer(gameObject);
        }
    }

    public void KillPlayer(GameObject player)
    {
        if (player == null) return;
        if (playerIsDead) return;

        playerIsDead = true;
        Debug.Log($"[DeathManager] KillPlayer ejecutado para {player.name}");

        Vector3 pos = player.transform.position;
        Quaternion rot = player.transform.rotation;

        DisablePlayerBeforeDestroy(player);

        if (deathEffectPrefab != null)
        {
            GameObject effectInstance = Instantiate(deathEffectPrefab, pos, rot);
            ParticleSystem ps = effectInstance.GetComponent<ParticleSystem>();
            if (ps != null) ps.Play();
            Destroy(effectInstance, effectDuration + 0.5f);
        }

        Destroy(player);

        // 🔥 Ahora soporta 3 objetos adicionales
        if (additionalObjectToDestroy1 != null) Destroy(additionalObjectToDestroy1);
        if (additionalObjectToDestroy2 != null) Destroy(additionalObjectToDestroy2);
        if (additionalObjectToDestroy3 != null) Destroy(additionalObjectToDestroy3);

        // 🔥 Reinicio forzado a los 6 segundos, pase lo que pase
        StartCoroutine(RestartSceneAfter(6f));
    }

    private IEnumerator RestartSceneAfter(float seconds)
    {
        yield return new WaitForSeconds(seconds);

        Scene current = SceneManager.GetActiveScene();
        SceneManager.LoadScene(current.buildIndex);
    }

    private void DisablePlayerBeforeDestroy(GameObject player)
    {
        var behaviours = player.GetComponentsInChildren<Behaviour>();
        foreach (var b in behaviours)
        {
            if (b == this) continue;
            b.enabled = false;
        }

        var cols = player.GetComponentsInChildren<Collider2D>();
        foreach (var c in cols) c.enabled = false;

        var rbs = player.GetComponentsInChildren<Rigidbody2D>();
        foreach (var rb in rbs) rb.isKinematic = true;
    }
}
