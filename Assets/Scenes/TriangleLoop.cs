using UnityEngine;

public class TriangleLoop : MonoBehaviour
{
    public float visibleTime = 1f;
    private Renderer triangleRenderer;
    private Material triangleMaterial;
    private ParticleSystem particleSystem;
    private Color originalColor;
    private bool isLooping = false;

    void Start()
    {
        triangleRenderer = GetComponent<Renderer>();
        triangleMaterial = triangleRenderer.material;
        originalColor = triangleMaterial.color;

        particleSystem = GetComponentInChildren<ParticleSystem>();

        StartCoroutine(AnimationLoop());
    }

    System.Collections.IEnumerator AnimationLoop()
    {
        while (true)
        {
            // 🔵 Fase 1: Triángulo visible
            SetAlpha(1f);
            triangleRenderer.enabled = true;
            yield return new WaitForSeconds(visibleTime);

            // 🔵 Fase 2: Hacer transparente
            SetAlpha(0f);

            // 🔵 Fase 3: Activar el particle system
            particleSystem.Play();

            // 🔵 Fase 4: Esperar a que termine
            while (particleSystem.isPlaying)
            {
                yield return null;
            }

            // 🔁 Repetir
        }
    }

    void SetAlpha(float alpha)
    {
        Color color = triangleMaterial.color;
        color.a = alpha;
        triangleMaterial.color = color;
    }
}
