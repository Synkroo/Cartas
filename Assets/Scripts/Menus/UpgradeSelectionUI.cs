using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using JuegoDeCartas.Cards;

namespace JuegoDeCartas.UI
{
    public class UpgradeSelectionUI : MonoBehaviour
    {
        [Header("UI References")]
        public GameObject panel;
        public GameObject overlay;
        public TextMeshProUGUI titleText;
        public Button costUpgradeButton;
        public Button reactivationUpgradeButton;

        [Header("Text")]
        public string cardTitleFormat = "Mejorar: {0}";

        Card currentCard;
        Action onComplete;

        public bool IsConfigured => panel != null && costUpgradeButton != null && reactivationUpgradeButton != null;

        void Awake()
        {
            if (panel != null)
                panel.SetActive(false);

        }

        public bool Show(Card card, Action onUpgradeComplete)
        {
            if (card == null)
                return false;

            currentCard = card;
            onComplete = onUpgradeComplete;

            if (!HasRequiredReferences())
                return false;

            if (overlay != null)
                overlay.SetActive(true);

            if (titleText != null && card.data != null)
                titleText.text = string.Format(cardTitleFormat, card.data.cardName);

            if (costUpgradeButton != null)
            {
                costUpgradeButton.onClick.RemoveAllListeners();
                costUpgradeButton.onClick.AddListener(ApplyCostUpgrade);
                costUpgradeButton.interactable = card.effectiveCost > 0;
            }

            if (reactivationUpgradeButton != null)
            {
                reactivationUpgradeButton.onClick.RemoveAllListeners();
                reactivationUpgradeButton.onClick.AddListener(ApplyReactivationUpgrade);
            }

            if (panel != null)
                panel.SetActive(true);

            return true;
        }

        bool HasRequiredReferences()
        {
            if (IsConfigured)
                return true;

            Debug.LogError("[UpgradeSelectionUI] Missing Inspector references.");
            return false;
        }

        void ApplyCostUpgrade()
        {
            if (currentCard == null)
                return;

            currentCard.ReduceCost();
            Close();
        }

        void ApplyReactivationUpgrade()
        {
            if (currentCard == null)
                return;

            currentCard.AddReactivation();
            Close();
        }

        void Close()
        {
            if (panel != null)
                panel.SetActive(false);

            if (overlay != null)
                overlay.SetActive(false);

            Action completed = onComplete;
            currentCard = null;
            onComplete = null;
            completed?.Invoke();
        }
    }
}
