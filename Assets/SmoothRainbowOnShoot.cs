using UnityEngine;

public class ShaderEffectController : MonoBehaviour
{
    public Material material;
    public float flashDuration = 0.3f;
    public float fadeSpeed = 0.5f;

    private float flashProgress = 0f;
    private float toBlackProgress = 0f;
    private Coroutine flashCoroutine;

    // Nueva variable para brillo
    private float brillo = 1f;

    void Update()
    {
        if (DeathManager.Instance.IsPlayerDead())
        {
            material.SetFloat("_PlayerExists", 0f);

            toBlackProgress += Time.deltaTime * fadeSpeed;
            toBlackProgress = Mathf.Clamp01(toBlackProgress);
        }
        else
        {
            material.SetFloat("_PlayerExists", 1f);

            toBlackProgress = 0f;
        }

        material.SetFloat("_ToBlackProgress", toBlackProgress);

        // Actualiza el brillo cada frame (puedes optimizar si quieres)
        material.SetFloat("_Brightness", brillo);
    }

    public void SetBrillo(float nuevoBrillo)
    {
        brillo = Mathf.Clamp01(nuevoBrillo);
    }

    public void StartFlash()
    {
        if (flashCoroutine != null)
            StopCoroutine(flashCoroutine);

        flashCoroutine = StartCoroutine(DoFlash());
    }

    private System.Collections.IEnumerator DoFlash()
    {
        float t = 0f;
        while (t < flashDuration)
        {
            t += Time.deltaTime;
            flashProgress = Mathf.Clamp01(t / flashDuration);
            material.SetFloat("_FlashProgress", flashProgress);
            yield return null;
        }

        t = flashDuration;
        while (t > 0f)
        {
            t -= Time.deltaTime;
            flashProgress = Mathf.Clamp01(t / flashDuration);
            material.SetFloat("_FlashProgress", flashProgress);
            yield return null;
        }

        material.SetFloat("_FlashProgress", 0f);
    }
}
