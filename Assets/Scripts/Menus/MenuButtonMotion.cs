using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace JuegoDeCartas.UI
{
    public class MenuButtonMotion : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        public RectTransform target;
        [Min(1f)] public float hoverScale = 1.04f;
        [Range(0.5f, 1f)] public float pressedScale = 0.96f;
        [Min(0.01f)] public float duration = 0.08f;

        Vector3 baseScale;
        Coroutine routine;
        bool hovering;

        void Awake()
        {
            if (target == null)
                target = transform as RectTransform;
            baseScale = target != null ? target.localScale : Vector3.one;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            hovering = true;
            AnimateTo(hoverScale);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            hovering = false;
            AnimateTo(1f);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            AnimateTo(pressedScale);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            AnimateTo(hovering ? hoverScale : 1f);
        }

        void AnimateTo(float multiplier)
        {
            if (target == null)
                return;
            if (routine != null)
                StopCoroutine(routine);
            routine = StartCoroutine(AnimateRoutine(baseScale * multiplier));
        }

        IEnumerator AnimateRoutine(Vector3 destination)
        {
            Vector3 start = target.localScale;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                target.localScale = Vector3.Lerp(start, destination, Mathf.Clamp01(elapsed / duration));
                yield return null;
            }
            target.localScale = destination;
        }
    }
}
