using UnityEngine;
using UnityEngine.SceneManagement;

public class RestartOnR : MonoBehaviour
{
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            // Resetear contador persistente antes de recargar la escena
            Projectile.ResetKillCount();

            // Recarga la escena actual
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
