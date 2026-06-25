using UnityEngine;
using UnityEngine.EventSystems;

namespace JuegoDeCartas.Cards
{
    public class CardHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public bool isHovering;

        [Header("Scale")]
        public float hoverScale = 1.1f;
        public float lerpSpeed = 10f;

        public Vector3 originalScale;
        private Vector3 targetScale;
        private RectTransform rectTransform;

        void Awake()
        {
            EnsureInitialized();
            targetScale = originalScale;
        }

        void Update()
        {
            EnsureInitialized();
            if (rectTransform == null)
                return;

            targetScale = isHovering ? originalScale * hoverScale : originalScale;
            rectTransform.localScale = Vector3.Lerp(
                rectTransform.localScale,
                targetScale,
                Time.unscaledDeltaTime * lerpSpeed
            );
        }

        public void RefreshState()
        {
            EnsureInitialized();
            if (rectTransform == null)
                return;

            targetScale = isHovering ? originalScale * hoverScale : originalScale;
            rectTransform.localScale = targetScale;
        }

        void EnsureInitialized()
        {
            if (rectTransform != null)
                return;

            rectTransform = GetComponent<RectTransform>();
            if (rectTransform == null)
                return;

            originalScale = rectTransform.localScale;
        }

        public void SetHover(bool value)
        {
            isHovering = value;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            isHovering = true;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isHovering = false;
        }
    }
}
