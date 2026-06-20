using TMPro;
using UnityEngine;
using UnityEngine.UI;
using JuegoDeCartas.Managers;

namespace JuegoDeCartas.Cards
{
    public class CardView : MonoBehaviour
    {
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI descriptionText;
        public TextMeshProUGUI costText;
        public Image artworkImage;
        public Image backgroundImage;

        [Header("State Colors")]
        public Color normalColor = new Color(0.3301887f, 0.3301887f, 0.3301887f, 1f);
        public Color upgradedColor = Color.red;

        public bool interactable = true;

        private Card card;
        private BattleManager battleManager;
        private Button button;

        void Awake()
        {
            button = GetComponent<Button>();

            if (button != null)
            {
                button.onClick.AddListener(OnClick);
            }
        }

        public void Setup(Card newCard, BattleManager manager)
        {
            card = newCard;
            battleManager = manager;

            if (card == null || card.data == null)
            {
                if (nameText != null) nameText.text = "";
                if (descriptionText != null) descriptionText.text = "";
                if (costText != null) costText.text = "";
                if (artworkImage != null) artworkImage.enabled = false;
                return;
            }

            if (nameText != null) nameText.text = card.data.cardName;
            if (descriptionText != null)
            {
                CardUpgradeOption upgrade = card.SelectedUpgrade;
                string baseDescription = battleManager != null
                    ? battleManager.GetRuntimeCardDescription(card.data)
                    : card.data.description;
                descriptionText.text = upgrade != null && !string.IsNullOrWhiteSpace(upgrade.description)
                    ? baseDescription + "\n" + upgrade.description
                    : baseDescription;
            }
            if (costText != null) costText.text = card.effectiveCost.ToString();
            if (artworkImage != null)
            {
                artworkImage.sprite = card.data.sprite;
                artworkImage.enabled = card.data.sprite != null;
            }

            if (backgroundImage == null)
                backgroundImage = transform.Find("fondo carta")?.GetComponent<Image>();

            if (backgroundImage != null)
                backgroundImage.color = card.upgraded ? upgradedColor : normalColor;
        }

        public void OnClick()
        {
            if (!interactable)
                return;

            if (battleManager == null || card == null)
                return;

            battleManager.PlayCard(card);
        }
    }
}
