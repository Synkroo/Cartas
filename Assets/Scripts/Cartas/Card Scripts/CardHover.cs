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
            rectTransform = GetComponent<RectTransform>();
            originalScale = rectTransform.localScale;
            targetScale = originalScale;
        }

        void Update()
        {
            targetScale = isHovering ? originalScale * hoverScale : originalScale;
            rectTransform.localScale = Vector3.Lerp(
                rectTransform.localScale,
                targetScale,
                Time.unscaledDeltaTime * lerpSpeed
            );
        }

        public void RefreshState()
        {
            targetScale = isHovering ? originalScale * hoverScale : originalScale;
            rectTransform.localScale = targetScale;
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
