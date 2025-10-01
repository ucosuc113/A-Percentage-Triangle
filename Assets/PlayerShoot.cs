using UnityEngine;
using TMPro;
using System.Collections;

public class PlayerShoot : MonoBehaviour
{
    public GameObject projectilePrefab;
    public float projectileSpeed = 15f;
    public Transform collisionsParent;
    public TMP_Text porcentajeTexto;

    public GameObject jugador;

    private int probabilidadActual = 0;

    public enum TipoArma
    {
        Normal,
        Triple,
        TripleGrande
    }

    private TipoArma tipoArmaActual = TipoArma.Normal;

    public GameObject powerT1Prefab;
    public GameObject powerT2Prefab;
    public GameObject powerT3Prefab;

    private bool estaEnCooldownTripleGrande = false;

    // --- AUDIO ---
    public AudioClip shootClip;                 // asigna en Inspector
    [Range(0f, 1f)] public float volume = 1f;
    public float pitchMin = 1f;
    public float pitchMax = 1f;
    private AudioSource audioSource;

    void Start()
    {
        if (jugador == null)
        {
            jugador = this.gameObject;
            Debug.Log("Jugador asignado automáticamente al propio GameObject.");
        }

        // Init AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f; // 0 = 2D (sonido global), pon 1 para 3D si lo deseas
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            // 🔁 1. Activar Flash visual
            ShaderEffectController shaderController = FindObjectOfType<ShaderEffectController>();
            if (shaderController != null)
                shaderController.StartFlash();

            // 🔁 2. Activar Shake de cámara
            CameraShake camShake = FindObjectOfType<CameraShake>();
            if (camShake != null)
                camShake.TriggerShake();

            // 🔫 3. Disparar el arma
            switch (tipoArmaActual)
            {
                case TipoArma.Normal:
                    ShootProjectile();
                    PlayShootSound();
                    IncrementarProbabilidadYChequear(2);
                    break;

                case TipoArma.Triple:
                    ShootTripleProjectile();
                    PlayShootSound();
                    IncrementarProbabilidadYChequear(2);
                    break;

                case TipoArma.TripleGrande:
                    if (!estaEnCooldownTripleGrande)
                    {
                        ShootTripleGrande();
                        PlayShootSound();
                        StartCoroutine(CooldownTripleGrande());
                        IncrementarProbabilidadYChequear(3);
                    }
                    else
                    {
                        Debug.Log("Disparo Triple Grande en cooldown.");
                    }
                    break;
            }
        }
    }

    void ShootProjectile()
    {
        if (projectilePrefab == null) return;

        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0f;
        Vector3 dir = (mousePos - transform.position).normalized;

        GameObject proj = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        proj.transform.rotation = Quaternion.Euler(0, 0, angle);

        Projectile projectileScript = proj.GetComponent<Projectile>();
        if (projectileScript != null)
        {
            projectileScript.SetDirection(dir);
            projectileScript.collisionsParent = collisionsParent;
            projectileScript.SetSpeed(projectileSpeed);
        }
    }

    void ShootTripleProjectile()
    {
        if (projectilePrefab == null) return;

        Vector3 forward = transform.right;
        Vector3 position = transform.position;

        float[] angles = { 90f, 135f, 45f };

        foreach (float angle in angles)
        {
            Quaternion rot = Quaternion.Euler(0, 0, angle);
            Vector3 dir = rot * forward;

            GameObject proj = Instantiate(projectilePrefab, position, Quaternion.identity);
            float angleDeg = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            proj.transform.rotation = Quaternion.Euler(0, 0, angleDeg);

            Projectile projectileScript = proj.GetComponent<Projectile>();
            if (projectileScript != null)
            {
                projectileScript.SetDirection(dir.normalized);
                projectileScript.collisionsParent = collisionsParent;
                projectileScript.SetSpeed(projectileSpeed);
            }
        }
    }

    void ShootTripleGrande()
    {
        if (projectilePrefab == null) return;

        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0f;
        Vector3 dirBase = (mousePos - transform.position).normalized;

        float[] angles = { 0f, 15f, -15f };

        foreach (float angle in angles)
        {
            Quaternion rot = Quaternion.Euler(0, 0, angle);
            Vector3 dir = rot * dirBase;

            GameObject proj = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
            float angleDeg = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            proj.transform.rotation = Quaternion.Euler(0, 0, angleDeg);

            // Aumentar el tamaño del proyectil
            proj.transform.localScale *= 2f;

            Projectile projectileScript = proj.GetComponent<Projectile>();
            if (projectileScript != null)
            {
                projectileScript.SetDirection(dir.normalized);
                projectileScript.collisionsParent = collisionsParent;
                projectileScript.SetSpeed(projectileSpeed);
            }
        }
    }

    IEnumerator CooldownTripleGrande()
    {
        estaEnCooldownTripleGrande = true;
        yield return new WaitForSeconds(2f);
        estaEnCooldownTripleGrande = false;
    }

    void IncrementarProbabilidadYChequear(int cambio)
    {
        probabilidadActual = Mathf.Clamp(probabilidadActual + cambio, 0, 100);

        if (porcentajeTexto != null)
            porcentajeTexto.text = probabilidadActual + "%";

        if (cambio > 0)
        {
            float tirada = Random.Range(0f, 100f);
            Debug.Log($"Probabilidad actual: {probabilidadActual}%, tirada: {tirada}");

            if (tirada <= probabilidadActual)
            {
                EjecutarScriptExtra();
            }
        }
    }

    void EjecutarScriptExtra()
    {
        if (DeathManager.Instance != null)
        {
            Debug.Log("Ejecutando KillPlayer desde PlayerShoot");
            DeathManager.Instance.KillPlayer(jugador);
        }
        else
        {
            Debug.LogError("DeathManager.Instance es null. No se puede matar al jugador.");
        }
    }

    public void CambiarArma(TipoArma nuevaArma)
    {
        tipoArmaActual = nuevaArma;
        probabilidadActual = 0;

        if (porcentajeTexto != null)
            porcentajeTexto.text = probabilidadActual + "%";

        Debug.Log($"Arma cambiada a: {nuevaArma}");
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (powerT1Prefab != null && other.gameObject.name.Contains(powerT1Prefab.name))
        {
            CambiarArma(TipoArma.Normal);
            Destroy(other.gameObject);
        }
        else if (powerT2Prefab != null && other.gameObject.name.Contains(powerT2Prefab.name))
        {
            CambiarArma(TipoArma.Triple);
            Destroy(other.gameObject);
        }
        else if (powerT3Prefab != null && other.gameObject.name.Contains(powerT3Prefab.name))
        {
            CambiarArma(TipoArma.TripleGrande);
            Destroy(other.gameObject);
        }
    }

    // Reproduce sonido por DISPARO (una vez por presionar Space)
    void PlayShootSound()
    {
        if (shootClip == null || audioSource == null) return;
        audioSource.pitch = Random.Range(pitchMin, pitchMax);
        audioSource.PlayOneShot(shootClip, volume);
    }
}
