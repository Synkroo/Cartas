using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace JuegoDeCartas.Missions
{
    public class MissionEntryUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("Data")]
        public MissionData missionData;

        [Header("References")]
        public Button button;
        public TextMeshProUGUI titleText;
        public TextMeshProUGUI descriptionText;
        public TextMeshProUGUI difficultyText;
        public TextMeshProUGUI completedText;
        public Image missionImage;
        public Image glowImage;
        public GameObject selectedState;
        public Image[] difficultySquares;
        public Button[] difficultyButtons;
        public GameObject[] difficultyMedals;

        [Header("Animation")]
        [Min(0.01f)] public float scaleDuration = 0.15f;
        public float hoverScale = 1.04f;
        public float selectedScale = 1.06f;
        [Range(0f, 1f)] public float idleGlowAlpha = 0f;
        [Range(0f, 1f)] public float hoverGlowAlpha = 0.25f;
        [Range(0f, 1f)] public float selectedGlowAlpha = 0.4f;

        [Header("Difficulty Colors")]
        public Color filledDifficultyColor = Color.white;
        public Color emptyDifficultyColor = new Color(1f, 1f, 1f, 0.25f);

        MissionSelectionMenu owner;
        MissionDifficulty selectedDifficulty;
        Vector3 baseScale;
        Coroutine scaleRoutine;
        Coroutine glowRoutine;
        bool selected;
        bool hovering;

        void Awake()
        {
            baseScale = transform.localScale;
            if (button == null)
                button = GetComponent<Button>();

            BindButtonEvents();
            if (missionData != null)
                Refresh();
        }

        public void Setup(MissionData data, MissionSelectionMenu menu)
        {
            missionData = data;
            owner = menu;

            BindButtonEvents();
            Refresh();
            SetSelected(false, true);
        }

        void BindButtonEvents()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(OnClicked);
                button.onClick.AddListener(OnClicked);
            }

            if (difficultyButtons != null)
            {
                for (int i = 0; i < difficultyButtons.Length; i++)
                {
                    if (difficultyButtons[i] == null)
                        continue;

                    int capturedIndex = i;
                    difficultyButtons[i].onClick.RemoveAllListeners();
                    difficultyButtons[i].onClick.AddListener(() => SelectDifficulty((MissionDifficulty)(capturedIndex + 1)));
                }
            }
        }

        public void Refresh()
        {
            if (missionData == null)
                return;

            if (titleText != null)
                titleText.text = missionData.missionName;

            if (descriptionText != null)
                descriptionText.text = missionData.description;

            if (difficultyText != null)
                difficultyText.text = "Dificultad";

            if (missionImage != null)
            {
                Sprite sprite = missionData.DisplaySprite;
                missionImage.enabled = sprite != null;
                if (sprite != null)
                    missionImage.sprite = sprite;
            }

            if (completedText != null)
                completedText.gameObject.SetActive(missionData.IsDifficultyCompleted(MissionDifficulty.Letal));

            RefreshDifficultySquares();
            RefreshDifficultyButtons();
        }

        void RefreshDifficultySquares()
        {
            if (difficultySquares == null)
                return;

            for (int i = 0; i < difficultySquares.Length; i++)
            {
                if (difficultySquares[i] == null)
                    continue;

                MissionDifficulty difficulty = (MissionDifficulty)(i + 1);
                bool unlocked = missionData.IsDifficultyUnlocked(difficulty);
                bool completed = missionData.IsDifficultyCompleted(difficulty);
                difficultySquares[i].color = unlocked || completed ? filledDifficultyColor : emptyDifficultyColor;
            }
        }

        void RefreshDifficultyButtons()
        {
            if (difficultyButtons == null)
                return;

            for (int i = 0; i < difficultyButtons.Length; i++)
            {
                MissionDifficulty difficulty = (MissionDifficulty)(i + 1);
                bool unlocked = missionData.IsDifficultyUnlocked(difficulty);
                bool completed = missionData.IsDifficultyCompleted(difficulty);

                if (difficultyButtons[i] != null)
                    difficultyButtons[i].interactable = unlocked;

                if (difficultyMedals != null && i < difficultyMedals.Length && difficultyMedals[i] != null)
                    difficultyMedals[i].SetActive(completed);
            }
        }

        public void SetSelected(bool value, bool instant = false)
        {
            selected = value;
            if (selectedState != null)
                selectedState.SetActive(selected);

            float targetScale = selected ? selectedScale : hovering ? hoverScale : 1f;
            float targetGlow = selected ? selectedGlowAlpha : hovering ? hoverGlowAlpha : idleGlowAlpha;

            AnimateScale(targetScale, instant);
            AnimateGlow(targetGlow, instant);
        }

        void OnClicked()
        {
            if (missionData != null && owner != null)
                SelectFirstUnlockedDifficulty();
        }

        void SelectFirstUnlockedDifficulty()
        {
            for (int i = 0; i < 3; i++)
            {
                MissionDifficulty difficulty = (MissionDifficulty)(i + 1);
                if (missionData.IsDifficultyUnlocked(difficulty))
                {
                    SelectDifficulty(difficulty);
                    return;
                }
            }
        }

        public void SelectDifficulty(MissionDifficulty difficulty)
        {
            if (missionData == null || owner == null || !missionData.IsDifficultyUnlocked(difficulty))
                return;

            selectedDifficulty = difficulty;
            owner.SelectMission(this, missionData, selectedDifficulty);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            hovering = true;
            if (!selected)
                SetSelected(false);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            hovering = false;
            if (!selected)
                SetSelected(false);
        }

        void AnimateScale(float multiplier, bool instant)
        {
            if (scaleRoutine != null)
                StopCoroutine(scaleRoutine);

            Vector3 target = baseScale * multiplier;
            if (instant)
            {
                transform.localScale = target;
                return;
            }

            scaleRoutine = StartCoroutine(LerpScale(target));
        }

        IEnumerator LerpScale(Vector3 target)
        {
            Vector3 start = transform.localScale;
            float elapsed = 0f;

            while (elapsed < scaleDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / scaleDuration);
                transform.localScale = Vector3.Lerp(start, target, t);
                yield return null;
            }

            transform.localScale = target;
        }

        void AnimateGlow(float targetAlpha, bool instant)
        {
            if (glowImage == null)
                return;

            if (glowRoutine != null)
                StopCoroutine(glowRoutine);

            if (instant)
            {
                SetGlowAlpha(targetAlpha);
                return;
            }

            glowRoutine = StartCoroutine(LerpGlow(targetAlpha));
        }

        IEnumerator LerpGlow(float targetAlpha)
        {
            float start = glowImage.color.a;
            float elapsed = 0f;

            while (elapsed < scaleDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / scaleDuration);
                SetGlowAlpha(Mathf.Lerp(start, targetAlpha, t));
                yield return null;
            }

            SetGlowAlpha(targetAlpha);
        }

        void SetGlowAlpha(float alpha)
        {
            Color color = glowImage.color;
            color.a = alpha;
            glowImage.color = color;
        }
    }
}
