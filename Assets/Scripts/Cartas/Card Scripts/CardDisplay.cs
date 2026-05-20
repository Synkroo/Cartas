using TMPro;
using UnityEngine;
using JuegoDeCartas.Cards;

namespace JuegoDeCartas.UI
{
    public class CardDisplay : MonoBehaviour
    {
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI costText;

        public void Setup(Card card)
        {
            nameText.text = card.data.cardName;
            costText.text = card.data.cost.ToString();
        }
    }
}
