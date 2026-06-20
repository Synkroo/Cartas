using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using JuegoDeCartas.Characters;

namespace JuegoDeCartas.UI
{
    public class SubclassChoiceDisplay : MonoBehaviour
    {
        public Button button;
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI descriptionText;
        public TextMeshProUGUI passiveText;
        public Image iconImage;

        public void Setup(SubclassData subclass, Action<SubclassData> selected)
        {
            if (subclass == null)
                return;

            if (nameText != null)
                nameText.text = subclass.subclassName;
            if (descriptionText != null)
                descriptionText.text = subclass.description;
            if (passiveText != null)
                passiveText.text = subclass.passiveDescription;
            if (iconImage != null)
            {
                iconImage.sprite = subclass.icon;
                iconImage.enabled = subclass.icon != null;
            }

            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => selected?.Invoke(subclass));
            }
        }
    }
}
