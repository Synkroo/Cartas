using System;
using JuegoDeCartas.Relics;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JuegoDeCartas.UI
{
    public class RelicReplacementChoiceUI : MonoBehaviour
    {
        public Button button;
        public Image iconImage;
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI descriptionText;
        public UITransitionAnimator transition;

        RelicData relic;
        Action<RelicData> selected;

        void Awake()
        {
            if (button == null)
                button = GetComponent<Button>();
            if (button != null)
                button.onClick.AddListener(Select);
        }

        public void Setup(
            RelicData newRelic,
            Action<RelicData> onSelected)
        {
            relic = newRelic;
            selected = onSelected;
            bool hasRelic = relic != null;

            if (iconImage != null)
            {
                iconImage.sprite = hasRelic ? relic.icon : null;
                iconImage.enabled = hasRelic && relic.icon != null;
            }
            if (nameText != null)
                nameText.text = hasRelic ? relic.relicName : "";
            if (descriptionText != null)
                descriptionText.text = hasRelic ? relic.description : "";
            if (button != null)
                button.interactable = hasRelic;

            gameObject.SetActive(hasRelic);
        }

        public void PlayEntrance(float delay)
        {
            if (transition == null || !gameObject.activeSelf)
                return;

            transition.CaptureCurrentAsVisible();
            transition.PlayIn(delay);
        }

        void Select()
        {
            if (relic != null)
                selected?.Invoke(relic);
        }
    }
}
