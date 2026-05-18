using TMPro;
using UnityEngine;
using UnityEngine.UI;
using JuegoDeCartas.Managers;

namespace JuegoDeCartas.Cards
{
    public class CardView : MonoBehaviour
    {
        public TextMeshProUGUI nameText;
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

            nameText.text = card.data.cardName;
            costText.text = card.data.cost.ToString();
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
