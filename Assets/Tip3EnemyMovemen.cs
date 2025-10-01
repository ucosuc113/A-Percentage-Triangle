// File: ExploderEnemy_Music.cs
using UnityEngine;
using System;
using System.Collections;

/// <summary>
/// ExploderEnemy (tipo 3) con reproducción de música cada vez que dispara
/// y opción de spawn secuencial en espiral (configurable desde inspector).
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class ExploderEnemy : MonoBehaviour
{
    [Header("Referencias")]
    public Transform player;
    [Tooltip("Prefab de proyectil (usado por comportamiento por defecto/legacy)")]
    public GameObject bulletPrefab;
    public Transform spawnParent;

    [Header("Disparo")]
    public int projectilesCount = 9;
    public float bulletSpeed = 4f;
    public float shootInterval = 5f;

    [Header("Spawn secuencial (espiral)")]
    [Tooltip("Si true: los proyectiles salen uno por uno con retraso (espiral). Si false: todos al instante.")]
    public bool useSequentialSpawn = true;
    [Tooltip("Segundos entre cada proyectil cuando useSequentialSpawn==true")] 
    public float delayBetweenProjectiles = 0.1f;
    [Tooltip("Grados extra por proyectil para generar giro/espiral")]
    public float spinDegreesPerProjectile = 10f;

    [Header("Movimiento / distancia")]
    public float preferredMinDistance = 3.5f;
    public float preferredMaxDistance = 6.5f;
    public float moveSpeed = 3.5f;
    public float turnSpeed = 8f;
    public float wanderIntensity = 0.6f;

    [Header("Colisión")]
    public LayerMask obstacleMask;

    [Header("Debug / Gizmos")]
    public bool debugDraw = false;

    [Header("Control de spawn")]
    [Tooltip("Si true: intentará usar RequestProjectileSpawn antes de instanciar. Si false: instancia localmente (default).")]
    public bool useExternalSpawner = false; // <- POR DEFECTO instanciar localmente

    /// <summary>
    /// Delegado opcional para que un sistema externo maneje la creación de proyectiles.
    /// Signature: (prefab, position, velocity, rotationDegrees)
    /// </summary>
    [NonSerialized] public Action<GameObject, Vector3, Vector2, float> RequestProjectileSpawn;

    [Header("Audio")]
    [Tooltip("Clip de música que se reproducirá cada vez que dispare (asignar en inspector)")]
    public AudioClip musicClip;
    [Range(0f, 1f)] public float musicVolume = 1f;
    [Tooltip("Si true: usa PlayClipAtPoint (fuente temporal 3D). Si false: usa AudioSource local.")]
    public bool playMusicAtPoint = false;
    [Tooltip("Si true: usa PlayOneShot; si false y loopMusic==false, usa AudioSource.Play() con clip (reemplaza y reproduce).")]
    public bool playMusicAsOneShot = true;
    [Tooltip("Si true: reproducirá el clip en loop usando AudioSource (reinicia en cada disparo).")]
    public bool loopMusic = false;

    // Internals
    Rigidbody2D rb;
    float shootTimer = 0f;
    Vector2 moveVelocity = Vector2.zero;

    // Audio
    AudioSource audioSource;
    Coroutine spawnCoroutine;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.loop = false;
            audioSource.spatialBlend = 0f; // 2D por defecto
        }
    }

    void Start()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }
        shootTimer = shootInterval * UnityEngine.Random.Range(0.2f, 1.0f);
    }

    void Update()
    {
        if (player == null) return;

        shootTimer -= Time.deltaTime;
        if (shootTimer <= 0f)
        {
            PlayMusicOnShoot();
            ShootRadialBurst();
            shootTimer = shootInterval;
        }

        if (debugDraw)
        {
            Debug.DrawLine(transform.position, player.position, Color.magenta);
        }
    }

    void FixedUpdate()
    {
        if (player == null) return;

        Vector2 pos = transform.position;
        Vector2 ppos = player.position;
        Vector2 toPlayer = ppos - pos;
        float dist = toPlayer.magnitude;

        Vector2 desired = Vector2.zero;

        if (dist < preferredMinDistance)
            desired = (pos - ppos).normalized * moveSpeed;
        else if (dist > preferredMaxDistance)
            desired = (ppos - pos).normalized * moveSpeed;
        else
        {
            Vector2 perp = Vector2.Perpendicular(toPlayer).normalized;
            float side = Mathf.PerlinNoise(Time.time * 0.5f, transform.position.x * 0.1f) * 2f - 1f;
            desired = perp * side * (moveSpeed * wanderIntensity);

            float centerBias = (dist - (preferredMinDistance + preferredMaxDistance) * 0.5f) * 0.3f;
            desired += (toPlayer.normalized * -centerBias);
        }

        if (obstacleMask != 0 && desired.sqrMagnitude > 0.001f)
        {
            RaycastHit2D hit = Physics2D.Raycast(transform.position, desired.normalized, Mathf.Max(0.5f, desired.magnitude * Time.fixedDeltaTime * 2f), obstacleMask);
            if (hit.collider != null) desired = Vector2.zero;
        }

        moveVelocity = Vector2.Lerp(moveVelocity, desired, Time.fixedDeltaTime * 6f);
        rb.MovePosition(rb.position + moveVelocity * Time.fixedDeltaTime);

        Vector2 look = (player.position - transform.position).normalized;
        if (look.sqrMagnitude > 0.0001f)
        {
            float targetAngle = Mathf.Atan2(look.y, look.x) * Mathf.Rad2Deg;
            float curAngle = transform.eulerAngles.z;
            float newAngle = Mathf.LerpAngle(curAngle, targetAngle, Time.fixedDeltaTime * turnSpeed);
            transform.rotation = Quaternion.Euler(0f, 0f, newAngle);
        }
    }

    void PlayMusicOnShoot()
    {
        if (musicClip == null) return;

        if (playMusicAtPoint)
        {
            AudioSource.PlayClipAtPoint(musicClip, transform.position, musicVolume);
            return;
        }

        if (loopMusic)
        {
            audioSource.Stop();
            audioSource.clip = musicClip;
            audioSource.loop = true;
            audioSource.volume = musicVolume;
            audioSource.Play();
            return;
        }

        if (playMusicAsOneShot)
        {
            audioSource.PlayOneShot(musicClip, musicVolume);
        }
        else
        {
            audioSource.Stop();
            audioSource.clip = musicClip;
            audioSource.loop = false;
            audioSource.volume = musicVolume;
            audioSource.Play();
        }
    }

    void ShootRadialBurst()
    {
        // Si ya hay una coroutine corriendo, detenerla (evita solapamientos)
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }

        if (!useSequentialSpawn)
        {
            // comportamiento instantáneo (legacy)
            int count = Mathf.Max(1, projectilesCount);
            float stepAngle = 360f / count;
            float initialOffset = UnityEngine.Random.Range(0f, stepAngle);

            for (int i = 0; i < count; i++)
            {
                float angle = initialOffset + i * stepAngle;
                SpawnSingleBullet(angle);
            }

            return;
        }

        // spawn secuencial en espiral
        int total = Mathf.Max(1, projectilesCount);
        float baseStep = 360f / total;
        spawnCoroutine = StartCoroutine(SpawnBurstCoroutine(total, baseStep));
    }

    IEnumerator SpawnBurstCoroutine(int count, float stepAngle)
    {
        float initialOffset = UnityEngine.Random.Range(0f, stepAngle);

        for (int i = 0; i < count; i++)
        {
            float angle = initialOffset + i * (stepAngle + spinDegreesPerProjectile);
            SpawnSingleBullet(angle);

            // esperar antes del siguiente proyectil
            yield return new WaitForSeconds(Mathf.Max(0f, delayBetweenProjectiles));
        }

        spawnCoroutine = null;
    }

    void SpawnSingleBullet(float angleDegrees)
    {
        float rad = angleDegrees * Mathf.Deg2Rad;
        Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)).normalized;
        Vector2 velocity = dir * bulletSpeed;
        float rot = angleDegrees;

        // Si se delega al sistema externo
        if (useExternalSpawner && RequestProjectileSpawn != null)
        {
            try
            {
                RequestProjectileSpawn.Invoke(bulletPrefab, transform.position, velocity, rot);
                return;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"ExploderEnemy: excepción en RequestProjectileSpawn: {ex.Message}. Haciendo fallback a Instantiate.");
                // caer al instanciar localmente abajo
            }
        }

        // Fallback / instanciado local
        if (bulletPrefab == null) return;

        GameObject b = Instantiate(bulletPrefab, transform.position, Quaternion.Euler(0f, 0f, rot));
        if (spawnParent != null) b.transform.SetParent(spawnParent, true);

        Collider2D myCol = GetComponent<Collider2D>();
        Collider2D bcol = b.GetComponent<Collider2D>();
        if (myCol != null && bcol != null)
            Physics2D.IgnoreCollision(myCol, bcol);

        var enemyProj = b.GetComponent<EnemyProjectile>();
        if (enemyProj != null) { enemyProj.SetDirection(dir); enemyProj.SetSpeed(bulletSpeed); return; }

        var proj = b.GetComponent<Projectile>();
        if (proj != null) { proj.SetDirection(dir); proj.SetSpeed(bulletSpeed); return; }

        var pInst = b.GetComponent<ProjectileInstance>();
        if (pInst != null) { pInst.Initialize(velocity); return; }

        Rigidbody2D brb = b.GetComponent<Rigidbody2D>();
        if (brb != null) brb.linearVelocity = velocity;
    }

    void OnDrawGizmosSelected()
    {
        if (!debugDraw) return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, preferredMinDistance);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, preferredMaxDistance);
    }

    void OnDestroy()
    {
        if (spawnCoroutine != null) StopCoroutine(spawnCoroutine);
        if (audioSource != null && audioSource.isPlaying) audioSource.Stop();
    }
}
