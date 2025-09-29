using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    public float velocidad = 5f;
    public float friccion = 0.9f; // Cuanto más cerca de 1, más desliza
    public float rotacionVelocidad = 720f; // Grados por segundo

    private Rigidbody2D rb;
    private Vector2 direccionDeseada;
    private Vector2 velocidadActual;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0; // Asegúrate de que no le afecte la gravedad
        rb.freezeRotation = true; // Rotaremos manualmente
    }

    void Update()
    {
        direccionDeseada = Vector2.zero;

        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
            direccionDeseada += Vector2.up;

        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
            direccionDeseada += Vector2.down;

        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            direccionDeseada += Vector2.left;

        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            direccionDeseada += Vector2.right;

        direccionDeseada = direccionDeseada.normalized;

        // Rotar hacia el mouse
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 direccionMouse = (mousePos - transform.position);
        float angulo = Mathf.Atan2(direccionMouse.y, direccionMouse.x) * Mathf.Rad2Deg;
        rb.MoveRotation(angulo - 90f); // Aplicamos rotación hacia el mouse
    }

    void FixedUpdate()
    {
        // Aplicar movimiento con interpolación para simular deslizamiento
        Vector2 velocidadObjetivo = direccionDeseada * velocidad;
        velocidadActual = Vector2.Lerp(velocidadActual, velocidadObjetivo, 1 - friccion);

        rb.MovePosition(rb.position + velocidadActual * Time.fixedDeltaTime);
    }
}
