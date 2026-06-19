using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using JuegoDeCartas.Articulos;
using JuegoDeCartas.Missions;

namespace JuegoDeCartas.UI
{
    public class ItemPackDisplay : MonoBehaviour
    {
        public Button button;
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI descriptionText;
        public TextMeshProUGUI priceText;
        public TextMeshProUGUI choiceCountText;
        public Image image;
        public Image background;

        [Header("Rarity Colors")]
        public Color commonColor = new Color(0.15f, 0.45f, 0.75f, 1f);
        public Color rareColor = new Color(0.15f, 0.65f, 0.25f, 1f);
        public Color epicColor = new Color(0.6f, 0.2f, 0.7f, 1f);
        public string currencySuffix = "\u20ac";
        public string choicesFormat = "Elige 1 de {0}";

        ItemPackOffer offer;
        Action<ItemPackOffer> onSelected;

        void Awake()
        {
            if (button == null)
                button = GetComponent<Button>();
            if (button != null)
                button.onClick.AddListener(Select);
        }

        public void Setup(ItemPackOffer newOffer, Action<ItemPackOffer> selected)
        {
            offer = newOffer;
            onSelected = selected;

            ItemPackData definition = offer?.Definition;
            if (definition == null)
                return;

            if (nameText != null) nameText.text = definition.packName;
            if (descriptionText != null) descriptionText.text = definition.description;
            if (priceText != null)
            {
                int displayPrice = Mathf.RoundToInt(definition.price * MissionRunState.ShopCostMultiplier);
                priceText.text = displayPrice + currencySuffix;
            }
            if (choiceCountText != null) choiceCountText.text = string.Format(choicesFormat, offer.Contents.Count);
            if (image != null)
            {
                image.sprite = definition.image;
                image.enabled = definition.image != null;
            }
            if (background != null)
                background.color = GetRarityColor(definition.displayRarity);
            SetClaimed(offer.Claimed);
        }

        public void SetClaimed(bool claimed)
        {
            if (button != null)
                button.interactable = !claimed;
            gameObject.SetActive(!claimed);
        }

        void Select()
        {
            if (offer != null && !offer.Claimed)
                onSelected?.Invoke(offer);
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
    }
}
