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

            if (nameText != null) nameText.text = card.data.cardName;
            if (descriptionText != null) descriptionText.text = card.data.description;
            if (costText != null) costText.text = card.effectiveCost.ToString();
            if (artworkImage != null)
            {
                artworkImage.sprite = card.data.sprite;
                artworkImage.enabled = card.data.sprite != null;
            }

            var bg = transform.Find("fondo carta")?.GetComponent<Image>();
            if (bg != null)
                bg.color = card.upgraded ? Color.red : new Color(0.3301887f, 0.3301887f, 0.3301887f, 1f);
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
