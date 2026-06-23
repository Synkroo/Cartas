using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using JuegoDeCartas.Cards;
using JuegoDeCartas.Progression;

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
        public string epiphanyTitleFormat = "Epifania: {0}";

        [Header("Single Epiphany Layout")]
        public Vector2 singleOptionAnchorMin = new Vector2(0.25f, 0.28f);
        public Vector2 singleOptionAnchorMax = new Vector2(0.75f, 0.72f);

        Card currentCard;
        Action onComplete;
        Action onCancel;
        bool selectingEpiphany;
        readonly Dictionary<Button, ButtonLayout> originalLayouts =
            new Dictionary<Button, ButtonLayout>();

        public bool IsConfigured => panel != null && GetButtons().Count > 0;

        void Awake()
        {
            if (panel != null)
                panel.SetActive(false);

            ConfigureCancelButton(false);
        }

        public bool Show(Card card, Action onUpgradeComplete, Action onUpgradeCancel = null)
        {
            return ShowOptions(card, false, onUpgradeComplete, onUpgradeCancel);
        }

        public bool ShowEpiphanies(
            Card card,
            Action onEpiphanyComplete,
            Action onEpiphanyCancel = null)
        {
            return ShowOptions(
                card,
                true,
                onEpiphanyComplete,
                onEpiphanyCancel
            );
        }

        bool ShowOptions(
            Card card,
            bool epiphanyMode,
            Action onSelectionComplete,
            Action onSelectionCancel)
        {
            if (card == null || !HasRequiredReferences())
                return false;

            currentCard = card;
            onComplete = onSelectionComplete;
            onCancel = onSelectionCancel;
            selectingEpiphany = epiphanyMode;
            ConfigureCancelButton(onSelectionCancel != null);

            if (overlay != null)
                overlay.SetActive(true);

            if (titleText != null && card.data != null)
            {
                titleText.text = string.Format(
                    selectingEpiphany
                        ? epiphanyTitleFormat
                        : cardTitleFormat,
                    card.data.cardName
                );
            }

            List<Button> buttons = GetButtons();
            List<TextMeshProUGUI> labels = GetLabels();
            CacheAndRestoreButtonLayouts(buttons);
            int availableOptionCount = CountAvailableOptions(
                card,
                buttons.Count,
                selectingEpiphany
            );
            for (int i = 0; i < buttons.Count; i++)
            {
                int optionIndex = i;
                Button button = buttons[i];
                if (button == null)
                    continue;
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => ApplyOption(optionIndex));
                bool hasOption = selectingEpiphany
                    ? HasEpiphany(card, optionIndex)
                    : HasUpgrade(card, optionIndex);
                button.gameObject.SetActive(!selectingEpiphany || hasOption);
                button.interactable = hasOption;
                if (i < labels.Count)
                    SetOptionText(labels[i], card, optionIndex, selectingEpiphany);

                if (selectingEpiphany && hasOption)
                    CollectionProgress.MarkEpiphanySeen(card.data, optionIndex);
            }

            if (selectingEpiphany && availableOptionCount == 1)
                CenterSingleAvailableOption(card, buttons);

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

        static bool HasUpgrade(Card card, int index)
        {
            return card?.data != null &&
                   card.selectedUpgradeIndex < 0 &&
                   index >= 0 &&
                   index < card.data.upgradeOptions.Count &&
                   card.data.upgradeOptions[index] != null;
        }

        static bool HasEpiphany(Card card, int index)
        {
            if (card?.data == null || card.epiphanyUnlocked)
                return false;

            List<CardEpiphany> options = card.data.GetEpiphanyOptions();
            return index >= 0 &&
                   index < options.Count &&
                   options[index] != null &&
                   !string.IsNullOrWhiteSpace(options[index].epiphanyName);
        }

        static void SetOptionText(
            TextMeshProUGUI label,
            Card card,
            int index,
            bool epiphanyMode)
        {
            if (label == null)
                return;

            bool hasOption = epiphanyMode
                ? HasEpiphany(card, index)
                : HasUpgrade(card, index);
            if (!hasOption)
            {
                label.text = "No disponible";
                return;
            }

            if (epiphanyMode)
            {
                CardEpiphany option = card.data.GetEpiphanyOptions()[index];
                string finalDescription =
                    CardDescriptionBuilder.BuildFinalDescription(card, option);
                label.text = string.IsNullOrWhiteSpace(finalDescription)
                    ? option.epiphanyName
                    : option.epiphanyName + "\n" + finalDescription;
            }
            else
            {
                CardUpgradeOption option = card.data.upgradeOptions[index];
                label.text = string.IsNullOrWhiteSpace(option.description)
                    ? option.upgradeName
                    : option.upgradeName + "\n" + option.description;
            }
        }

        void ApplyOption(int optionIndex)
        {
            if (currentCard == null)
                return;

            bool applied = selectingEpiphany
                ? currentCard.ApplyEpiphany(optionIndex)
                : currentCard.ApplyUpgrade(optionIndex);
            if (applied)
                Close(true);
        }

        public void Cancel()
        {
            Close(false);
        }

        void Close(bool completed)
        {
            CacheAndRestoreButtonLayouts(GetButtons());

            if (panel != null)
                panel.SetActive(false);

            if (overlay != null)
                overlay.SetActive(false);

            Action callback = completed ? onComplete : onCancel;
            currentCard = null;
            onComplete = null;
            onCancel = null;
            selectingEpiphany = false;
            callback?.Invoke();
        }

        void ConfigureCancelButton(bool visible)
        {
            if (cancelButton == null)
                return;

            cancelButton.onClick.RemoveListener(Cancel);
            cancelButton.onClick.AddListener(Cancel);
            cancelButton.interactable = visible;
            cancelButton.gameObject.SetActive(visible);
        }

        void CacheAndRestoreButtonLayouts(List<Button> buttons)
        {
            foreach (Button button in buttons)
            {
                if (button == null ||
                    !(button.transform is RectTransform rectTransform))
                {
                    continue;
                }

                if (!originalLayouts.TryGetValue(button, out ButtonLayout layout))
                {
                    layout = new ButtonLayout(rectTransform);
                    originalLayouts.Add(button, layout);
                }

                layout.Apply(rectTransform);
            }
        }

        static int CountAvailableOptions(
            Card card,
            int buttonCount,
            bool epiphanyMode)
        {
            int count = 0;
            for (int i = 0; i < buttonCount; i++)
            {
                if (epiphanyMode
                    ? HasEpiphany(card, i)
                    : HasUpgrade(card, i))
                {
                    count++;
                }
            }

            return count;
        }

        void CenterSingleAvailableOption(Card card, List<Button> buttons)
        {
            for (int i = 0; i < buttons.Count; i++)
            {
                if (!HasEpiphany(card, i) ||
                    buttons[i] == null ||
                    !(buttons[i].transform is RectTransform rectTransform))
                {
                    continue;
                }

                rectTransform.anchorMin = singleOptionAnchorMin;
                rectTransform.anchorMax = singleOptionAnchorMax;
                rectTransform.anchoredPosition = Vector2.zero;
                rectTransform.sizeDelta = Vector2.zero;
                return;
            }
        }

        readonly struct ButtonLayout
        {
            readonly Vector2 anchorMin;
            readonly Vector2 anchorMax;
            readonly Vector2 anchoredPosition;
            readonly Vector2 sizeDelta;

            public ButtonLayout(RectTransform rectTransform)
            {
                anchorMin = rectTransform.anchorMin;
                anchorMax = rectTransform.anchorMax;
                anchoredPosition = rectTransform.anchoredPosition;
                sizeDelta = rectTransform.sizeDelta;
            }

            public void Apply(RectTransform rectTransform)
            {
                rectTransform.anchorMin = anchorMin;
                rectTransform.anchorMax = anchorMax;
                rectTransform.anchoredPosition = anchoredPosition;
                rectTransform.sizeDelta = sizeDelta;
            }
        }
    }
}
