using UnityEngine;

public class CardUI : MonoBehaviour
{
    public Card card;
    public BattleManager battleManager;

    public void Setup(Card newCard, BattleManager manager)
    {
        card = newCard;
        battleManager = manager;
    }

    public void OnClick()
    {
        battleManager.PlayCard(card);
    }
}