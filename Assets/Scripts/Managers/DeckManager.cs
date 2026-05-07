using System.Collections.Generic;
using UnityEngine;

public class DeckManager : MonoBehaviour
{
    public List<CardData> startingDeck;

    public List<Card> deck = new List<Card>();
    public List<Card> hand = new List<Card>();
    public List<Card> discard = new List<Card>();

    public void InitializeDeck()
    {
        deck.Clear();

        foreach (var cardData in startingDeck)
        {
            deck.Add(new Card(cardData));
        }

        Shuffle(deck);
    }

    public void DrawCards(int amount)
    {
        for (int i = 0; i < amount; i++)
        {
            if (deck.Count == 0)
            {
                deck.AddRange(discard);
                discard.Clear();
                Shuffle(deck);
            }

            if (deck.Count == 0) return;

            hand.Add(deck[0]);
            deck.RemoveAt(0);
        }
    }

    public void Shuffle(List<Card> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            Card temp = list[i];
            int randomIndex = Random.Range(i, list.Count);
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }
}