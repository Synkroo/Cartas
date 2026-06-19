using System;
using System.Collections;
using UnityEngine;

namespace JuegoDeCartas.UI
{
    public class UITransitionAnimator : MonoBehaviour
    {
        public CanvasGroup canvasGroup;
        public RectTransform target;

        [Header("Animation")]
        [Min(0.01f)] public float duration = 0.25f;
        public Vector2 hiddenOffset = new Vector2(0f, -45f);
        [Min(0.01f)] public float hiddenScale = 0.92f;
        public AnimationCurve curve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        Vector2 visiblePosition;
        Vector3 visibleScale;
        bool cached;
        Coroutine routine;

        void Awake()
        {
            Cache();
        }

        public void PlayIn(float delay = 0f)
        {
            Cache();
            gameObject.SetActive(true);
            StartTransition(true, delay, null);
        }

        public void PlayOut(Action completed = null)
        {
            Cache();
            StartTransition(false, 0f, completed);
        }

        void StartTransition(bool showing, float delay, Action completed)
        {
            if (routine != null)
                StopCoroutine(routine);
            routine = StartCoroutine(TransitionRoutine(showing, delay, completed));
        }

        IEnumerator TransitionRoutine(bool showing, float delay, Action completed)
        {
            if (delay > 0f)
                yield return new WaitForSecondsRealtime(delay);

            Vector2 hiddenPosition = visiblePosition + hiddenOffset;
            Vector3 hiddenScaleVector = visibleScale * hiddenScale;
            float startAlpha = showing ? 0f : 1f;
            float endAlpha = showing ? 1f : 0f;
            Vector2 startPosition = showing ? hiddenPosition : visiblePosition;
            Vector2 endPosition = showing ? visiblePosition : hiddenPosition;
            Vector3 startScale = showing ? hiddenScaleVector : visibleScale;
            Vector3 endScale = showing ? visibleScale : hiddenScaleVector;

            SetVisual(startAlpha, startPosition, startScale);
            if (canvasGroup != null)
            {
                canvasGroup.interactable = showing;
                canvasGroup.blocksRaycasts = showing;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float value = curve.Evaluate(Mathf.Clamp01(elapsed / duration));
                SetVisual(
                    Mathf.Lerp(startAlpha, endAlpha, value),
                    Vector2.LerpUnclamped(startPosition, endPosition, value),
                    Vector3.LerpUnclamped(startScale, endScale, value)
                );
                yield return null;
            }

            SetVisual(endAlpha, endPosition, endScale);
            routine = null;
            completed?.Invoke();
        }

        void Cache()
        {
            if (cached)
                return;

            if (target == null)
                target = transform as RectTransform;
            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();

            visiblePosition = target != null ? target.anchoredPosition : Vector2.zero;
            visibleScale = target != null ? target.localScale : Vector3.one;
            cached = true;
        }

        void SetVisual(float alpha, Vector2 position, Vector3 scale)
        {
            if (canvasGroup != null)
                canvasGroup.alpha = alpha;
            if (target != null)
            {
                target.anchoredPosition = position;
                target.localScale = scale;
            }
        }
    }
}
