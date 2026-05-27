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
