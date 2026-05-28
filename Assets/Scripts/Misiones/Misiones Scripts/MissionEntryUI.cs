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
        public Button difficultySelectorButton;
        public Image difficultySelectorGlowImage;
        public GameObject[] difficultyMedals;

        [Header("Difficulty Popup")]
        public GameObject difficultyPopupRoot;
        public CanvasGroup difficultyPopupCanvasGroup;
        public Button[] popupDifficultyButtons;
        public TextMeshProUGUI[] popupDifficultyLabels;
        public TextMeshProUGUI[] popupDifficultyStatuses;

        [Header("Animation")]
        [Min(0.01f)] public float scaleDuration = 0.15f;
        public float hoverScale = 1.04f;
        public float selectedScale = 1.06f;
        [Range(0f, 1f)] public float idleGlowAlpha = 0f;
        [Range(0f, 1f)] public float hoverGlowAlpha = 0.12f;
        [Range(0f, 1f)] public float selectedGlowAlpha = 0.24f;
        [Range(0f, 1f)] public float difficultyButtonIdleGlowAlpha = 0f;
        [Range(0f, 1f)] public float difficultyButtonHoverGlowAlpha = 0.35f;
        [Range(0f, 1f)] public float popupDifficultyHoverLighten = 0.35f;

        [Header("Difficulty Colors")]
        public Color filledDifficultyColor = Color.white;
        public Color emptyDifficultyColor = new Color(1f, 1f, 1f, 0.25f);

        MissionSelectionMenu owner;
        MissionDifficulty selectedDifficulty;
        Vector3 baseScale;
        Coroutine scaleRoutine;
        Coroutine glowRoutine;
        Coroutine difficultyGlowRoutine;
        Coroutine[] popupDifficultyHoverRoutines;
        Image[] popupDifficultyImages;
        Color[] popupDifficultyBaseColors;
        Image difficultySelectorBackgroundImage;
        Color difficultySelectorDefaultColor;
        bool selected;
        bool hovering;
        bool difficultyHovering;
        bool popupOpen;
        bool[] popupDifficultyHovering;

        void Awake()
        {
            baseScale = transform.localScale;
            if (button == null)
                button = GetComponent<Button>();

            BindButtonEvents();
            BindPopupEvents();
            CachePopupDifficultyVisuals();
            CacheDifficultySelectorVisuals();
            if (missionData != null)
                Refresh();
            SetImageAlpha(difficultySelectorGlowImage, difficultyButtonIdleGlowAlpha);
        }

        public void Setup(MissionData data, MissionSelectionMenu menu)
        {
            missionData = data;
            owner = menu;

            BindButtonEvents();
            BindPopupEvents();
            CachePopupDifficultyVisuals();
            CacheDifficultySelectorVisuals();
            Refresh();
            CloseDifficultySelector(true);
            SetSelected(false, true);
            SetImageAlpha(difficultySelectorGlowImage, difficultyButtonIdleGlowAlpha);
        }

        void BindButtonEvents()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(OnClicked);
                button.onClick.AddListener(OnClicked);
            }

            Button selector = difficultySelectorButton;
            if (selector != null)
            {
                selector.onClick.RemoveAllListeners();
                selector.onClick.AddListener(OpenDifficultySelector);
                return;
            }

            if (difficultyButtons == null)
                return;

            for (int i = 0; i < difficultyButtons.Length; i++)
            {
                if (difficultyButtons[i] == null)
                    continue;

                difficultyButtons[i].onClick.RemoveAllListeners();
                difficultyButtons[i].onClick.AddListener(OpenDifficultySelector);
            }
        }

        void OpenDifficultySelector()
        {
            if (missionData == null || owner == null || difficultyPopupRoot == null)
                return;

            if (popupOpen)
            {
                CloseDifficultySelector();
                return;
            }

            owner.CloseDifficultyPopups(this);
            RefreshPopupOptions();
            ResetPopupDifficultyHover(true);
            difficultyPopupRoot.SetActive(true);
            popupOpen = true;

            if (difficultyPopupCanvasGroup != null)
            {
                difficultyPopupCanvasGroup.alpha = 1f;
                difficultyPopupCanvasGroup.interactable = true;
                difficultyPopupCanvasGroup.blocksRaycasts = true;
            }
        }

        void BindPopupEvents()
        {
            if (popupDifficultyButtons == null)
                return;

            for (int i = 0; i < popupDifficultyButtons.Length; i++)
            {
                if (popupDifficultyButtons[i] == null)
                    continue;

                int capturedIndex = i;
                popupDifficultyButtons[i].onClick.RemoveAllListeners();
                popupDifficultyButtons[i].onClick.AddListener(() => SelectDifficultyFromPopup((MissionDifficulty)(capturedIndex + 1)));
            }
        }

        void CachePopupDifficultyVisuals()
        {
            if (popupDifficultyButtons == null)
                return;

            int count = popupDifficultyButtons.Length;
            popupDifficultyImages = new Image[count];
            popupDifficultyBaseColors = new Color[count];
            popupDifficultyHovering = new bool[count];
            popupDifficultyHoverRoutines = new Coroutine[count];

            for (int i = 0; i < count; i++)
            {
                if (popupDifficultyButtons[i]?.targetGraphic is not Image image)
                    continue;

                popupDifficultyImages[i] = image;
                popupDifficultyBaseColors[i] = image.color;
            }
        }

        void CacheDifficultySelectorVisuals()
        {
            if (difficultySelectorButton?.targetGraphic is not Image image)
                return;

            difficultySelectorBackgroundImage = image;
            difficultySelectorDefaultColor = image.color;
        }

        void RefreshPopupOptions()
        {
            for (int i = 0; i < 3; i++)
            {
                MissionDifficulty difficulty = (MissionDifficulty)(i + 1);
                bool unlocked = missionData.IsDifficultyUnlocked(difficulty);
                bool completed = missionData.IsDifficultyCompleted(difficulty);

                if (popupDifficultyButtons != null && i < popupDifficultyButtons.Length && popupDifficultyButtons[i] != null)
                    popupDifficultyButtons[i].interactable = unlocked;

                if (popupDifficultyStatuses != null && i < popupDifficultyStatuses.Length && popupDifficultyStatuses[i] != null)
                    popupDifficultyStatuses[i].text = completed ? "Completada" : unlocked ? "Disponible" : "Bloqueada";
            }
        }

        void SelectDifficultyFromPopup(MissionDifficulty difficulty)
        {
            SelectDifficulty(difficulty);
            CloseDifficultySelector();
        }

        public void CloseDifficultySelector(bool instant = false)
        {
            popupOpen = false;
            ResetPopupDifficultyHover(instant);

            if (difficultyPopupCanvasGroup != null)
            {
                difficultyPopupCanvasGroup.interactable = false;
                difficultyPopupCanvasGroup.blocksRaycasts = false;
            }

            if (difficultyPopupRoot != null)
                difficultyPopupRoot.SetActive(false);
        }

        void Update()
        {
            UpdateDifficultyButtonHover();
            UpdatePopupDifficultyHover();

            if (!popupOpen || difficultyPopupRoot == null || !Input.GetMouseButtonDown(0))
                return;

            Vector2 mousePosition = Input.mousePosition;
            Camera eventCamera = null;
            RectTransform popupRect = difficultyPopupRoot.transform as RectTransform;
            RectTransform selectorRect = difficultySelectorButton != null
                ? difficultySelectorButton.transform as RectTransform
                : null;

            bool insidePopup = popupRect != null && RectTransformUtility.RectangleContainsScreenPoint(popupRect, mousePosition, eventCamera);
            bool insideSelector = selectorRect != null && RectTransformUtility.RectangleContainsScreenPoint(selectorRect, mousePosition, eventCamera);

            if (!insidePopup && !insideSelector)
                CloseDifficultySelector();
        }

        void UpdateDifficultyButtonHover()
        {
            if (difficultySelectorButton == null || difficultySelectorGlowImage == null)
                return;

            RectTransform selectorRect = difficultySelectorButton.transform as RectTransform;
            bool isHovering = selectorRect != null &&
                RectTransformUtility.RectangleContainsScreenPoint(selectorRect, Input.mousePosition, null);

            if (isHovering == difficultyHovering)
                return;

            difficultyHovering = isHovering;
            AnimateDifficultyGlow(difficultyHovering ? difficultyButtonHoverGlowAlpha : difficultyButtonIdleGlowAlpha);
        }

        void UpdatePopupDifficultyHover()
        {
            if (!popupOpen || popupDifficultyButtons == null || popupDifficultyHovering == null)
                return;

            for (int i = 0; i < popupDifficultyButtons.Length; i++)
            {
                Button popupButton = popupDifficultyButtons[i];
                if (popupButton == null || popupDifficultyImages == null || i >= popupDifficultyImages.Length || popupDifficultyImages[i] == null)
                    continue;

                bool isHoveringButton = popupButton.interactable &&
                    popupButton.transform is RectTransform rect &&
                    RectTransformUtility.RectangleContainsScreenPoint(rect, Input.mousePosition, null);

                if (popupDifficultyHovering[i] == isHoveringButton)
                    continue;

                popupDifficultyHovering[i] = isHoveringButton;
                AnimatePopupDifficultyHover(i, isHoveringButton);
            }
        }

        void ResetPopupDifficultyHover(bool instant)
        {
            if (popupDifficultyHovering == null)
                return;

            for (int i = 0; i < popupDifficultyHovering.Length; i++)
            {
                popupDifficultyHovering[i] = false;
                if (instant)
                {
                    SetPopupDifficultyColor(i, 0f);
                    continue;
                }

                AnimatePopupDifficultyHover(i, false);
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
            RefreshDifficultySelectorColor();
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
            {
                MissionDifficulty diff = MissionDifficulty.Facil;
                if (!missionData.IsDifficultyUnlocked(diff))
                {
                    for (int i = 1; i < 3; i++)
                    {
                        MissionDifficulty d = (MissionDifficulty)(i + 1);
                        if (missionData.IsDifficultyUnlocked(d))
                        { diff = d; break; }
                    }
                }
                selectedDifficulty = diff;
                RefreshDifficultySelectorColor();
                owner.SelectMission(this, missionData, selectedDifficulty);
            }
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
            RefreshDifficultyButtons();
            RefreshDifficultySelectorColor();
        }

        void RefreshDifficultySelectorColor()
        {
            if (difficultySelectorBackgroundImage == null)
                return;

            if (selectedDifficulty == 0)
            {
                difficultySelectorBackgroundImage.color = difficultySelectorDefaultColor;
                return;
            }

            int difficultyIndex = (int)selectedDifficulty - 1;
            if (popupDifficultyBaseColors != null &&
                difficultyIndex >= 0 &&
                difficultyIndex < popupDifficultyBaseColors.Length)
            {
                difficultySelectorBackgroundImage.color = popupDifficultyBaseColors[difficultyIndex];
                return;
            }

            difficultySelectorBackgroundImage.color = difficultySelectorDefaultColor;
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
            SetImageAlpha(glowImage, alpha);
        }

        void AnimatePopupDifficultyHover(int index, bool isHoveringButton)
        {
            if (popupDifficultyImages == null || popupDifficultyHoverRoutines == null || index < 0 || index >= popupDifficultyImages.Length)
                return;

            if (popupDifficultyHoverRoutines[index] != null)
                StopCoroutine(popupDifficultyHoverRoutines[index]);

            popupDifficultyHoverRoutines[index] = StartCoroutine(LerpPopupDifficultyColor(index, isHoveringButton ? popupDifficultyHoverLighten : 0f));
        }

        IEnumerator LerpPopupDifficultyColor(int index, float targetLighten)
        {
            if (popupDifficultyImages == null || popupDifficultyBaseColors == null || index < 0 || index >= popupDifficultyImages.Length || popupDifficultyImages[index] == null)
                yield break;

            Image image = popupDifficultyImages[index];
            Color start = image.color;
            Color target = GetPopupDifficultyTargetColor(index, targetLighten);
            float elapsed = 0f;

            while (elapsed < scaleDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / scaleDuration);
                image.color = Color.Lerp(start, target, t);
                yield return null;
            }

            image.color = target;
            popupDifficultyHoverRoutines[index] = null;
        }

        void SetPopupDifficultyColor(int index, float lightenAmount)
        {
            if (popupDifficultyImages == null || popupDifficultyBaseColors == null || index < 0 || index >= popupDifficultyImages.Length || popupDifficultyImages[index] == null)
                return;

            popupDifficultyImages[index].color = GetPopupDifficultyTargetColor(index, lightenAmount);
        }

        Color GetPopupDifficultyTargetColor(int index, float lightenAmount)
        {
            Color baseColor = popupDifficultyBaseColors[index];
            return Color.Lerp(baseColor, Color.white, lightenAmount);
        }

        void AnimateDifficultyGlow(float targetAlpha)
        {
            if (difficultySelectorGlowImage == null)
                return;

            if (difficultyGlowRoutine != null)
                StopCoroutine(difficultyGlowRoutine);

            difficultyGlowRoutine = StartCoroutine(LerpImageAlpha(difficultySelectorGlowImage, targetAlpha));
        }

        IEnumerator LerpImageAlpha(Image image, float targetAlpha)
        {
            float start = image.color.a;
            float elapsed = 0f;

            while (elapsed < scaleDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / scaleDuration);
                SetImageAlpha(image, Mathf.Lerp(start, targetAlpha, t));
                yield return null;
            }

            SetImageAlpha(image, targetAlpha);
        }

        void SetImageAlpha(Image image, float alpha)
        {
            if (image == null)
                return;

            Color color = image.color;
            color.a = alpha;
            image.color = color;
        }
    }
}
