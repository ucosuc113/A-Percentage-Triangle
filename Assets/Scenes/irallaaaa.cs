using UnityEngine;
using UnityEngine.SceneManagement;

public class CambiarEscenaPorVariable : MonoBehaviour
{
    [Tooltip("Número de la escena a la que quieres cambiar")]
    public int numeroEscena;

    public void CambiarEscena()
    {
        if (numeroEscena >= 0 && numeroEscena < SceneManager.sceneCountInBuildSettings)
        {
            SceneManager.LoadScene(numeroEscena);
        }
        else
        {
            Debug.LogWarning("Número de escena inválido: " + numeroEscena);
        }
    }
}
