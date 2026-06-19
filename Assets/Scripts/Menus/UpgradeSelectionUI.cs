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
        public TextMeshProUGUI firstUpgradeText;
        public TextMeshProUGUI secondUpgradeText;

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
                costUpgradeButton.onClick.AddListener(() => ApplyUpgrade(0));
                costUpgradeButton.interactable = HasOption(card, 0);
            }

            if (reactivationUpgradeButton != null)
            {
                reactivationUpgradeButton.onClick.RemoveAllListeners();
                reactivationUpgradeButton.onClick.AddListener(() => ApplyUpgrade(1));
                reactivationUpgradeButton.interactable = HasOption(card, 1);
            }

            SetOptionText(firstUpgradeText, card, 0);
            SetOptionText(secondUpgradeText, card, 1);

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

        static bool HasOption(Card card, int index)
        {
            return card?.data != null &&
                   card.selectedUpgradeIndex < 0 &&
                   index >= 0 &&
                   index < card.data.upgradeOptions.Count &&
                   card.data.upgradeOptions[index] != null;
        }

        static void SetOptionText(TextMeshProUGUI label, Card card, int index)
        {
            if (label == null)
                return;

            if (!HasOption(card, index))
            {
                label.text = "No disponible";
                return;
            }

            CardUpgradeOption option = card.data.upgradeOptions[index];
            label.text = string.IsNullOrWhiteSpace(option.description)
                ? option.upgradeName
                : option.upgradeName + "\n" + option.description;
        }

        void ApplyUpgrade(int optionIndex)
        {
            if (currentCard == null)
                return;

            if (currentCard.ApplyUpgrade(optionIndex))
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
