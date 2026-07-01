using System.Collections;
using UnityEngine;
using JuegoDeCartas.Managers;

namespace JuegoDeCartas.UI
{
    public class PauseMenu : MonoBehaviour
    {
        [Header("Menu")]
        public GameObject optionsMenu;
        public SettingsMenuUI settingsMenu;

        [Header("Key")]
        public KeyCode toggleKey = KeyCode.Escape;

        [Header("Animation")]
        public RectTransform menuTransform;

        public float animationDuration = 0.25f;

        private Vector3 closedScale = Vector3.zero;
        private Vector3 openedScale = Vector3.one;

        private Coroutine currentRoutine;
        private float previousTimeScale = 1f;

        void Start()
        {
            CacheSettingsMenu();

            if (menuTransform != null)
                menuTransform.localScale = closedScale;

            if (optionsMenu != null)
                optionsMenu.SetActive(false);
        }

        void Update()
        {
            if (optionsMenu != null && Input.GetKeyDown(toggleKey))
            {
                if (optionsMenu.activeSelf)
                    ResumeGame();
                else
                    OpenMenu();
            }
        }

        public void OpenMenu()
        {
            CacheSettingsMenu();
            if (optionsMenu == null)
                return;

            previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;

            optionsMenu.SetActive(true);
            if (settingsMenu != null)
                settingsMenu.Bind(this);

            if (currentRoutine != null)
                StopCoroutine(currentRoutine);

            currentRoutine = StartCoroutine(ScaleMenu(openedScale));
        }

        public void ResumeGame()
        {
            if (optionsMenu == null)
                return;

            if (currentRoutine != null)
                StopCoroutine(currentRoutine);

            currentRoutine = StartCoroutine(CloseRoutine());
        }

        IEnumerator CloseRoutine()
        {
            yield return StartCoroutine(ScaleMenu(closedScale));

            optionsMenu.SetActive(false);

            Time.timeScale = previousTimeScale;
        }

        void CacheSettingsMenu()
        {
            if (settingsMenu == null && optionsMenu != null)
                settingsMenu = optionsMenu.GetComponentInChildren<SettingsMenuUI>(true);

            if (settingsMenu == null)
                settingsMenu = FindBestSettingsMenu();

            if (settingsMenu == null)
                return;

            optionsMenu = settingsMenu.gameObject;
            if (menuTransform == null)
            {
                Transform panel = optionsMenu.transform.Find("Panel");
                menuTransform = panel as RectTransform;
            }
        }

        static SettingsMenuUI FindBestSettingsMenu()
        {
            SettingsMenuUI[] menus = FindObjectsByType<SettingsMenuUI>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );
            SettingsMenuUI fallback = null;
            for (int i = 0; i < menus.Length; i++)
            {
                if (menus[i] == null)
                    continue;

                if (menus[i].gameObject.name == "OptionsMenu")
                    return menus[i];

                if (fallback == null)
                    fallback = menus[i];
            }
            return fallback;
        }

        IEnumerator ScaleMenu(Vector3 targetScale)
        {
            if (menuTransform == null)
                yield break;

            Vector3 startScale = menuTransform.localScale;

            float time = 0f;

            while (time < animationDuration)
            {
                time += Time.unscaledDeltaTime;

                float t = time / animationDuration;

                menuTransform.localScale =
                    Vector3.Lerp(startScale, targetScale, t);

                yield return null;
            }

            menuTransform.localScale = targetScale;
        }
        public void BackToMenu()
        {
            Time.timeScale = 1f;
            RunStateCoordinator.Reset();
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
