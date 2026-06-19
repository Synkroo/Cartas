using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace JuegoDeCartas.UI
{
    public class CombatStatusIconUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public GameObject root;
        public Image iconImage;
        public TextMeshProUGUI valueText;
        public CombatTooltipUI tooltip;

        [Header("Feedback")]
        [Min(0.01f)] public float feedbackDuration = 0.25f;
        [Min(1f)] public float feedbackScale = 1.18f;
        public AnimationCurve feedbackCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        string title;
        string description;
        string previousValue;
        string previousTitle;
        Color iconBaseColor = Color.white;
        bool cachedIconBaseColor;
        bool wasVisible;
        Vector3 baseScale;
        bool cachedBaseScale;
        Coroutine feedbackRoutine;

        public void SetStatus(bool visible, Sprite sprite, string value, string newTitle, string newDescription)
        {
            if (!visible)
            {
                title = newTitle;
                description = newDescription;
                previousValue = value;
                previousTitle = newTitle;

                if (wasVisible)
                {
                    wasVisible = false;
                    PlayHideFeedback();
                }
                else if (feedbackRoutine == null)
                {
                    SetRootActive(false);
                }
                return;
            }

            bool changed = visible && (!wasVisible || previousValue != value || previousTitle != newTitle);

            SetRootActive(true);

            if (iconImage != null)
            {
                CacheBaseIconColor();
                iconImage.sprite = sprite;
                iconImage.enabled = visible;
                iconImage.color = HasAssignedSprite(iconImage)
                    ? iconBaseColor
                    : new Color(iconBaseColor.r, iconBaseColor.g, iconBaseColor.b, 0f);
            }

            if (valueText != null)
                valueText.text = value;

            title = newTitle;
            description = newDescription;
            previousValue = value;
            previousTitle = newTitle;
            wasVisible = visible;

            if (changed)
                PlayFeedback();
        }

        public void SetStatus(bool visible, string value, string newTitle, string newDescription)
        {
            SetStatus(visible, iconImage != null ? iconImage.sprite : null, value, newTitle, newDescription);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (tooltip == null || string.IsNullOrEmpty(title))
                return;

            tooltip.Show(this, title, description);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (tooltip != null)
                tooltip.Hide(this);
        }

        void OnDisable()
        {
            if (tooltip != null)
                tooltip.Hide(this);
        }

        void PlayFeedback()
        {
            Transform target = root != null ? root.transform : transform;
            if (!cachedBaseScale)
            {
                baseScale = target.localScale;
                cachedBaseScale = true;
            }

            if (feedbackRoutine != null)
                StopCoroutine(feedbackRoutine);
            target.localScale = baseScale;
            feedbackRoutine = StartCoroutine(FeedbackRoutine(target));
        }

        void PlayHideFeedback()
        {
            Transform target = root != null ? root.transform : transform;
            if (!cachedBaseScale)
            {
                baseScale = target.localScale;
                cachedBaseScale = true;
            }

            if (feedbackRoutine != null)
                StopCoroutine(feedbackRoutine);
            target.localScale = baseScale;
            feedbackRoutine = StartCoroutine(HideFeedbackRoutine(target));
        }

        IEnumerator FeedbackRoutine(Transform target)
        {
            float elapsed = 0f;
            while (elapsed < feedbackDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / feedbackDuration);
                float pulse = Mathf.Sin(feedbackCurve.Evaluate(progress) * Mathf.PI);
                target.localScale = baseScale * Mathf.Lerp(1f, feedbackScale, pulse);
                yield return null;
            }

            target.localScale = baseScale;
            feedbackRoutine = null;
        }

        IEnumerator HideFeedbackRoutine(Transform target)
        {
            float elapsed = 0f;
            while (elapsed < feedbackDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = feedbackCurve.Evaluate(Mathf.Clamp01(elapsed / feedbackDuration));
                target.localScale = Vector3.LerpUnclamped(baseScale, Vector3.zero, progress);
                yield return null;
            }

            target.localScale = baseScale;
            feedbackRoutine = null;
            SetRootActive(false);
        }

        void SetRootActive(bool active)
        {
            if (root != null)
                root.SetActive(active);
            else
                gameObject.SetActive(active);
        }

        static bool HasAssignedSprite(Image image)
        {
            return image != null && (image.sprite != null || image.overrideSprite != null);
        }

        void CacheBaseIconColor()
        {
            if (cachedIconBaseColor || iconImage == null)
                return;

            iconBaseColor = iconImage.color;
            cachedIconBaseColor = true;
        }
    }
}
