using UnityEngine;

public class BolaAnimada : MonoBehaviour
{
    public float altura = 4f;          // Cuánto sube localmente
    public float duracion = 1.5f;      // Tiempo de subida

    void Start()
    {
        StartCoroutine(AnimarBucle());
    }

    System.Collections.IEnumerator AnimarBucle()
    {
        while (true)
        {
            // Capturar la posición local actual (respecto al padre)
            Vector3 inicio = transform.localPosition;
            Vector3 destino = inicio + Vector3.up * altura;

            float tiempo = 0f;

            while (tiempo < duracion)
            {
                float t = tiempo / duracion;

                // Easing cuadrático "ease out"
                float easedT = 1f - Mathf.Pow(1f - t, 2f);

                // Movimiento local
                transform.localPosition = Vector3.Lerp(inicio, destino, easedT);

                tiempo += Time.deltaTime;
                yield return null;
            }

            // Teletransportar al inicio local
            transform.localPosition = inicio;

            yield return null; // Esperar un frame antes de repetir
        }
    }
}
