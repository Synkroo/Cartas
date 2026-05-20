using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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
        GraphicRaycaster menusRaycaster;
        List<GraphicRaycaster> disabledRaycasters = new List<GraphicRaycaster>();

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

            // Ensure the Menus Canvas has a raycaster for its buttons
            if (menusRaycaster == null && canvas != null)
                menusRaycaster = canvas.GetComponent<GraphicRaycaster>() ?? canvas.gameObject.AddComponent<GraphicRaycaster>();

            // Disable all other raycaster so game UI is unclickable
            var all = FindObjectsByType<GraphicRaycaster>(FindObjectsSortMode.None);
            disabledRaycasters.Clear();
            foreach (var rc in all)
            {
                if (rc != menusRaycaster && rc.enabled)
                {
                    rc.enabled = false;
                    disabledRaycasters.Add(rc);
                }
            }

            if (canvas != null)
                canvas.enabled = true;

            panel.SetActive(true);
        }

        public void HideAll()
        {
            foreach (var rc in disabledRaycasters)
                if (rc != null) rc.enabled = true;
            disabledRaycasters.Clear();

            if (victoryPanel != null) victoryPanel.SetActive(false);
            if (defeatPanel != null) defeatPanel.SetActive(false);

            if (canvas != null)
                canvas.enabled = false;

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
