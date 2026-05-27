using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace JuegoDeCartas.Missions
{
    public class MissionSelectionMenu : MonoBehaviour
    {
        [Header("Panel")]
        public GameObject panel;
        public CanvasGroup canvasGroup;

        [Header("Missions")]
        public List<MissionData> missions = new List<MissionData>();
        public Transform content;
        public MissionEntryUI missionPrefab;

        [Header("Buttons")]
        public Button startButton;
        public TextMeshProUGUI startButtonText;
        public string gameSceneName = "Game";

        [Header("Animation")]
        [Min(0.01f)] public float fadeDuration = 0.2f;
        [Min(0.01f)] public float diffPanelFadeDuration = 0.15f;

        readonly List<MissionEntryUI> entries = new List<MissionEntryUI>();
        MissionEntryUI selectedEntry;
        MissionData selectedMission;
        MissionDifficulty selectedDifficulty;
        Coroutine fadeRoutine;

        void Awake()
        {
            if (panel == null)
                panel = gameObject;

            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();

            if (startButton != null)
                startButton.onClick.AddListener(StartSelectedMission);

            SetStartButtonState(false);
        }

        void OnEnable()
        {
            RefreshEntries();
        }

        public void Open()
        {
            if (panel == null)
                return;

            bool wasActive = panel.activeSelf;
            panel.SetActive(true);

            RefreshEntries();

            if (!wasActive && canvasGroup != null)
                canvasGroup.alpha = 0f;
            FadeTo(1f);
        }

        public void Close()
        {
            FadeTo(0f, () =>
            {
                if (panel != null)
                    panel.SetActive(false);
            });
        }

        public void RefreshEntries()
        {
            ClearEntries();

            if (content == null || missionPrefab == null)
                return;

            bool hasValidMission = false;
            for (int i = 0; i < missions.Count; i++)
            {
                if (missions[i] != null)
                {
                    hasValidMission = true;
                    break;
                }
            }

            if (!hasValidMission && missionPrefab.missionData != null)
                missions.Add(missionPrefab.missionData);

            for (int i = 0; i < missions.Count; i++)
            {
                if (missions[i] == null)
                    continue;

                MissionEntryUI entry = Instantiate(missionPrefab, content);
                entry.gameObject.SetActive(true);
                entry.Setup(missions[i], this);
                entries.Add(entry);
            }

            SelectMission(null, null, MissionDifficulty.Facil);
        }

        void ClearEntries()
        {
            selectedEntry = null;
            selectedMission = null;
            selectedDifficulty = MissionDifficulty.Facil;

            for (int i = entries.Count - 1; i >= 0; i--)
            {
                if (entries[i] != null)
                    Destroy(entries[i].gameObject);
            }

            entries.Clear();
            SetStartButtonState(false);
        }

        public void SelectMission(MissionEntryUI entry, MissionData mission, MissionDifficulty difficulty)
        {
            selectedEntry = entry;
            selectedMission = mission;
            selectedDifficulty = difficulty;

            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i] != null)
                    entries[i].SetSelected(entries[i] == selectedEntry);
            }

            SetStartButtonState(selectedMission != null);
        }

        void SetStartButtonState(bool hasSelection)
        {
            if (startButton != null)
                startButton.interactable = hasSelection;

            if (startButtonText != null)
                startButtonText.text = hasSelection ? "Jugar" : "Selecciona una mision";
        }

        public void StartSelectedMission()
        {
            if (selectedMission == null)
                return;

            MissionRunState.SelectMission(selectedMission, selectedDifficulty);
            SceneManager.LoadScene(gameSceneName);
        }

        // ── Difficulty popup ────────────────────────────────────────────────

        MissionEntryUI pendingMissionEntry;
        MissionData pendingMissionData;
        GameObject diffPopupRoot;
        CanvasGroup diffPopupGroup;
        TextMeshProUGUI diffTitleText;
        Button[] diffOptionButtons;
        TextMeshProUGUI[] diffOptionLabels;
        TextMeshProUGUI[] diffOptionStatuses;
        GameObject diffOverlay;

        public void OpenDifficultyPopup(MissionEntryUI entry, MissionData mission)
        {
            if (diffOverlay != null && diffOverlay.activeSelf)
            {
                HideDifficultyPanel();
                return;
            }

            pendingMissionEntry = entry;
            pendingMissionData = mission;
            ShowDifficultyPanel();
        }

        void ShowDifficultyPanel()
        {
            if (diffPopupRoot == null)
                CreateDifficultyPanel();

            if (pendingMissionData == null) return;

            diffTitleText.text = pendingMissionData.missionName;

            string[] names = { "Facil", "Media", "Letal" };
            string[] descs =
            {
                "Sin modificadores",
                "Enemigos +20% estadisticas",
                "Enemigos +20% estadisticas\ny tienda +20% cara"
            };

            for (int i = 0; i < 3; i++)
            {
                MissionDifficulty diff = (MissionDifficulty)(i + 1);
                bool unlocked = pendingMissionData.IsDifficultyUnlocked(diff);
                bool completed = pendingMissionData.IsDifficultyCompleted(diff);

                diffOptionLabels[i].text = names[i] + "\n<size=70%>" + descs[i] + "</size>";

                if (completed)
                    diffOptionStatuses[i].text = "COMPLETADO";
                else if (unlocked)
                    diffOptionStatuses[i].text = "DISPONIBLE";
                else
                    diffOptionStatuses[i].text = "BLOQUEADO";

                diffOptionStatuses[i].color = completed
                    ? new Color(1f, 0.84f, 0f)
                    : unlocked ? Color.green : Color.gray;

                diffOptionButtons[i].interactable = unlocked;
                ColorBlock cb = diffOptionButtons[i].colors;
                cb.disabledColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
                diffOptionButtons[i].colors = cb;

                int captured = i;
                diffOptionButtons[i].onClick.RemoveAllListeners();
                diffOptionButtons[i].onClick.AddListener(() =>
                    SelectDifficultyForPending((MissionDifficulty)(captured + 1)));
            }

            PositionPopupAboveEntry();

            if (canvasGroup != null)
            {
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }

            diffOverlay.SetActive(true);
            diffPopupRoot.SetActive(true);
            diffPopupGroup.alpha = 0f;
            diffPopupGroup.blocksRaycasts = true;
            diffPopupGroup.interactable = true;
            StartCoroutine(FadePopupRoutine(1f));
        }

        void PositionPopupAboveEntry()
        {
            if (pendingMissionEntry == null || diffPopupRoot == null) return;

            RectTransform entryRT = pendingMissionEntry.GetComponent<RectTransform>();
            Vector3[] corners = new Vector3[4];
            entryRT.GetWorldCorners(corners);
            float entryTopY = corners[1].y;
            float entryCenterX = (corners[0].x + corners[2].x) / 2f;

            Canvas canvas = GetComponentInParent<Canvas>();
            float scale = canvas != null ? canvas.scaleFactor : 1f;
            RectTransform popupRT = diffPopupRoot.GetComponent<RectTransform>();
            float popupHalfScreenW = popupRT.sizeDelta.x * scale * 0.5f;

            float clampedX = Mathf.Clamp(entryCenterX, popupHalfScreenW + 5f, Screen.width - popupHalfScreenW - 5f);

            diffPopupRoot.transform.position = new Vector3(clampedX, entryTopY + 10f * scale, entryRT.position.z);
        }

        void SelectDifficultyForPending(MissionDifficulty difficulty)
        {
            if (pendingMissionEntry != null && pendingMissionData != null)
                SelectMission(pendingMissionEntry, pendingMissionData, difficulty);
            HideDifficultyPanel();
        }

        void HideDifficultyPanel()
        {
            if (diffPopupRoot != null)
                diffPopupRoot.SetActive(false);
            if (diffOverlay != null)
                diffOverlay.SetActive(false);

            if (canvasGroup != null)
            {
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }

            pendingMissionEntry = null;
            pendingMissionData = null;
        }

        void CreateDifficultyPanel()
        {
            Transform parent = panel != null ? panel.transform : transform;

            diffOverlay = new GameObject("DiffOverlay", typeof(RectTransform), typeof(Image), typeof(Button));
            diffOverlay.transform.SetParent(parent, false);
            diffOverlay.transform.SetAsLastSibling();
            RectTransform olRT = diffOverlay.GetComponent<RectTransform>();
            olRT.anchorMin = Vector2.zero;
            olRT.anchorMax = Vector2.one;
            olRT.sizeDelta = Vector2.zero;
            Image olImg = diffOverlay.GetComponent<Image>();
            olImg.color = Color.clear;
            olImg.raycastTarget = true;
            Button olBtn = diffOverlay.GetComponent<Button>();
            olBtn.transition = Selectable.Transition.None;
            olBtn.onClick.AddListener(HideDifficultyPanel);
            diffOverlay.SetActive(false);

            diffPopupRoot = new GameObject("DifficultyPopup", typeof(RectTransform),
                typeof(Image), typeof(CanvasGroup));
            diffPopupRoot.transform.SetParent(parent, false);
            diffPopupRoot.transform.SetAsLastSibling();
            RectTransform rt = diffPopupRoot.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(2.5f, 2.6f);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 1f);
            Image bg = diffPopupRoot.GetComponent<Image>();
            bg.color = new Color(0.12f, 0.12f, 0.12f, 1f);
            diffPopupGroup = diffPopupRoot.GetComponent<CanvasGroup>();

            // Title
            GameObject titleGO = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleGO.transform.SetParent(diffPopupRoot.transform, false);
            RectTransform titleRT = titleGO.GetComponent<RectTransform>();
            titleRT.anchorMin = new Vector2(0, 1);
            titleRT.anchorMax = new Vector2(1, 1);
            titleRT.pivot = new Vector2(0.5f, 1);
            titleRT.sizeDelta = new Vector2(0, 0.4f);
            titleRT.anchoredPosition = new Vector2(0, -0.2f);
            diffTitleText = titleGO.GetComponent<TextMeshProUGUI>();
            diffTitleText.fontSize = 0.3f;
            diffTitleText.alignment = TextAlignmentOptions.Center;
            diffTitleText.text = "";

            Color[] diffColors = {
                new Color(0.2f, 0.6f, 0.3f, 1f),
                new Color(0.8f, 0.5f, 0.1f, 1f),
                new Color(0.7f, 0.15f, 0.15f, 1f)
            };

            diffOptionButtons = new Button[3];
            diffOptionLabels = new TextMeshProUGUI[3];
            diffOptionStatuses = new TextMeshProUGUI[3];

            for (int i = 0; i < 3; i++)
            {
                float yPos = -0.8f - i * 0.95f;

                GameObject btn = new GameObject("DiffOption" + (i + 1),
                    typeof(RectTransform), typeof(Image), typeof(Button));
                btn.transform.SetParent(diffPopupRoot.transform, false);
                RectTransform btnRT = btn.GetComponent<RectTransform>();
                btnRT.anchorMin = new Vector2(0.5f, 0.5f);
                btnRT.anchorMax = new Vector2(0.5f, 0.5f);
                btnRT.sizeDelta = new Vector2(2.2f, 0.7f);
                btnRT.anchoredPosition = new Vector2(0, yPos);
                Image btnImg = btn.GetComponent<Image>();
                btnImg.color = diffColors[i];
                diffOptionButtons[i] = btn.GetComponent<Button>();

                GameObject lbl = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
                lbl.transform.SetParent(btn.transform, false);
                RectTransform lblRT = lbl.GetComponent<RectTransform>();
                lblRT.anchorMin = Vector2.zero;
                lblRT.anchorMax = new Vector2(0.7f, 1f);
                lblRT.sizeDelta = Vector2.zero;
                diffOptionLabels[i] = lbl.GetComponent<TextMeshProUGUI>();
                diffOptionLabels[i].fontSize = 0.22f;
                diffOptionLabels[i].alignment = TextAlignmentOptions.Left;
                diffOptionLabels[i].text = "";

                GameObject sts = new GameObject("Status", typeof(RectTransform), typeof(TextMeshProUGUI));
                sts.transform.SetParent(btn.transform, false);
                RectTransform stsRT = sts.GetComponent<RectTransform>();
                stsRT.anchorMin = new Vector2(0.7f, 0);
                stsRT.anchorMax = new Vector2(1f, 1f);
                stsRT.sizeDelta = Vector2.zero;
                diffOptionStatuses[i] = sts.GetComponent<TextMeshProUGUI>();
                diffOptionStatuses[i].fontSize = 0.18f;
                diffOptionStatuses[i].alignment = TextAlignmentOptions.Right;
                diffOptionStatuses[i].text = "";
            }

            diffPopupRoot.SetActive(false);
        }

        IEnumerator FadePopupRoutine(float targetAlpha)
        {
            float start = diffPopupGroup.alpha;
            float elapsed = 0f;
            while (elapsed < diffPanelFadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                diffPopupGroup.alpha = Mathf.Lerp(start, targetAlpha,
                    Mathf.Clamp01(elapsed / diffPanelFadeDuration));
                yield return null;
            }
            diffPopupGroup.alpha = targetAlpha;
        }

        void FadeTo(float targetAlpha, System.Action onComplete = null)
        {
            if (canvasGroup == null)
            {
                onComplete?.Invoke();
                return;
            }

            if (fadeRoutine != null)
                StopCoroutine(fadeRoutine);

            fadeRoutine = StartCoroutine(FadeRoutine(targetAlpha, onComplete));
        }

        IEnumerator FadeRoutine(float targetAlpha, System.Action onComplete)
        {
            float start = canvasGroup.alpha;
            float elapsed = 0f;

            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / fadeDuration);
                canvasGroup.alpha = Mathf.Lerp(start, targetAlpha, t);
                yield return null;
            }

            canvasGroup.alpha = targetAlpha;
            bool visible = targetAlpha > 0.01f;
            canvasGroup.blocksRaycasts = visible;
            canvasGroup.interactable = visible;
            onComplete?.Invoke();
        }
    }
}
