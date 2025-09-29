using UnityEngine;
using TMPro;
using System.Collections;

public class MoverElemento : MonoBehaviour
{
    private bool isUI = false;
    private Vector3 worldTargetPos;
    private Vector2 uiTargetPos;
    private int enterCount = 0;
    private const int maxEnterPresses = 3;
    private bool canPress = true;

    private RectTransform rectTransform;

    [Header("Elementos para hacer fade in")]
    public TextMeshProUGUI textoTMP;  // Texto principal
    public GameObject botonGO;        // Botón completo (GameObject)

    private CanvasGroup botonCanvasGroup;
    private TextMeshProUGUI botonTextoTMP;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        isUI = rectTransform != null;

        // Preparar texto principal
        if (textoTMP != null)
        {
            Color c = textoTMP.color;
            c.a = 0f;
            textoTMP.color = c;
            textoTMP.gameObject.SetActive(true);
        }

        // Preparar botón y su CanvasGroup para el fade
        if (botonGO != null)
        {
            // Asegurar que el botón está activo
            botonGO.SetActive(true);

            // Añadir CanvasGroup si no tiene
            botonCanvasGroup = botonGO.GetComponent<CanvasGroup>();
            if (botonCanvasGroup == null)
                botonCanvasGroup = botonGO.AddComponent<CanvasGroup>();

            // Inicializar alpha del CanvasGroup en 0 (invisible)
            botonCanvasGroup.alpha = 0f;
            botonCanvasGroup.interactable = false;
            botonCanvasGroup.blocksRaycasts = false;

            // Buscar texto TMP hijo y asegurarse que está activo con alfa 0
            botonTextoTMP = botonGO.GetComponentInChildren<TextMeshProUGUI>();
            if (botonTextoTMP != null)
            {
                Color cBotonTexto = botonTextoTMP.color;
                cBotonTexto.a = 0f;
                botonTextoTMP.color = cBotonTexto;
                botonTextoTMP.gameObject.SetActive(true);
            }

            // Asegurarse que el botón tiene Image activo para que sea visible (importante)
            var image = botonGO.GetComponent<UnityEngine.UI.Image>();
            if (image != null)
            {
                Color cImage = image.color;
                cImage.a = 1f;  // Siempre alfa 1 en la imagen de fondo
                image.color = cImage;
                image.enabled = true;
            }
        }

        if (isUI)
        {
            uiTargetPos = rectTransform.anchoredPosition + Vector2.up * 600f;
            StartCoroutine(MoverUI(uiTargetPos, 0.8f));
        }
        else
        {
            worldTargetPos = transform.position + Vector3.up * 20f;
            StartCoroutine(MoverNormal(worldTargetPos, 0.8f));
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return) && enterCount < maxEnterPresses && canPress)
        {
            enterCount++;
            canPress = false;

            if (isUI)
            {
                uiTargetPos += Vector2.up * 1200f;
                StartCoroutine(MoverUI(uiTargetPos, 1f));
            }
            else
            {
                worldTargetPos += Vector3.up * 40f;
                StartCoroutine(MoverNormal(worldTargetPos, 1f));
            }

            if (enterCount == maxEnterPresses)
            {
                StartCoroutine(FadeInElementos(1f));
            }
        }
    }

    IEnumerator MoverNormal(Vector3 destino, float duracion)
    {
        Vector3 inicio = transform.position;
        float tiempo = 0f;

        while (tiempo < duracion)
        {
            float t = tiempo / duracion;
            t = 1f - Mathf.Pow(1f - t, 2f);
            transform.position = Vector3.Lerp(inicio, destino, t);
            tiempo += Time.deltaTime;
            yield return null;
        }

        transform.position = destino;
        canPress = true;
    }

    IEnumerator MoverUI(Vector2 destino, float duracion)
    {
        Vector2 inicio = rectTransform.anchoredPosition;
        float tiempo = 0f;

        while (tiempo < duracion)
        {
            float t = tiempo / duracion;
            t = 1f - Mathf.Pow(1f - t, 2f);
            rectTransform.anchoredPosition = Vector2.Lerp(inicio, destino, t);
            tiempo += Time.deltaTime;
            yield return null;
        }

        rectTransform.anchoredPosition = destino;
        canPress = true;
    }

    IEnumerator FadeInElementos(float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Clamp01(elapsed / duration);

            if (textoTMP != null)
            {
                Color c = textoTMP.color;
                c.a = alpha;
                textoTMP.color = c;
            }

            if (botonGO != null && botonCanvasGroup != null)
            {
                botonCanvasGroup.alpha = alpha;

                if (botonTextoTMP != null)
                {
                    Color cBotonTexto = botonTextoTMP.color;
                    cBotonTexto.a = alpha;
                    botonTextoTMP.color = cBotonTexto;
                }
            }

            yield return null;
        }

        // Al final aseguramos opacidad completa y botón interactuable
        if (textoTMP != null)
        {
            Color c = textoTMP.color;
            c.a = 1f;
            textoTMP.color = c;
        }

        if (botonGO != null && botonCanvasGroup != null)
        {
            botonCanvasGroup.alpha = 1f;
            botonCanvasGroup.interactable = true;
            botonCanvasGroup.blocksRaycasts = true;

            if (botonTextoTMP != null)
            {
                Color cBotonTexto = botonTextoTMP.color;
                cBotonTexto.a = 1f;
                botonTextoTMP.color = cBotonTexto;
            }
        }
    }
}
