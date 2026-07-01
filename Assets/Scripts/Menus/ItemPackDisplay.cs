using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using JuegoDeCartas.Articulos;
using JuegoDeCartas.Missions;
using System.Collections;

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

        [Header("Reservation")]
        public Button reserveButton;
        public GameObject reservedMarker;
        public TextMeshProUGUI reserveText;
        public string reserveLabel = "RESERVAR";
        public string reservedLabel = "FIJADO";

        [Header("Rarity Colors")]
        public Color commonColor = new Color(0.15f, 0.45f, 0.75f, 1f);
        public Color rareColor = new Color(0.15f, 0.65f, 0.25f, 1f);
        public Color epicColor = new Color(0.6f, 0.2f, 0.7f, 1f);
        public string currencySuffix = "\u20ac";
        public string choicesFormat = "Elige 1 de {0}";
        public UITransitionAnimator transition;
        [Min(0.01f)] public float openDuration = 0.18f;

        ItemPackOffer offer;
        Action<ItemPackOffer> onSelected;
        Action<ItemPackOffer> onReservationChanged;

        void Awake()
        {
            if (button == null)
                button = GetComponent<Button>();
            if (button != null)
                button.onClick.AddListener(Select);
            if (reserveButton != null)
                reserveButton.onClick.AddListener(ToggleReservation);
        }

        public void Setup(
            ItemPackOffer newOffer,
            Action<ItemPackOffer> selected,
            Action<ItemPackOffer> reservationChanged = null)
        {
            offer = newOffer;
            onSelected = selected;
            onReservationChanged = reservationChanged;

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
            RefreshReservation();
        }

        public void SetClaimed(bool claimed)
        {
            if (button != null)
                button.interactable = !claimed;
            if (reserveButton != null)
                reserveButton.interactable = !claimed;
            gameObject.SetActive(!claimed);
        }

        public void RefreshReservation()
        {
            bool reserved = offer != null && offer.Reserved && !offer.Claimed;
            if (reservedMarker != null)
                reservedMarker.SetActive(reserved);
            if (reserveText != null)
                reserveText.text = reserved ? reservedLabel : reserveLabel;
        }

        public void PrepareForRefresh()
        {
            if (transition != null)
                transition.PrepareForEntrance();
        }

        public void PlayEntrance(float delay)
        {
            if (transition != null)
                transition.PlayIn(delay);
        }

        void Select()
        {
            if (offer != null && !offer.Claimed)
                StartCoroutine(OpenRoutine());
        }

        void ToggleReservation()
        {
            if (offer == null || offer.Claimed)
                return;

            onReservationChanged?.Invoke(offer);
            RefreshReservation();
        }

        IEnumerator OpenRoutine()
        {
            if (button != null)
                button.interactable = false;

            float elapsed = 0f;
            Vector3 baseScale = transform.localScale;
            while (elapsed < openDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / openDuration);
                float scale = 1f + Mathf.Sin(progress * Mathf.PI) * 0.12f;
                transform.localScale = baseScale * scale;
                yield return null;
            }

            transform.localScale = baseScale;
            onSelected?.Invoke(offer);
            if (button != null && offer != null && !offer.Claimed)
                button.interactable = true;
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
