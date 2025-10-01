using UnityEngine;

/// <summary>
/// ArcherEnemy - comportamiento agresivo continuo:
/// - Siempre apunta al jugador (puntería predictiva).
/// - Se reposiciona periódicamente (repositionIntervalMin/Max).
/// - Se reposiciona también tras disparar.
/// - Opcional: permite disparar mientras se desplaza.
/// - Instancia clones de flechas y evita colisiones own-arrow.
/// - Ahora reproduce un sonido al disparar.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class ArcherEnemy : MonoBehaviour
{
    [Header("Referencias")]
    public Transform player;
    public string playerTag = "Player";
    public Transform shootPoint;
    public GameObject arrowPrefab;
    public LayerMask layerMaskObstacles;

    [Header("Posicionamiento (más cercano por defecto)")]
    public float minDistanceFromPlayer = 2.5f;
    public float maxDistanceFromPlayer = 5f;
    public float moveSpeed = 3.0f;
    public float arriveThreshold = 0.12f;
    public float separationDistance = 1.2f;
    public ArcherEnemy[] otherArchers;

    [Header("Reposition (constantly)")]
    [Tooltip("Tiempo mínimo entre reposicionamientos (segundos)")]
    public float repositionIntervalMin = 1.0f;
    [Tooltip("Tiempo máximo entre reposicionamientos (segundos)")]
    public float repositionIntervalMax = 3.0f;

    [Header("Disparo / puntería")]
    public float shootingCooldown = 1.1f;
    public float aimSmoothing = 10f;
    [Range(0f, 6f)] public float aimError = 0.04f;
    public float maxLeadTime = 2.0f;
    [Tooltip("Si arrowPrefab NO tiene ProjectileInfo, usar esta velocidad (u/s).")]
    public float fallbackProjectileSpeed = 14f;
    [Tooltip("Si true, puede disparar mientras se mueve; si false, dispara solo al llegar al objetivo")]
    public bool allowShootWhileMoving = false;

    [Header("Audio")]
    [Tooltip("Clip que se reproduce al disparar (asignar en el inspector)")]
    public AudioClip shootClip;
    [Range(0f, 1f)] public float shootVolume = 1f;
    [Tooltip("Si es true, usa PlayClipAtPoint (crea fuente temporal en posición). Si false, usa AudioSource del mismo GameObject.")]
    public bool playSoundAtPoint = false;

    [Header("Debug")]
    public bool debugDraw = false;
    public bool verboseWarnings = true;

    // Internals
    Rigidbody2D rb;
    Vector2 targetPosition;
    float cooldownTimer = 0f;
    bool canShoot = true;
    Vector2 lastPlayerPos;
    Vector2 estimatedPlayerVelocity;

    float repositionTimer = 0f;

    const float arriveEpsilon = 0.025f;
    bool warnedArrowMissing = false;
    bool warnedPlayerMissing = false;

    // Audio source (opcional)
    AudioSource audioSource;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null) Debug.LogError("ArcherEnemy necesita Rigidbody2D.");

        // intentar obtener AudioSource; si no existe lo añadimos (config por defecto)
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.loop = false;
            // Para 2D: spatialBlend = 0; si quieres 3D, cámbialo a 1 desde inspector o desde código
            audioSource.spatialBlend = 0f;
        }
    }

    void Start()
    {
        // Buscar player si no asignado
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag(playerTag);
            if (p != null) player = p.transform;
        }

        if (player == null)
        {
            if (verboseWarnings && !warnedPlayerMissing) { Debug.LogWarning("ArcherEnemy: No se encontró 'Player' (tag). Asigna 'player' en el inspector."); warnedPlayerMissing = true; }
            targetPosition = rb != null ? rb.position : (Vector2)transform.position;
        }
        else
        {
            lastPlayerPos = (Vector2)player.position;
            // Primer reposicionamiento ya
            SetNewPosition();
        }

        // Inicializar timer de reposición aleatorio
        ResetRepositionTimer();

        if (rb != null) rb.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    void Update()
    {
        // timers
        cooldownTimer -= Time.deltaTime;
        repositionTimer -= Time.deltaTime;

        // actualizar estimación de velocidad del player para puntería predictiva
        UpdateEstimatedPlayerVelocity();

        // siempre apunta al jugador (predicción), aun mientras se mueve
        AimContinuously();

        // si se acabó el timer, pide nueva posición
        if (repositionTimer <= 0f)
        {
            SetNewPosition();
            ResetRepositionTimer();
        }

        // Si llegamos al target y podemos disparar, o si está permitido disparar mientras se mueve
        bool atTarget = Vector2.Distance((Vector2)transform.position, targetPosition) <= arriveThreshold + arriveEpsilon;
        if (canShoot && cooldownTimer <= 0f && (atTarget || allowShootWhileMoving))
        {
            TryShoot();
        }

        if (debugDraw)
        {
            Debug.DrawLine(transform.position, (Vector3)targetPosition, Color.yellow);
            if (shootPoint != null && player != null) Debug.DrawLine(shootPoint.position, player.position, Color.cyan);
        }
    }

    void FixedUpdate()
    {
        MoveTowardsTargetFixed();
    }

    void ResetRepositionTimer()
    {
        repositionTimer = Random.Range(repositionIntervalMin, repositionIntervalMax);
    }

    void SetNewPosition()
    {
        if (player == null) return;

        Vector2 basePos = (Vector2)player.position;
        Vector2 dir = ((Vector2)transform.position - basePos);
        if (dir.sqrMagnitude < 0.0001f) dir = Random.insideUnitCircle.normalized;
        dir.Normalize();

        Vector2 candidate = Vector2.zero;
        int attempts = 0;
        const int MAX_ATTEMPTS = 8;
        do
        {
            float dist = Random.Range(minDistanceFromPlayer, maxDistanceFromPlayer);
            // variar un poco la dirección para distribuir mejor
            float angleOff = Random.Range(-30f, 30f) * Mathf.Deg2Rad;
            Vector2 dirOff = new Vector2(Mathf.Cos(angleOff) * dir.x - Mathf.Sin(angleOff) * dir.y,
                                         Mathf.Sin(angleOff) * dir.x + Mathf.Cos(angleOff) * dir.y).normalized;
            candidate = basePos + dirOff * dist;
            attempts++;
        } while (attempts < MAX_ATTEMPTS && Vector2.Distance(candidate, (Vector2)transform.position) < 0.25f);

        targetPosition = DistributeWithOtherArchers(candidate);
    }

    Vector2 DistributeWithOtherArchers(Vector2 desired)
    {
        if (otherArchers != null && otherArchers.Length > 0)
        {
            int idx = System.Array.IndexOf(otherArchers, this);
            if (idx >= 0)
            {
                float spread = (idx - (otherArchers.Length - 1) / 2f) * separationDistance;
                desired += new Vector2(0f, spread);
            }
            else
            {
                int pseudo = Mathf.Abs(gameObject.GetInstanceID()) % Mathf.Max(1, otherArchers.Length);
                float spread = (pseudo - (otherArchers.Length - 1) / 2f) * separationDistance;
                desired += new Vector2(0f, spread);
            }
        }
        return desired;
    }

    void MoveTowardsTargetFixed()
    {
        if (rb == null) return;

        Vector2 current = rb.position;
        float dist = Vector2.Distance(current, targetPosition);

        if (dist > arriveThreshold + arriveEpsilon)
        {
            Vector2 next = Vector2.MoveTowards(current, targetPosition, moveSpeed * Time.fixedDeltaTime);
            rb.MovePosition(next);
        }
        else
        {
            if ((Vector2)rb.position != targetPosition) rb.MovePosition(targetPosition);
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
    }

    void UpdateEstimatedPlayerVelocity()
    {
        if (player == null) return;
        Rigidbody2D prb = player.GetComponent<Rigidbody2D>();
        if (prb != null)
        {
            estimatedPlayerVelocity = prb.linearVelocity;
        }
        else
        {
            Vector2 current = (Vector2)player.position;
            float dt = Mathf.Max(Time.deltaTime, 1e-6f);
            Vector2 rawVel = (current - lastPlayerPos) / dt;
            estimatedPlayerVelocity = Vector2.Lerp(estimatedPlayerVelocity, rawVel, 0.45f);
            lastPlayerPos = current;
        }
    }

    void AimContinuously()
    {
        if (player == null || shootPoint == null) return;

        // obtener velocidad del proyectil (fallback si no hay ProjectileInfo)
        float projectileSpeed = fallbackProjectileSpeed;
        ProjectileInfo info = arrowPrefab != null ? arrowPrefab.GetComponent<ProjectileInfo>() : null;
        if (info != null) projectileSpeed = info.speed;

        Vector2 shootPos = (Vector2)shootPoint.position;
        Vector2 targetPos = (Vector2)player.position;

        bool gotIntercept;
        Vector2 interceptPoint = CalculateInterceptPoint(shootPos, projectileSpeed, targetPos, estimatedPlayerVelocity, out gotIntercept);
        Vector2 aimPoint = gotIntercept ? interceptPoint : targetPos;
        Vector2 dir = aimPoint - shootPos;
        if (dir.sqrMagnitude < 0.0001f) return;
        dir.Normalize();

        float errorAngle = Random.Range(-aimError, aimError);
        float desiredDeg = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + errorAngle * Mathf.Rad2Deg;
        Quaternion desiredRotation = Quaternion.Euler(0f, 0f, desiredDeg);

        // toujours: suavizado para evitar "tirones" bruscos de la cabeza del arquero
        transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, Time.deltaTime * aimSmoothing);
    }

    void TryShoot()
    {
        if (!canShoot) return;
        if (player == null) { if (!warnedPlayerMissing && verboseWarnings) { Debug.LogWarning("ArcherEnemy: player ausente, no se puede disparar."); warnedPlayerMissing = true; } return; }
        if (arrowPrefab == null) { if (!warnedArrowMissing && verboseWarnings) { Debug.LogWarning("ArcherEnemy: arrowPrefab no asignado."); warnedArrowMissing = true; } return; }
        if (shootPoint == null) { if (!warnedArrowMissing && verboseWarnings) { Debug.LogWarning("ArcherEnemy: shootPoint no asignado."); warnedArrowMissing = true; } return; }

        // velocidad efectiva del proyectil
        float projectileSpeed = fallbackProjectileSpeed;
        ProjectileInfo info = arrowPrefab.GetComponent<ProjectileInfo>();
        if (info != null) projectileSpeed = info.speed;
        if (projectileSpeed <= 0.01f) { Debug.LogError("Projectile speed inválida. Ajusta ProjectileInfo o fallbackProjectileSpeed."); return; }

        Vector2 shootPos = (Vector2)shootPoint.position;
        Vector2 targetPos = (Vector2)player.position;

        bool gotIntercept;
        Vector2 interceptPoint = CalculateInterceptPoint(shootPos, projectileSpeed, targetPos, estimatedPlayerVelocity, out gotIntercept);
        if (!gotIntercept) interceptPoint = targetPos;

        Vector2 dir = interceptPoint - shootPos;
        float dist = dir.magnitude;
        if (dist < 0.001f) dir = ((Vector2)player.position - shootPos).normalized; else dir.Normalize();

        // comprobar línea de tiro
        RaycastHit2D hit = Physics2D.Raycast(shootPos, dir, Mathf.Min(dist, 50f), layerMaskObstacles);
        if (hit.collider != null)
        {
            if (debugDraw) Debug.DrawLine((Vector3)shootPos, hit.point, Color.red, 0.6f);
            // si hay muro, reubicar y no disparar
            SetNewPosition();
            canShoot = false;
            cooldownTimer = 0.35f;
            ResetRepositionTimer();
            return;
        }

        // aplicar error y rotación definitiva justo antes de instanciar
        float errorAngle = Random.Range(-aimError, aimError);
        float angleDeg = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + errorAngle * Mathf.Rad2Deg;
        Quaternion desiredRotation = Quaternion.Euler(0f, 0f, angleDeg);
        transform.rotation = desiredRotation;

        // clonar la bala (Instantiate)
        GameObject arrowClone = Instantiate(arrowPrefab, (Vector3)shootPos, desiredRotation);

        // evitar que la flecha empuje al archer
        Collider2D arrowCol = arrowClone.GetComponent<Collider2D>();
        if (arrowCol != null)
        {
            Collider2D[] myCols = GetComponentsInChildren<Collider2D>();
            foreach (Collider2D c in myCols) if (c != null) Physics2D.IgnoreCollision(arrowCol, c);
        }

        // ------------- NUEVO: asegurar que la instancia reciba VELOCIDAD y DIRECCIÓN -------------
        ProjectileInstance pi = arrowClone.GetComponent<ProjectileInstance>();
        if (pi != null)
        {
            pi.Initialize(dir * projectileSpeed);
        }
        else
        {
            Projectile pScript = arrowClone.GetComponent<Projectile>();
            if (pScript != null)
            {
                pScript.SetDirection(dir);
                pScript.SetSpeed(projectileSpeed);
            }
            else
            {
                EnemyProjectile ep = arrowClone.GetComponent<EnemyProjectile>();
                if (ep != null)
                {
                    ep.SetDirection(dir);
                    ep.SetSpeed(projectileSpeed);
                }
                else
                {
                    Rigidbody2D arb = arrowClone.GetComponent<Rigidbody2D>();
                    if (arb != null) arb.linearVelocity = dir * projectileSpeed;
                    else
                    {
                        Debug.LogWarning("Arrow clone no tiene ProjectileInstance, Projectile, EnemyProjectile ni Rigidbody2D. No se le asignó velocidad.");
                    }
                }
            }
        }
        // ------------------------------------------------------------------------------------------

        // Reproducir sonido de disparo (si se asignó)
        if (shootClip != null)
        {
            if (playSoundAtPoint)
            {
                AudioSource.PlayClipAtPoint(shootClip, transform.position, shootVolume);
            }
            else if (audioSource != null)
            {
                audioSource.PlayOneShot(shootClip, shootVolume);
            }
        }

        // disparado: setear cooldown y forzar reposicion
        canShoot = false;
        cooldownTimer = shootingCooldown;

        // reposicionar inmediatamente (también hay el repositionTimer periódico)
        SetNewPosition();
        ResetRepositionTimer();
    }


    Vector2 CalculateInterceptPoint(Vector2 shooterPos, float shotSpeed, Vector2 targetPos, Vector2 targetVel, out bool success)
    {
        Vector2 rel = targetPos - shooterPos;
        float a = Vector2.Dot(targetVel, targetVel) - shotSpeed * shotSpeed;
        float b = 2f * Vector2.Dot(rel, targetVel);
        float c = Vector2.Dot(rel, rel);

        float t = 0f;
        if (Mathf.Abs(a) < 0.0001f)
        {
            if (Mathf.Abs(b) < 0.0001f) { success = false; return targetPos; }
            t = -c / b;
            if (t <= 0f) { success = false; return targetPos; }
        }
        else
        {
            float disc = b * b - 4f * a * c;
            if (disc < 0f) { success = false; return targetPos; }
            float sqrtD = Mathf.Sqrt(disc);
            float t1 = (-b + sqrtD) / (2f * a);
            float t2 = (-b - sqrtD) / (2f * a);
            t = Mathf.Infinity;
            if (t1 > 0f) t = Mathf.Min(t, t1);
            if (t2 > 0f) t = Mathf.Min(t, t2);
            if (float.IsInfinity(t)) { success = false; return targetPos; }
        }

        if (t > maxLeadTime) { success = false; return targetPos; }
        success = true;
        return targetPos + targetVel * t;
    }

    void OnDrawGizmosSelected()
    {
        if (!debugDraw) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.18f);
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere((Vector3)targetPosition, 0.12f);
    }
}

// Helpers mínimos si no existen en tu proyecto
public class ProjectileInfo : MonoBehaviour { public float speed = 14f; }

[RequireComponent(typeof(Rigidbody2D))]
public class ProjectileInstance : MonoBehaviour
{
    Rigidbody2D rb;
    public float life = 6f;
    float timer = 0f;
    void Awake() { rb = GetComponent<Rigidbody2D>(); }
    public void Initialize(Vector2 velocity)
    {
        timer = 0f;
        if (rb != null) rb.linearVelocity = velocity;
        gameObject.SetActive(true);
    }
    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= life) Destroy(gameObject);
    }
    void OnTriggerEnter2D(Collider2D col) { Destroy(gameObject); }
}
