using System.Collections;
using JuegoDeCartas.Relics;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace JuegoDeCartas.UI
{
    public class RelicIconUI : MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler,
        IPointerDownHandler,
        IPointerUpHandler
    {
        public Image iconImage;
        public GameObject emptyState;
        public CombatTooltipUI tooltip;

        [Header("Animation")]
        public RectTransform animationTarget;
        [Min(1f)] public float hoverScale = 1.08f;
        [Range(0.5f, 1f)] public float pressedScale = 0.94f;
        [Min(1f)] public float acquiredScale = 1.28f;
        [Min(0.01f)] public float hoverDuration = 0.08f;
        [Min(0.01f)] public float acquiredDuration = 0.28f;
        public AnimationCurve acquiredCurve =
            AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        RelicData relic;
        Vector3 baseScale;
        Coroutine scaleRoutine;
        bool hovering;

        void Awake()
        {
            if (animationTarget == null)
                animationTarget = transform as RectTransform;
            baseScale = animationTarget != null
                ? animationTarget.localScale
                : Vector3.one;
        }

        public void Setup(RelicData newRelic)
        {
            bool acquired = newRelic != null && newRelic != relic;
            relic = newRelic;
            bool hasRelic = relic != null;

            if (iconImage != null)
            {
                iconImage.sprite = hasRelic ? relic.icon : null;
                iconImage.enabled = hasRelic && relic.icon != null;
            }

            if (emptyState != null)
                emptyState.SetActive(!hasRelic);

            if (acquired && Application.isPlaying && isActiveAndEnabled)
                PlayAcquiredAnimation();
            else if (!hasRelic)
                SetScale(1f);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (relic == null)
                return;

            hovering = true;
            AnimateScale(hoverScale, hoverDuration);

            if (tooltip == null)
                return;

            tooltip.Show(this, relic.relicName, relic.description);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            hovering = false;
            AnimateScale(1f, hoverDuration);

            if (tooltip != null)
                tooltip.Hide(this);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (relic != null)
                AnimateScale(pressedScale, hoverDuration);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            AnimateScale(hovering ? hoverScale : 1f, hoverDuration);
        }

        void OnDisable()
        {
            if (tooltip != null)
                tooltip.Hide(this);

            hovering = false;
            if (scaleRoutine != null)
            {
                StopCoroutine(scaleRoutine);
                scaleRoutine = null;
            }
            SetScale(1f);
        }

        void PlayAcquiredAnimation()
        {
            if (animationTarget == null)
                return;
            if (scaleRoutine != null)
                StopCoroutine(scaleRoutine);
            scaleRoutine = StartCoroutine(AcquiredRoutine());
        }

        IEnumerator AcquiredRoutine()
        {
            float elapsed = 0f;
            while (elapsed < acquiredDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / acquiredDuration);
                float pulse = Mathf.Sin(progress * Mathf.PI);
                float value = Mathf.LerpUnclamped(
                    1f,
                    acquiredScale,
                    acquiredCurve.Evaluate(pulse)
                );
                SetScale(value);
                yield return null;
            }

            scaleRoutine = null;
            AnimateScale(hovering ? hoverScale : 1f, hoverDuration);
        }

        void AnimateScale(float multiplier, float duration)
        {
            if (!Application.isPlaying || animationTarget == null)
            {
                SetScale(multiplier);
                return;
            }

            if (scaleRoutine != null)
                StopCoroutine(scaleRoutine);
            scaleRoutine = StartCoroutine(ScaleRoutine(multiplier, duration));
        }

        IEnumerator ScaleRoutine(float multiplier, float duration)
        {
            Vector3 start = animationTarget.localScale;
            Vector3 destination = baseScale * multiplier;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                animationTarget.localScale = Vector3.Lerp(
                    start,
                    destination,
                    Mathf.Clamp01(elapsed / duration)
                );
                yield return null;
            }

            animationTarget.localScale = destination;
            scaleRoutine = null;
        }

        void SetScale(float multiplier)
        {
            if (animationTarget != null)
                animationTarget.localScale = baseScale * multiplier;
        }
    }
}
