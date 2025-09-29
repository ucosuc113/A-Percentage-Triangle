using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class UIEntranceAnimator : MonoBehaviour
{
    [Header("Animation Settings")]
    public Vector2 direction = Vector2.up;
    public float distance = 100f;
    public float duration = 1.5f;

    [Header("Easing Curves")]
    public AnimationCurve easeInCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public AnimationCurve easeOutCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Scene")]
    public int nextSceneIndex = 1;

    private RectTransform rectTransform;
    private Vector2 targetPosition;
    private Vector2 startPosition;
    private Coroutine currentAnimation;

    private static bool globalExitTriggered = false;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        if (rectTransform == null)
        {
            Debug.LogError("Este script requiere un componente RectTransform.");
            enabled = false;
            return;
        }

        targetPosition = rectTransform.anchoredPosition;
        startPosition = targetPosition + direction.normalized * distance;
        rectTransform.anchoredPosition = startPosition;
    }

    void OnEnable()
    {
        if (!globalExitTriggered) // Solo animar entrada si no estamos saliendo
        {
            if (currentAnimation != null)
                StopCoroutine(currentAnimation);

            currentAnimation = StartCoroutine(Animate(startPosition, targetPosition, duration, easeOutCurve, null));
        }
    }

    void Start()
    {
        // Registrar el botón si lo tiene
        Button btn = GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.AddListener(TriggerGlobalExit);
        }
    }

    public void TriggerGlobalExit()
    {
        if (globalExitTriggered)
            return;

        globalExitTriggered = true;

        // Encontrar todos los elementos con este script
        UIEntranceAnimator[] allAnimators = FindObjectsOfType<UIEntranceAnimator>();

        // Contador para saber cuándo todas las animaciones terminan
        int animationsPending = allAnimators.Length;

        foreach (var animator in allAnimators)
        {
            animator.AnimateExit(() =>
            {
                animationsPending--;
                if (animationsPending <= 0)
                {
                    // Todas las animaciones terminaron
                    SceneManager.LoadScene(nextSceneIndex);
                }
            });
        }
    }

    public void AnimateExit(System.Action onComplete)
    {
        if (currentAnimation != null)
            StopCoroutine(currentAnimation);

        currentAnimation = StartCoroutine(Animate(targetPosition, startPosition, duration, easeInCurve, onComplete));
    }

    IEnumerator Animate(Vector2 from, Vector2 to, float time, AnimationCurve curve, System.Action onComplete)
    {
        float elapsed = 0f;

        while (elapsed < time)
        {
            float t = elapsed / time;
            float easedT = curve.Evaluate(t);
            rectTransform.anchoredPosition = Vector2.LerpUnclamped(from, to, easedT);

            elapsed += Time.deltaTime;
            yield return null;
        }

        rectTransform.anchoredPosition = to;
        onComplete?.Invoke();
        currentAnimation = null;
    }
}
