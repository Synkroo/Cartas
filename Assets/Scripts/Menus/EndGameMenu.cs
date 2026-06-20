using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JuegoDeCartas.UI
{
    public class EndGameMenu : MonoBehaviour
    {
        [Header("Panels")]
        public GameObject victoryPanel;
        public GameObject defeatPanel;

        [Header("Canvas")]
        public Canvas menusCanvas;

        [Header("Settings")]
        public bool pauseTime = true;

        [Header("Animation")]
        public RectTransform victoryTitle;
        public RectTransform defeatTitle;
        public TextMeshProUGUI victoryStatsText;
        public TextMeshProUGUI defeatStatsText;
        public TextMeshProUGUI victorySecondaryStatsText;
        public TextMeshProUGUI defeatSecondaryStatsText;
        [Min(0.01f)] public float titleDuration = 0.3f;
        [Min(0f)] public float statsDelay = 0.12f;
        [Min(1f)] public float statsCharactersPerSecond = 95f;
        [Min(1f)] public float titleStartScale = 1.8f;
        public AnimationCurve titleCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        GraphicRaycaster menusRaycaster;
        readonly List<GraphicRaycaster> disabledRaycasters = new List<GraphicRaycaster>();
        readonly List<GraphicRaycaster> allRaycasters = new List<GraphicRaycaster>();
        bool raycastersCached;
        Coroutine animationRoutine;

        void Awake()
        {
            if (menusCanvas != null)
            {
                menusRaycaster = menusCanvas.GetComponent<GraphicRaycaster>()
                                 ?? menusCanvas.gameObject.AddComponent<GraphicRaycaster>();
            }
            HideAll();
        }

        void CacheRaycasters()
        {
            if (raycastersCached)
                return;

            allRaycasters.Clear();
            allRaycasters.AddRange(FindObjectsByType<GraphicRaycaster>(FindObjectsInactive.Include));
            raycastersCached = true;
        }

        public void ShowVictory()
        {
            Show(victoryPanel, victoryTitle, victoryStatsText, victorySecondaryStatsText);
        }

        public void ShowDefeat()
        {
            Show(defeatPanel, defeatTitle, defeatStatsText, defeatSecondaryStatsText);
        }

        void Show(
            GameObject panel,
            RectTransform title,
            TextMeshProUGUI statsText,
            TextMeshProUGUI secondaryStatsText)
        {
            if (pauseTime)
                Time.timeScale = 0f;

            CacheRaycasters();
            disabledRaycasters.Clear();
            foreach (GraphicRaycaster raycaster in allRaycasters)
            {
                if (raycaster != menusRaycaster && raycaster.enabled)
                {
                    raycaster.enabled = false;
                    disabledRaycasters.Add(raycaster);
                }
            }

            if (menusCanvas != null)
                menusCanvas.enabled = true;
            if (panel != null)
                panel.SetActive(true);

            if (animationRoutine != null)
                StopCoroutine(animationRoutine);
            animationRoutine = StartCoroutine(AnimateResult(title, statsText, secondaryStatsText));
        }

        IEnumerator AnimateResult(
            RectTransform title,
            TextMeshProUGUI statsText,
            TextMeshProUGUI secondaryStatsText)
        {
            Vector3 titleBaseScale = title != null ? title.localScale : Vector3.one;
            if (title != null)
                title.localScale = titleBaseScale * titleStartScale;
            if (statsText != null)
            {
                statsText.ForceMeshUpdate();
                statsText.maxVisibleCharacters = 0;
            }
            if (secondaryStatsText != null)
            {
                secondaryStatsText.ForceMeshUpdate();
                secondaryStatsText.maxVisibleCharacters = 0;
            }

            float elapsed = 0f;
            while (title != null && elapsed < titleDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = titleCurve.Evaluate(Mathf.Clamp01(elapsed / titleDuration));
                title.localScale = Vector3.LerpUnclamped(
                    titleBaseScale * titleStartScale,
                    titleBaseScale,
                    progress
                );
                yield return null;
            }

            if (title != null)
                title.localScale = titleBaseScale;
            if (statsDelay > 0f)
                yield return new WaitForSecondsRealtime(statsDelay);

            if (statsText != null || secondaryStatsText != null)
            {
                int primaryCharacterCount = GetCharacterCount(statsText);
                int secondaryCharacterCount = GetCharacterCount(secondaryStatsText);
                int characterCount = Mathf.Max(primaryCharacterCount, secondaryCharacterCount);
                elapsed = 0f;
                while (GetVisibleCharacters(statsText, secondaryStatsText) < characterCount)
                {
                    elapsed += Time.unscaledDeltaTime;
                    int visible = Mathf.Min(
                        characterCount,
                        Mathf.FloorToInt(elapsed * statsCharactersPerSecond));
                    if (statsText != null)
                        statsText.maxVisibleCharacters = Mathf.Min(primaryCharacterCount, visible);
                    if (secondaryStatsText != null)
                        secondaryStatsText.maxVisibleCharacters = Mathf.Min(secondaryCharacterCount, visible);
                    yield return null;
                }
            }

            animationRoutine = null;
        }

        static int GetCharacterCount(TextMeshProUGUI text)
        {
            if (text == null)
                return 0;
            text.ForceMeshUpdate();
            return text.textInfo.characterCount;
        }

        static int GetVisibleCharacters(TextMeshProUGUI primary, TextMeshProUGUI secondary)
        {
            return Mathf.Max(
                primary != null ? primary.maxVisibleCharacters : 0,
                secondary != null ? secondary.maxVisibleCharacters : 0
            );
        }

        public void HideAll()
        {
            if (animationRoutine != null)
            {
                StopCoroutine(animationRoutine);
                animationRoutine = null;
            }

            foreach (GraphicRaycaster raycaster in disabledRaycasters)
            {
                if (raycaster != null)
                    raycaster.enabled = true;
            }
            disabledRaycasters.Clear();

            if (victoryPanel != null)
                victoryPanel.SetActive(false);
            if (defeatPanel != null)
                defeatPanel.SetActive(false);
            if (menusCanvas != null)
                menusCanvas.enabled = false;

            Time.timeScale = 1f;
        }

        public void RestartGame()
        {
            HideAll();
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
        }

        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
