using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using JuegoDeCartas.Cards;

namespace JuegoDeCartas.UI
{
    public class UpgradeSelectionUI : MonoBehaviour
    {
        [Header("UI References")]
        public GameObject panel;
        public TextMeshProUGUI titleText;
        public Button costUpgradeButton;
        public Button reactivationUpgradeButton;

        private Card currentCard;
        private Action onComplete;
        private GameObject overlay;

        void Awake()
        {
            if (panel != null)
                panel.SetActive(false);

            CreateUI();
        }

        void CreateUI()
        {
            if (panel != null) return;

            var parent = transform.parent ?? transform;

            overlay = new GameObject("UpgradeOverlay", typeof(RectTransform), typeof(Image));
            overlay.transform.SetParent(parent, false);
            var olRT = overlay.GetComponent<RectTransform>();
            olRT.anchorMin = Vector2.zero;
            olRT.anchorMax = Vector2.one;
            olRT.sizeDelta = Vector2.zero;
            olRT.localScale = Vector3.one;
            var olImg = overlay.GetComponent<Image>();
            olImg.color = new Color(0, 0, 0, 0.6f);
            olImg.raycastTarget = true;
            overlay.SetActive(false);

            panel = new GameObject("UpgradePanel", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            panel.transform.SetParent(parent, false);
            panel.transform.SetAsLastSibling();

            var rt = panel.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(4, 3);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.localScale = Vector3.one;

            var img = panel.GetComponent<Image>();
            img.color = new Color(0.15f, 0.15f, 0.15f, 1f);

            var cg = panel.GetComponent<CanvasGroup>();
            cg.blocksRaycasts = true;

            var titleGO = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleGO.transform.SetParent(panel.transform, false);
            var titleRT = titleGO.GetComponent<RectTransform>();
            titleRT.anchorMin = new Vector2(0, 1);
            titleRT.anchorMax = new Vector2(1, 1);
            titleRT.pivot = new Vector2(0.5f, 1);
            titleRT.sizeDelta = new Vector2(0, 0.6f);
            titleRT.anchoredPosition = new Vector2(0, -0.2f);
            titleText = titleGO.GetComponent<TextMeshProUGUI>();
            titleText.fontSize = 0.4f;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.text = "Mejorar Carta";

            var costBtn = new GameObject("CostButton", typeof(RectTransform), typeof(Image), typeof(Button));
            costBtn.transform.SetParent(panel.transform, false);
            var costBtnRT = costBtn.GetComponent<RectTransform>();
            costBtnRT.anchorMin = new Vector2(0.5f, 0.5f);
            costBtnRT.anchorMax = new Vector2(0.5f, 0.5f);
            costBtnRT.sizeDelta = new Vector2(2.5f, 0.6f);
            costBtnRT.anchoredPosition = new Vector2(0, 0.5f);
            var costBtnImg = costBtn.GetComponent<Image>();
            costBtnImg.color = new Color(0.2f, 0.4f, 0.8f, 1f);
            costUpgradeButton = costBtn.GetComponent<Button>();

            var costBtnText = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            costBtnText.transform.SetParent(costBtn.transform, false);
            var costBtnTextRT = costBtnText.GetComponent<RectTransform>();
            costBtnTextRT.anchorMin = Vector2.zero;
            costBtnTextRT.anchorMax = Vector2.one;
            costBtnTextRT.sizeDelta = Vector2.zero;
            var costTMP = costBtnText.GetComponent<TextMeshProUGUI>();
            costTMP.fontSize = 0.3f;
            costTMP.alignment = TextAlignmentOptions.Center;
            costTMP.text = "-1 Coste de Maná";

            var reactBtn = new GameObject("ReactivationButton", typeof(RectTransform), typeof(Image), typeof(Button));
            reactBtn.transform.SetParent(panel.transform, false);
            var reactBtnRT = reactBtn.GetComponent<RectTransform>();
            reactBtnRT.anchorMin = new Vector2(0.5f, 0.5f);
            reactBtnRT.anchorMax = new Vector2(0.5f, 0.5f);
            reactBtnRT.sizeDelta = new Vector2(2.5f, 0.6f);
            reactBtnRT.anchoredPosition = new Vector2(0, -0.5f);
            var reactBtnImg = reactBtn.GetComponent<Image>();
            reactBtnImg.color = new Color(0.8f, 0.4f, 0.2f, 1f);
            reactivationUpgradeButton = reactBtn.GetComponent<Button>();

            var reactBtnText = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            reactBtnText.transform.SetParent(reactBtn.transform, false);
            var reactBtnTextRT = reactBtnText.GetComponent<RectTransform>();
            reactBtnTextRT.anchorMin = Vector2.zero;
            reactBtnTextRT.anchorMax = Vector2.one;
            reactBtnTextRT.sizeDelta = Vector2.zero;
            var reactTMP = reactBtnText.GetComponent<TextMeshProUGUI>();
            reactTMP.fontSize = 0.3f;
            reactTMP.alignment = TextAlignmentOptions.Center;
            reactTMP.text = "+1 Reactivación";

            panel.SetActive(false);
        }

        public void Show(Card card, Action onUpgradeComplete)
        {
            currentCard = card;
            onComplete = onUpgradeComplete;

            CreateUI();

            if (overlay != null)
                overlay.SetActive(true);

            if (titleText != null)
                titleText.text = "Mejorar: " + card.data.cardName;

            if (costUpgradeButton != null)
            {
                costUpgradeButton.onClick.RemoveAllListeners();
                costUpgradeButton.onClick.AddListener(ApplyCostUpgrade);
            }

            if (reactivationUpgradeButton != null)
            {
                reactivationUpgradeButton.onClick.RemoveAllListeners();
                reactivationUpgradeButton.onClick.AddListener(ApplyReactivationUpgrade);
            }

            if (panel != null)
                panel.SetActive(true);
        }

        void ApplyCostUpgrade()
        {
            if (currentCard == null) return;
            currentCard.costReduction++;
            currentCard.upgraded = true;
            Close();
        }

        void ApplyReactivationUpgrade()
        {
            if (currentCard == null) return;
            currentCard.reactivationCount++;
            currentCard.upgraded = true;
            Close();
        }

        void Close()
        {
            if (panel != null)
                panel.SetActive(false);
            if (overlay != null)
                overlay.SetActive(false);
            onComplete?.Invoke();
        }
    }
}
