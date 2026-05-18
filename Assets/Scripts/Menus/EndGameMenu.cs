using UnityEngine;
using UnityEngine.UI;
using TMPro;

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
        CanvasScaler scaler;

        void Awake()
        {
            if (victoryPanel == null || defeatPanel == null)
                BuildUI();
            else
            {
                canvas = GetComponentInParent<Canvas>();
                HideAll();
            }
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

        void BuildUI()
        {
            GameObject menuRoot = gameObject;

            canvas = menuRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            scaler = menuRoot.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            menuRoot.AddComponent<GraphicRaycaster>();

            victoryPanel = BuildPanel("Menu victoria", true, "¡VICTORIA!", new Color32(255, 215, 0, 255));
            defeatPanel = BuildPanel("Menu derrota", false, "DERROTA", new Color32(220, 40, 40, 255));

            HideAll();
        }

        GameObject BuildPanel(string name, bool isVictory, string titleText, Color titleColor)
        {
            GameObject root = new GameObject(name, typeof(RectTransform));
            root.transform.SetParent(transform, false);

            RectTransform rt = root.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;

            GameObject overlay = new GameObject("Overlay", typeof(RectTransform), typeof(Image));
            overlay.transform.SetParent(root.transform, false);
            RectTransform overlayRt = overlay.GetComponent<RectTransform>();
            overlayRt.anchorMin = Vector2.zero;
            overlayRt.anchorMax = Vector2.one;
            overlayRt.sizeDelta = Vector2.zero;
            Image overlayImage = overlay.GetComponent<Image>();
            overlayImage.color = new Color(0, 0, 0, 0.65f);

            GameObject panelBox = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            panelBox.transform.SetParent(root.transform, false);
            RectTransform panelRt = panelBox.GetComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(0.5f, 0.5f);
            panelRt.anchorMax = new Vector2(0.5f, 0.5f);
            panelRt.sizeDelta = new Vector2(500, 350);
            panelRt.anchoredPosition = Vector2.zero;
            Image panelImage = panelBox.GetComponent<Image>();
            panelImage.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);

            GameObject title = new GameObject("Title", typeof(RectTransform));
            title.transform.SetParent(panelBox.transform, false);
            TextMeshProUGUI titleTmp = title.AddComponent<TextMeshProUGUI>();
            titleTmp.text = titleText;
            titleTmp.fontSize = 64;
            titleTmp.fontStyle = FontStyles.Bold;
            titleTmp.alignment = TextAlignmentOptions.Center;
            titleTmp.color = titleColor;
            RectTransform titleRt = title.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0.5f, 0.5f);
            titleRt.anchorMax = new Vector2(0.5f, 0.5f);
            titleRt.sizeDelta = new Vector2(400, 80);
            titleRt.anchoredPosition = new Vector2(0, 60);

            GameObject subtitle = new GameObject("Subtitle", typeof(RectTransform));
            subtitle.transform.SetParent(panelBox.transform, false);
            TextMeshProUGUI subTmp = subtitle.AddComponent<TextMeshProUGUI>();
            subTmp.text = isVictory ? "Has derrotado a todos los enemigos" : "Has caído en combate";
            subTmp.fontSize = 22;
            subTmp.alignment = TextAlignmentOptions.Center;
            subTmp.color = new Color32(180, 180, 180, 255);
            RectTransform subRt = subtitle.GetComponent<RectTransform>();
            subRt.anchorMin = new Vector2(0.5f, 0.5f);
            subRt.anchorMax = new Vector2(0.5f, 0.5f);
            subRt.sizeDelta = new Vector2(400, 40);
            subRt.anchoredPosition = new Vector2(0, 10);

            string btnLabel = isVictory ? "Continuar" : "Reintentar";
            GameObject btn = new GameObject(btnLabel, typeof(RectTransform), typeof(Image), typeof(Button));
            btn.transform.SetParent(panelBox.transform, false);
            RectTransform btnRt = btn.GetComponent<RectTransform>();
            btnRt.anchorMin = new Vector2(0.5f, 0.5f);
            btnRt.anchorMax = new Vector2(0.5f, 0.5f);
            btnRt.sizeDelta = new Vector2(200, 50);
            btnRt.anchoredPosition = new Vector2(0, -70);
            Image btnImage = btn.GetComponent<Image>();
            btnImage.color = isVictory ? new Color32(40, 120, 40, 255) : new Color32(140, 40, 40, 255);
            Button btnComp = btn.GetComponent<Button>();

            GameObject btnText = new GameObject("Text", typeof(RectTransform));
            btnText.transform.SetParent(btn.transform, false);
            TextMeshProUGUI btnTmp = btnText.AddComponent<TextMeshProUGUI>();
            btnTmp.text = btnLabel;
            btnTmp.fontSize = 28;
            btnTmp.fontStyle = FontStyles.Bold;
            btnTmp.alignment = TextAlignmentOptions.Center;
            btnTmp.color = Color.white;
            RectTransform btnTextRt = btnText.GetComponent<RectTransform>();
            btnTextRt.anchorMin = Vector2.zero;
            btnTextRt.anchorMax = Vector2.one;
            btnTextRt.sizeDelta = Vector2.zero;

            if (isVictory)
                btnComp.onClick.AddListener(RestartGame);
            else
                btnComp.onClick.AddListener(RestartGame);

            root.SetActive(false);
            return root;
        }
    }
}
