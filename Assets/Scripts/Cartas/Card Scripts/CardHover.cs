using UnityEngine;

namespace JuegoDeCartas.Cards
{
    public class CardHover : MonoBehaviour
    {
        public bool isHovering;

        [Header("Scale")]
        public float hoverScale = 1.1f;
        public float lerpSpeed = 10f;

        private Vector3 originalScale;
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
                Time.deltaTime * lerpSpeed
            );
        }

        public void SetHover(bool value)
        {
            isHovering = value;
        }
    }
}
