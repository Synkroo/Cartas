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

        [Header("Canvas")]
        public Canvas menusCanvas;

        [Header("Settings")]
        public bool pauseTime = true;

        GraphicRaycaster menusRaycaster;
        List<GraphicRaycaster> disabledRaycasters = new List<GraphicRaycaster>();
        List<GraphicRaycaster> allRaycasters = new List<GraphicRaycaster>();
        bool raycastersCached;

        void Awake()
        {
            if (menusCanvas != null)
                menusRaycaster = menusCanvas.GetComponent<GraphicRaycaster>()
                                 ?? menusCanvas.gameObject.AddComponent<GraphicRaycaster>();
            HideAll();
        }

        void CacheRaycasters()
        {
            if (raycastersCached) return;
            allRaycasters.Clear();
            allRaycasters.AddRange(FindObjectsByType<GraphicRaycaster>(FindObjectsSortMode.None));
            raycastersCached = true;
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

            CacheRaycasters();
            disabledRaycasters.Clear();
            foreach (var rc in allRaycasters)
            {
                if (rc != menusRaycaster && rc.enabled)
                {
                    rc.enabled = false;
                    disabledRaycasters.Add(rc);
                }
            }

            if (menusCanvas != null)
                menusCanvas.enabled = true;

            panel.SetActive(true);
        }

        public void HideAll()
        {
            foreach (var rc in disabledRaycasters)
                if (rc != null) rc.enabled = true;
            disabledRaycasters.Clear();

            if (victoryPanel != null) victoryPanel.SetActive(false);
            if (defeatPanel != null) defeatPanel.SetActive(false);

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
