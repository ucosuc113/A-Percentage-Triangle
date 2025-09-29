using UnityEngine;

public class ArcherEnemy : MonoBehaviour
{
    [Header("Referencias")]
    public Transform player;
    private Transform[] tipo1Enemies; // Enemigos tipo 1 encontrados automáticamente
    public ArcherEnemy[] otherArchers; // Otros arqueros tipo 2, asignar manualmente
    public GameObject arrowPrefab;
    public Transform shootPoint;

    [Header("Configuración")]
    public float minDistanceFromPlayer = 5f;
    public float maxDistanceFromPlayer = 10f;
    public float minDistanceFromTipo1 = 3f;
    public float moveSpeed = 3f;
    public float shootingCooldown = 2.2f;

    private Vector2 targetPosition;
    private float cooldownTimer = 0f;
    private bool canShoot = true;

    void Start()
    {
        if (player == null)
        {
            GameObject foundPlayer = GameObject.FindGameObjectWithTag("Player");
            if (foundPlayer != null)
                player = foundPlayer.transform;
            else
                Debug.LogWarning("No se encontró el objeto con la etiqueta 'Player'.");
        }

        // Buscar enemigos tipo 1 por etiqueta "Villain"
        GameObject[] villains = GameObject.FindGameObjectsWithTag("Villain");
        tipo1Enemies = new Transform[villains.Length];
        for (int i = 0; i < villains.Length; i++)
        {
            tipo1Enemies[i] = villains[i].transform;
        }

        SetInitialPosition();
    }

    void Update()
    {
        cooldownTimer -= Time.deltaTime;

        if (!canShoot && cooldownTimer <= 0f)
        {
            canShoot = true;
            SetNewPosition();
        }

        MoveTowardsTarget();

        if (canShoot && Vector2.Distance(transform.position, targetPosition) < 0.2f)
        {
            Shoot();
        }

        // Ya no rota constantemente hacia el jugador en Update para que el apuntado sea fijo
    }

    void SetInitialPosition()
    {
        if (player == null) return;

        Vector2 dirToPlayer = ((Vector2)transform.position - (Vector2)player.position).normalized;
        float distance = Random.Range(minDistanceFromPlayer, maxDistanceFromPlayer);
        targetPosition = (Vector2)player.position + dirToPlayer * distance;

        // Evitar que esté muy cerca de enemigos tipo 1
        foreach (Transform tipo1 in tipo1Enemies)
        {
            if (Vector2.Distance(targetPosition, tipo1.position) < minDistanceFromTipo1)
            {
                targetPosition += (targetPosition - (Vector2)tipo1.position).normalized * minDistanceFromTipo1;
            }
        }

        transform.position = targetPosition;
    }

    void SetNewPosition()
    {
        if (player == null) return;

        Vector2 basePosition = (Vector2)player.position;
        float yOffset = 0f;

        if (otherArchers != null && otherArchers.Length > 1)
        {
            int index = System.Array.IndexOf(otherArchers, this);
            yOffset = (index - otherArchers.Length / 2) * minDistanceFromTipo1;
        }

        Vector2 dirToPlayer = ((Vector2)transform.position - basePosition).normalized;
        float distance = Random.Range(minDistanceFromPlayer, maxDistanceFromPlayer);

        Vector2 newPos = basePosition + dirToPlayer * distance + new Vector2(0, yOffset);

        foreach (Transform tipo1 in tipo1Enemies)
        {
            if (Vector2.Distance(newPos, tipo1.position) < minDistanceFromTipo1)
            {
                newPos += (newPos - (Vector2)tipo1.position).normalized * minDistanceFromTipo1;
            }
        }

        targetPosition = newPos;
    }

    void MoveTowardsTarget()
    {
        Vector2 currentPos = transform.position;
        if (Vector2.Distance(currentPos, targetPosition) > 0.1f)
        {
            Vector2 moveDir = (targetPosition - currentPos).normalized;
            transform.position = Vector2.MoveTowards(currentPos, targetPosition, moveSpeed * Time.deltaTime);
        }
    }

    void Shoot()
    {
        if (player != null)
        {
            // Apuntar justo al momento de disparar
            Vector2 dirToPlayer = (player.position - transform.position).normalized;
            float angle = Mathf.Atan2(dirToPlayer.y, dirToPlayer.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }

        if (arrowPrefab != null && shootPoint != null)
        {
            Instantiate(arrowPrefab, shootPoint.position, transform.rotation);
        }
        else
        {
            Debug.LogWarning("Falta asignar arrowPrefab o shootPoint en ArcherEnemy.");
        }

        canShoot = false;
        cooldownTimer = shootingCooldown;
    }
}
