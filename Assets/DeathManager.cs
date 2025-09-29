using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class DeathManager : MonoBehaviour
{
    public static DeathManager Instance { get; private set; }

    [Header("Configuración del efecto de muerte")]
    [Tooltip("Prefab del ParticleSystem que se instanciará en la posición del player.")]
    public GameObject deathEffectPrefab;

    [Tooltip("Duración (segundos) que debe mostrarse el particle effect antes de reiniciar.")]
    public float effectDuration = 0.5f;

    [Tooltip("Tiempo en segundos para esperar antes de reiniciar la escena tras morir.")]
    public float restartDelayAfterDeath = 3f;

    [Header("Referencia a objetos adicionales a eliminar")]
    [Tooltip("Villain (u otro objeto) que también será destruido al morir el player.")]
    public GameObject additionalObjectToDestroy;

    private bool playerIsDead = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // DontDestroyOnLoad(gameObject); // opcional
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void KillPlayer(GameObject player)
    {
        if (player == null)
        {
            Debug.LogWarning("[DeathManager] KillPlayer recibió null.");
            return;
        }

        playerIsDead = true;

        Vector3 pos = player.transform.position;
        Quaternion rot = player.transform.rotation;

        DisablePlayerBeforeDestroy(player);

        if (deathEffectPrefab != null)
        {
            GameObject effectInstance = Instantiate(deathEffectPrefab, pos, rot);
            ParticleSystem ps = effectInstance.GetComponent<ParticleSystem>();
            if (ps != null) ps.Play();
            Destroy(effectInstance, effectDuration + 0.5f);
        }

        Destroy(player);

        if (additionalObjectToDestroy != null)
        {
            Destroy(additionalObjectToDestroy);
        }

        StartCoroutine(RestartSceneAfter(restartDelayAfterDeath));
    }

    public bool IsPlayerDead()
    {
        return playerIsDead;
    }

    private IEnumerator RestartSceneAfter(float seconds)
    {
        yield return new WaitForSeconds(seconds);

        UIEntranceAnimator[] animators = FindObjectsOfType<UIEntranceAnimator>();
        if (animators.Length > 0)
        {
            animators[0].TriggerGlobalExit();
        }
        else
        {
            Scene current = SceneManager.GetActiveScene();
            SceneManager.LoadScene(current.buildIndex);
        }
    }

    private void DisablePlayerBeforeDestroy(GameObject player)
    {
        var behaviours = player.GetComponentsInChildren<Behaviour>();
        foreach (var b in behaviours)
        {
            if (b == this) continue;
            b.enabled = false;
        }

        var cols = player.GetComponentsInChildren<Collider>();
        foreach (var c in cols) c.enabled = false;

        var rbs = player.GetComponentsInChildren<Rigidbody>();
        foreach (var rb in rbs) rb.isKinematic = true;
    }
}
