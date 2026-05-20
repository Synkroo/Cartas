using UnityEngine;

namespace JuegoDeCartas.UI
{
    public class EndGameMenu : MonoBehaviour
    {
        [Header("Panels")]
        public GameObject victoryPanel;
        public GameObject defeatPanel;

        [Header("Settings")]
        public bool pauseTime = true;

        Canvas canvas;

        void Awake()
        {
            canvas = GetComponentInParent<Canvas>();
            HideAll();
        }

        public void ShowVictory()
        {
            Show(victoryPanel);
        }

        public void ShowDefeat()
        {
            Show(defeatPanel);
        }

        void Show(GameObject panel)
        {
            if (pauseTime)
                Time.timeScale = 0f;

            if (canvas != null)
                canvas.enabled = true;

            panel.SetActive(true);
        }

        public void HideAll()
        {
            if (victoryPanel != null) victoryPanel.SetActive(false);
            if (defeatPanel != null) defeatPanel.SetActive(false);

            if (canvas != null)
                canvas.enabled = false;

            Time.timeScale = 1f;
        }

        public void RestartGame()
        {
            HideAll();
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
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
