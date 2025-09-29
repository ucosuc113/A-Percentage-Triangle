using UnityEngine;
using System.Collections;

public class PlayerVida : MonoBehaviour
{
    [Header("Parámetros de Vida")]
    public int vidaMaxima = 5;
    public float dañoCooldown = 2f;
    public string tagEnemigo = "Enemy";

    [Header("Referencia al ShaderEffectController")]
    public ShaderEffectController shaderEffectController; // asignar en inspector

    [Header("Opciones")]
    public bool debugLogs = true;

    private int vidaActual;
    private bool puedeRecibirDaño = true;
    private bool estaMuerto = false;

    void Awake()
    {
        vidaActual = vidaMaxima;
    }

    void Start()
    {
        ActualizarBrilloFondo();
        DebugLog($"START: Vida inicial = {vidaActual}/{vidaMaxima}");
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        DebugLog($"OnTriggerEnter2D: tocó a {other.name}, tag = {other.tag}");
        ProcesarTrigger(other.gameObject);
    }

    void ProcesarTrigger(GameObject otro)
    {
        if (estaMuerto)
        {
            DebugLog("Jugador muerto, ignorando trigger.");
            return;
        }

        if (!puedeRecibirDaño)
        {
            DebugLog("En cooldown de daño, no recibe daño ahora.");
            return;
        }

        if (otro.CompareTag(tagEnemigo))
        {
            DebugLog("¡Entró en trigger con Enemy! Recibiendo daño.");
            RecibirDaño();
        }
        else
        {
            DebugLog($"Entró en trigger con objeto no enemigo: {otro.tag}");
        }
    }

    void RecibirDaño()
    {
        vidaActual--;
        vidaActual = Mathf.Clamp(vidaActual, 0, vidaMaxima);

        DebugLog($"RecibirDaño: vida actual = {vidaActual}/{vidaMaxima}");

        ActualizarBrilloFondo();

        if (vidaActual <= 0)
        {
            MuerteJugador();
        }
        else
        {
            StartCoroutine(DañoCooldown());
        }
    }

    IEnumerator DañoCooldown()
    {
        puedeRecibirDaño = false;
        DebugLog($"Cooldown de daño iniciado por {dañoCooldown} segundos.");
        yield return new WaitForSeconds(dañoCooldown);
        puedeRecibirDaño = true;
        DebugLog("Cooldown de daño terminado.");
    }

    void ActualizarBrilloFondo()
    {
        if (shaderEffectController != null)
        {
            float brillo = (float)vidaActual / vidaMaxima;
            shaderEffectController.SetBrillo(brillo);
            DebugLog($"Brillo del fondo actualizado a {brillo}");
        }
        else
        {
            DebugLog("ShaderEffectController no asignado, no puedo actualizar brillo.");
        }
    }

    void MuerteJugador()
    {
        estaMuerto = true;
        DebugLog("Jugador muerto.");

        if (DeathManager.Instance != null)
        {
            DeathManager.Instance.KillPlayer(gameObject);
            DebugLog("DeathManager llamado para manejar muerte.");
        }
        else
        {
            DebugLog("DeathManager no encontrado en escena.");
        }
    }

    void DebugLog(string msg)
    {
        if (debugLogs) Debug.Log("[PlayerVida2D] " + msg);
    }
}
