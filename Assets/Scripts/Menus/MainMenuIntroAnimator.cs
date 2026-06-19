using System.Collections;
using UnityEngine;

namespace JuegoDeCartas.UI
{
    public class MainMenuIntroAnimator : MonoBehaviour
    {
        public RectTransform[] targets;
        public CanvasGroup[] canvasGroups;
        [Min(0.01f)] public float duration = 0.3f;
        [Min(0f)] public float stagger = 0.08f;
        public Vector2 startOffset = new Vector2(-80f, 0f);
        public AnimationCurve movementCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        Vector2[] targetPositions;

        void Awake()
        {
            targetPositions = new Vector2[targets != null ? targets.Length : 0];
            for (int i = 0; i < targetPositions.Length; i++)
            {
                if (targets[i] != null)
                    targetPositions[i] = targets[i].anchoredPosition;
            }
        }

        void OnEnable()
        {
            StartCoroutine(Play());
        }

        IEnumerator Play()
        {
            for (int i = 0; i < targetPositions.Length; i++)
            {
                if (targets[i] != null)
                    targets[i].anchoredPosition = targetPositions[i] + startOffset;
                if (canvasGroups != null && i < canvasGroups.Length && canvasGroups[i] != null)
                    canvasGroups[i].alpha = 0f;
            }

            float totalDuration = duration + stagger * Mathf.Max(0, targetPositions.Length - 1);
            float elapsed = 0f;
            while (elapsed < totalDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                for (int i = 0; i < targetPositions.Length; i++)
                {
                    float localTime = Mathf.Clamp01((elapsed - i * stagger) / duration);
                    float value = movementCurve.Evaluate(localTime);
                    if (targets[i] != null)
                        targets[i].anchoredPosition = Vector2.Lerp(targetPositions[i] + startOffset, targetPositions[i], value);
                    if (canvasGroups != null && i < canvasGroups.Length && canvasGroups[i] != null)
                        canvasGroups[i].alpha = value;
                }
                yield return null;
            }
        }
    }
}
