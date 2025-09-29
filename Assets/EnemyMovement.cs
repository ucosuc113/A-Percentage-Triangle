using UnityEngine;

public class EnemyMovement : MonoBehaviour
{
    [Header("Referencias")]
    public Transform player;

    [Header("Movimiento")]
    public float acceleration = 5f;
    public float maxSpeed = 10f;           
    public float deceleration = 5f;
    public float stopSpeedThreshold = 0.2f;

    private Vector2 targetPosition;
    private Vector2 velocity;
    private bool moving = false;
    private bool decelerating = false;
    private bool trackingPlayer = false;
    private bool fixingNewTarget = false;
    private float waitTimer = 0f;

void Start()
{
    if (player == null)
    {
        GameObject foundPlayer = GameObject.FindGameObjectWithTag("Player");
        if (foundPlayer != null)
        {
            player = foundPlayer.transform;
            Debug.Log("Jugador asignado correctamente: " + player.name);
        }
        else
        {
            Debug.LogWarning("No se encontró el objeto con la etiqueta 'Player'.");
        }
    }

    SetNewTarget();
}

    void Update()
    {
        if (!moving)
        {
            waitTimer += Time.deltaTime;
            if (waitTimer >= 0.5f)
            {
                SetNewTarget();
                waitTimer = 0f;
            }
            return;
        }

        Vector2 currentPosition = new Vector2(transform.position.x, transform.position.y);
        Vector2 toTarget = targetPosition - currentPosition;
        float distanceToTarget = toTarget.magnitude;
        Vector2 directionToTarget = toTarget.normalized;

        if (!decelerating)
        {
            velocity += directionToTarget * acceleration * Time.deltaTime;

            if (distanceToTarget < 1.5f)
            {
                decelerating = true;
                trackingPlayer = true;
            }
        }
        else
        {
            Vector2 decelDirection = (targetPosition - currentPosition).normalized;
            velocity -= decelDirection * deceleration * Time.deltaTime;

            if (trackingPlayer && player != null)
            {
                Vector2 dirToPlayer = new Vector2(player.position.x, player.position.y) - currentPosition;
                dirToPlayer.Normalize();
                float angle = Mathf.Atan2(dirToPlayer.y, dirToPlayer.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0, 0, angle);
            }

            float dot = Vector2.Dot(velocity, toTarget);
            if (dot <= 0f)
            {
                velocity = Vector2.zero;
                moving = false;
                decelerating = false;
                trackingPlayer = false;
                fixingNewTarget = false;
                waitTimer = 0f;
                return;
            }

            if (!fixingNewTarget && velocity.magnitude < stopSpeedThreshold)
            {
                FixNewTarget();
            }
        }

        velocity = Vector2.ClampMagnitude(velocity, maxSpeed);
        transform.position = new Vector3(
            transform.position.x + velocity.x * Time.deltaTime,
            transform.position.y + velocity.y * Time.deltaTime,
            transform.position.z
        );
    }

    void SetNewTarget()
    {
        if (player == null) return;

        targetPosition = new Vector2(player.position.x, player.position.y);

        Vector2 dirToPlayer = (targetPosition - new Vector2(transform.position.x, transform.position.y)).normalized;
        float angle = Mathf.Atan2(dirToPlayer.y, dirToPlayer.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);

        velocity = Vector2.zero;
        moving = true;
        decelerating = false;
        trackingPlayer = false;
        fixingNewTarget = false;
    }

    void FixNewTarget()
    {
        if (player == null) return;

        targetPosition = new Vector2(player.position.x, player.position.y);

        Vector2 dirToPlayer = (targetPosition - new Vector2(transform.position.x, transform.position.y)).normalized;
        float angle = Mathf.Atan2(dirToPlayer.y, dirToPlayer.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);

        fixingNewTarget = true;
        moving = true;
        decelerating = false;
        trackingPlayer = false;
        velocity = Vector2.zero;
    }
}
