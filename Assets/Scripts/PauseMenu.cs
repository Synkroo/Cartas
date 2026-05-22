using System.Collections;
using UnityEngine;

namespace JuegoDeCartas.UI
{
    public class PauseMenu : MonoBehaviour
    {
        [Header("Menu")]
        public GameObject optionsMenu;

        [Header("Key")]
        public KeyCode toggleKey = KeyCode.Escape;

        [Header("Animation")]
        public RectTransform menuTransform;

        public float animationDuration = 0.25f;

        private Vector3 closedScale = Vector3.zero;
        private Vector3 openedScale = Vector3.one;

        private Coroutine currentRoutine;

        void Start()
        {
            menuTransform.localScale = closedScale;

            optionsMenu.SetActive(false);
        }

        void Update()
        {
            if (Input.GetKeyDown(toggleKey))
            {
                if (optionsMenu.activeSelf)
                    ResumeGame();
                else
                    OpenMenu();
            }
        }

        public void OpenMenu()
        {
            optionsMenu.SetActive(true);

            if (currentRoutine != null)
                StopCoroutine(currentRoutine);

            currentRoutine = StartCoroutine(ScaleMenu(openedScale));
        }

        public void ResumeGame()
        {
            if (currentRoutine != null)
                StopCoroutine(currentRoutine);

            currentRoutine = StartCoroutine(CloseRoutine());
        }

        IEnumerator CloseRoutine()
        {
            yield return StartCoroutine(ScaleMenu(closedScale));

            optionsMenu.SetActive(false);
        }

        IEnumerator ScaleMenu(Vector3 targetScale)
        {
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
