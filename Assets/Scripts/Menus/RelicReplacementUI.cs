using System;
using System.Collections.Generic;
using JuegoDeCartas.Relics;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JuegoDeCartas.UI
{
    public class RelicReplacementUI : MonoBehaviour
    {
        public GameObject panel;
        public TextMeshProUGUI titleText;
        public Image incomingIcon;
        public TextMeshProUGUI incomingNameText;
        public TextMeshProUGUI incomingDescriptionText;
        public List<RelicReplacementChoiceUI> choices =
            new List<RelicReplacementChoiceUI>();
        public Button cancelButton;
        public UITransitionAnimator transition;

        [Header("Text")]
        public string title = "Elige una reliquia para sustituir";

        Action<RelicData> selected;
        Action cancelled;

        public bool IsConfigured =>
            panel != null &&
            cancelButton != null &&
            choices != null &&
            choices.Count >= RelicInventory.MaxRelics &&
            choices.TrueForAll(choice => choice != null);

        void Awake()
        {
            if (cancelButton != null)
                cancelButton.onClick.AddListener(Cancel);
        }

        public bool Open(
            RelicData incoming,
            IReadOnlyList<RelicData> ownedRelics,
            Action<RelicData> onSelected,
            Action onCancelled)
        {
            if (!IsConfigured ||
                incoming == null ||
                ownedRelics == null ||
                ownedRelics.Count == 0)
            {
                return false;
            }

            selected = onSelected;
            cancelled = onCancelled;
            if (titleText != null)
                titleText.text = title;
            if (incomingIcon != null)
            {
                incomingIcon.sprite = incoming.icon;
                incomingIcon.enabled = incoming.icon != null;
            }
            if (incomingNameText != null)
                incomingNameText.text = incoming.relicName;
            if (incomingDescriptionText != null)
                incomingDescriptionText.text = incoming.description;

            panel.SetActive(true);
            for (int i = 0; i < choices.Count; i++)
            {
                RelicReplacementChoiceUI choice = choices[i];
                if (choice == null)
                    continue;

                RelicData relic = i < ownedRelics.Count
                    ? ownedRelics[i]
                    : null;
                choice.Setup(relic, Select);
                choice.PlayEntrance(i * 0.05f);
            }

            if (transition != null)
                transition.PlayIn();
            return true;
        }

        public void Close()
        {
            selected = null;
            cancelled = null;
            if (transition != null &&
                panel != null &&
                panel.activeInHierarchy)
            {
                transition.PlayOut(() => panel.SetActive(false));
            }
            else
            {
                CloseImmediate();
            }
        }

        public void CloseImmediate()
        {
            selected = null;
            cancelled = null;
            if (panel != null)
                panel.SetActive(false);
        }

        void Select(RelicData relic)
        {
            selected?.Invoke(relic);
        }

        void Cancel()
        {
            cancelled?.Invoke();
        }
    }
}
