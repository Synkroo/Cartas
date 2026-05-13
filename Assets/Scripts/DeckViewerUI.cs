using System.Collections.Generic;
using UnityEngine;

public class DeckViewerUI : MonoBehaviour
{
    [Header("References")]
    public BattleManager battle;

    public GameObject panel;

    public Transform contentParent;

    public GameObject cardPrefab;

    public void Open()
    {
        panel.SetActive(true);

        ShowRemaining();
    }

    public void Close()
    {
        panel.SetActive(false);
    }

    public void ShowRemaining()
    {
        RenderCards(battle.deckManager.deck);
    }

    public void ShowDiscard()
    {
        RenderCards(battle.deckManager.discard);
    }

    void RenderCards(List<Card> cards)
    {
        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }

        foreach (var card in cards)
        {
            GameObject obj =
                Instantiate(cardPrefab, contentParent);

            CardView view =
                obj.GetComponent<CardView>();

            view.Setup(card, battle);

            view.interactable = false;

            CardHover hover =
                obj.GetComponent<CardHover>();

            if (hover != null)
            {
                hover.enabled = false;
            }
        }
    }
}