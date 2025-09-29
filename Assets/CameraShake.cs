using UnityEngine;

public class CameraShake : MonoBehaviour
{
    public float duration = 0.2f;
    public float magnitude = 0.15f;

    private Vector3 originalPos;
    private float elapsed = 0f;
    private bool shaking = false;

    void Start()
    {
        originalPos = transform.localPosition;
    }

    void Update()
    {
        if (shaking)
        {
            if (elapsed < duration)
            {
                float x = Random.Range(-1f, 1f) * magnitude;
                float y = Random.Range(-1f, 1f) * magnitude;

                transform.localPosition = originalPos + new Vector3(x, y, 0);
                elapsed += Time.deltaTime;
            }
            else
            {
                shaking = false;
                elapsed = 0f;
                transform.localPosition = originalPos;
            }
        }
    }

    public void TriggerShake()
    {
        if (!shaking)
        {
            shaking = true;
            elapsed = 0f;
        }
    }
}
