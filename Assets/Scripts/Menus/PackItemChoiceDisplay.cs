using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using JuegoDeCartas.Articulos;

namespace JuegoDeCartas.UI
{
    public class PackItemChoiceDisplay : MonoBehaviour
    {
        public Button button;
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI rarityText;
        public TextMeshProUGUI descriptionText;
        public Image image;
        public Image background;
        public Color commonColor = new Color(0.15f, 0.45f, 0.75f, 1f);
        public Color rareColor = new Color(0.15f, 0.65f, 0.25f, 1f);
        public Color epicColor = new Color(0.6f, 0.2f, 0.7f, 1f);
        public UITransitionAnimator transition;

        ArticuloData item;
        Action<ArticuloData> onSelected;

        void Awake()
        {
            if (button == null)
                button = GetComponent<Button>();
            if (button != null)
                button.onClick.AddListener(Select);
        }

        public void Setup(
            ArticuloData newItem,
            bool canSelect,
            Action<ArticuloData> selected,
            float entranceDelay = 0f)
        {
            item = newItem;
            onSelected = selected;
            if (item == null)
                return;

            if (nameText != null) nameText.text = item.nombre;
            if (descriptionText != null) descriptionText.text = item.descripcion;
            if (rarityText != null) rarityText.text = GetRarityLabel(item.rareza);
            if (image != null)
            {
                image.sprite = item.imagen;
                image.enabled = item.imagen != null;
            }
            if (background != null)
                background.color = GetRarityColor(item.rareza);
            if (button != null)
                button.interactable = canSelect;
        }

        public void PlayEntrance(float entranceDelay = 0f)
        {
            if (transition != null)
            {
                transition.CaptureCurrentAsVisible();
                transition.PlayIn(entranceDelay);
            }
        }

        void Select()
        {
            if (item != null)
                onSelected?.Invoke(item);
        }

        Color GetRarityColor(Rareza rarity)
        {
            return rarity switch
            {
                Rareza.Raro => rareColor,
                Rareza.Epico => epicColor,
                _ => commonColor
            };
        }

        static string GetRarityLabel(Rareza rarity)
        {
            return rarity switch
            {
                Rareza.Raro => "Raro",
                Rareza.Epico => "Epico",
                _ => "Comun"
            };
        }
    }
}
