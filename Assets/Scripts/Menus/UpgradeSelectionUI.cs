using System;
using System.Collections.Generic;
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
        public List<Button> upgradeButtons = new List<Button>();
        public List<TextMeshProUGUI> upgradeLabels = new List<TextMeshProUGUI>();
        public Button cancelButton;

        [Header("Text")]
        public string cardTitleFormat = "Mejorar: {0}";

        Card currentCard;
        Action onComplete;
        Action onCancel;

        public bool IsConfigured => panel != null && GetButtons().Count > 0;

        void Awake()
        {
            if (panel != null)
                panel.SetActive(false);

            if (cancelButton != null)
            {
                cancelButton.onClick.RemoveAllListeners();
                cancelButton.onClick.AddListener(Cancel);
            }
        }

        public bool Show(Card card, Action onUpgradeComplete, Action onUpgradeCancel = null)
        {
            if (card == null)
                return false;

            currentCard = card;
            onComplete = onUpgradeComplete;
            onCancel = onUpgradeCancel;

            if (!HasRequiredReferences())
                return false;

            if (overlay != null)
                overlay.SetActive(true);

            if (titleText != null && card.data != null)
                titleText.text = string.Format(cardTitleFormat, card.data.cardName);

            List<Button> buttons = GetButtons();
            List<TextMeshProUGUI> labels = GetLabels();
            for (int i = 0; i < buttons.Count; i++)
            {
                int optionIndex = i;
                Button button = buttons[i];
                if (button == null)
                    continue;
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => ApplyUpgrade(optionIndex));
                button.interactable = HasOption(card, optionIndex);
                if (i < labels.Count)
                    SetOptionText(labels[i], card, optionIndex);
            }

            if (panel != null)
                panel.SetActive(true);

            return true;
        }

        List<Button> GetButtons()
        {
            if (upgradeButtons != null && upgradeButtons.Count > 0)
                return upgradeButtons;

            upgradeButtons = new List<Button>();
            if (costUpgradeButton != null)
                upgradeButtons.Add(costUpgradeButton);
            if (reactivationUpgradeButton != null)
                upgradeButtons.Add(reactivationUpgradeButton);
            return upgradeButtons;
        }

        List<TextMeshProUGUI> GetLabels()
        {
            if (upgradeLabels != null && upgradeLabels.Count > 0)
                return upgradeLabels;

            upgradeLabels = new List<TextMeshProUGUI>();
            if (firstUpgradeText != null)
                upgradeLabels.Add(firstUpgradeText);
            if (secondUpgradeText != null)
                upgradeLabels.Add(secondUpgradeText);
            return upgradeLabels;
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
                Close(true);
        }

        public void Cancel()
        {
            Close(false);
        }

        void Close(bool completed)
        {
            if (panel != null)
                panel.SetActive(false);

            if (overlay != null)
                overlay.SetActive(false);

            Action callback = completed ? onComplete : onCancel;
            currentCard = null;
            onComplete = null;
            onCancel = null;
            callback?.Invoke();
        }
    }
}
