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
            if (wasActive)
                RefreshEntries();
            else if (canvasGroup != null)
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

            if (missions.Count == 0 && missionPrefab.missionData != null)
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

        // ── Difficulty selection panel ──────────────────────────────────────

        MissionEntryUI pendingMissionEntry;
        MissionData pendingMissionData;
        GameObject diffPanelRoot;
        CanvasGroup diffPanelGroup;
        TextMeshProUGUI diffTitleText;
        Button[] diffOptionButtons;
        TextMeshProUGUI[] diffOptionLabels;
        TextMeshProUGUI[] diffOptionStatuses;
        GameObject diffOverlay;

        public void OnMissionClicked(MissionEntryUI entry, MissionData mission)
        {
            pendingMissionEntry = entry;
            pendingMissionData = mission;
            ShowDifficultyPanel();
        }

        void ShowDifficultyPanel()
        {
            if (diffPanelRoot == null)
                CreateDifficultyPanel();

            if (pendingMissionData == null) return;

            diffTitleText.text = pendingMissionData.missionName;

            string[] names = { "Facil", "Media", "Letal" };
            string[] descs =
            {
                "Sin modificadores",
                "Enemigos con +20% de estadisticas",
                "Enemigos con +20% de estadisticas\ny tienda +20% mas cara"
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

                int captured = i;
                diffOptionButtons[i].onClick.RemoveAllListeners();
                diffOptionButtons[i].onClick.AddListener(() =>
                    SelectDifficultyForPending((MissionDifficulty)(captured + 1)));
            }

            if (canvasGroup != null)
            {
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }

            diffOverlay.SetActive(true);
            diffPanelRoot.SetActive(true);
            diffPanelGroup.alpha = 0f;
            diffPanelGroup.blocksRaycasts = true;
            diffPanelGroup.interactable = true;
            StartCoroutine(FadePanelRoutine(1f));
        }

        void SelectDifficultyForPending(MissionDifficulty difficulty)
        {
            if (pendingMissionEntry != null && pendingMissionData != null)
                SelectMission(pendingMissionEntry, pendingMissionData, difficulty);
            HideDifficultyPanel();
        }

        void HideDifficultyPanel()
        {
            if (diffPanelRoot != null)
                diffPanelRoot.SetActive(false);
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

            diffOverlay = new GameObject("DiffOverlay", typeof(RectTransform), typeof(Image));
            diffOverlay.transform.SetParent(parent, false);
            diffOverlay.transform.SetAsLastSibling();
            RectTransform olRT = diffOverlay.GetComponent<RectTransform>();
            olRT.anchorMin = Vector2.zero;
            olRT.anchorMax = Vector2.one;
            olRT.sizeDelta = Vector2.zero;
            Image olImg = diffOverlay.GetComponent<Image>();
            olImg.color = new Color(0, 0, 0, 0.6f);
            olImg.raycastTarget = true;
            diffOverlay.SetActive(false);

            diffPanelRoot = new GameObject("DifficultyPanel", typeof(RectTransform),
                typeof(Image), typeof(CanvasGroup));
            diffPanelRoot.transform.SetParent(parent, false);
            diffPanelRoot.transform.SetAsLastSibling();
            RectTransform rt = diffPanelRoot.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(4, 5);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            Image bg = diffPanelRoot.GetComponent<Image>();
            bg.color = new Color(0.12f, 0.12f, 0.12f, 1f);
            diffPanelGroup = diffPanelRoot.GetComponent<CanvasGroup>();

            // Title
            GameObject titleGO = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleGO.transform.SetParent(diffPanelRoot.transform, false);
            RectTransform titleRT = titleGO.GetComponent<RectTransform>();
            titleRT.anchorMin = new Vector2(0, 1);
            titleRT.anchorMax = new Vector2(1, 1);
            titleRT.pivot = new Vector2(0.5f, 1);
            titleRT.sizeDelta = new Vector2(0, 0.6f);
            titleRT.anchoredPosition = new Vector2(0, -0.3f);
            diffTitleText = titleGO.GetComponent<TextMeshProUGUI>();
            diffTitleText.fontSize = 0.45f;
            diffTitleText.alignment = TextAlignmentOptions.Center;
            diffTitleText.text = "";

            // 3 difficulty option buttons
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
                float yPos = 1.8f - i * 1.2f;

                GameObject btn = new GameObject("DiffOption" + (i + 1),
                    typeof(RectTransform), typeof(Image), typeof(Button));
                btn.transform.SetParent(diffPanelRoot.transform, false);
                RectTransform btnRT = btn.GetComponent<RectTransform>();
                btnRT.anchorMin = new Vector2(0.5f, 0.5f);
                btnRT.anchorMax = new Vector2(0.5f, 0.5f);
                btnRT.sizeDelta = new Vector2(3.5f, 0.9f);
                btnRT.anchoredPosition = new Vector2(0, yPos);
                Image btnImg = btn.GetComponent<Image>();
                btnImg.color = diffColors[i];
                diffOptionButtons[i] = btn.GetComponent<Button>();

                // Label (name + description)
                GameObject lbl = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
                lbl.transform.SetParent(btn.transform, false);
                RectTransform lblRT = lbl.GetComponent<RectTransform>();
                lblRT.anchorMin = Vector2.zero;
                lblRT.anchorMax = new Vector2(0.7f, 1f);
                lblRT.sizeDelta = Vector2.zero;
                diffOptionLabels[i] = lbl.GetComponent<TextMeshProUGUI>();
                diffOptionLabels[i].fontSize = 0.3f;
                diffOptionLabels[i].alignment = TextAlignmentOptions.Left;
                diffOptionLabels[i].text = "";

                // Status
                GameObject sts = new GameObject("Status", typeof(RectTransform), typeof(TextMeshProUGUI));
                sts.transform.SetParent(btn.transform, false);
                RectTransform stsRT = sts.GetComponent<RectTransform>();
                stsRT.anchorMin = new Vector2(0.7f, 0);
                stsRT.anchorMax = new Vector2(1f, 1f);
                stsRT.sizeDelta = Vector2.zero;
                diffOptionStatuses[i] = sts.GetComponent<TextMeshProUGUI>();
                diffOptionStatuses[i].fontSize = 0.25f;
                diffOptionStatuses[i].alignment = TextAlignmentOptions.MiddleRight;
                diffOptionStatuses[i].text = "";
            }

            // Back button
            GameObject backBtn = new GameObject("BackButton", typeof(RectTransform),
                typeof(Image), typeof(Button));
            backBtn.transform.SetParent(diffPanelRoot.transform, false);
            RectTransform backRT = backBtn.GetComponent<RectTransform>();
            backRT.anchorMin = new Vector2(0.5f, 0f);
            backRT.anchorMax = new Vector2(0.5f, 0f);
            backRT.sizeDelta = new Vector2(2f, 0.5f);
            backRT.anchoredPosition = new Vector2(0, 0.3f);
            Image backImg = backBtn.GetComponent<Image>();
            backImg.color = new Color(0.3f, 0.3f, 0.3f, 1f);
            Button diffBackButton = backBtn.GetComponent<Button>();
            diffBackButton.onClick.AddListener(HideDifficultyPanel);

            GameObject backLbl = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            backLbl.transform.SetParent(backBtn.transform, false);
            RectTransform backLblRT = backLbl.GetComponent<RectTransform>();
            backLblRT.anchorMin = Vector2.zero;
            backLblRT.anchorMax = Vector2.one;
            backLblRT.sizeDelta = Vector2.zero;
            TextMeshProUGUI backTMP = backLbl.GetComponent<TextMeshProUGUI>();
            backTMP.fontSize = 0.3f;
            backTMP.alignment = TextAlignmentOptions.Center;
            backTMP.text = "Volver";

            diffPanelRoot.SetActive(false);
        }

        IEnumerator FadePanelRoutine(float targetAlpha)
        {
            float start = diffPanelGroup.alpha;
            float elapsed = 0f;
            while (elapsed < diffPanelFadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                diffPanelGroup.alpha = Mathf.Lerp(start, targetAlpha,
                    Mathf.Clamp01(elapsed / diffPanelFadeDuration));
                yield return null;
            }
            diffPanelGroup.alpha = targetAlpha;
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
