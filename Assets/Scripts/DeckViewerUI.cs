using System.Collections.Generic;
using UnityEngine;
using JuegoDeCartas.Managers;
using JuegoDeCartas.Cards;

namespace JuegoDeCartas.UI
{
    public class DeckViewerUI : MonoBehaviour
    {
        [Header("References")]
        public BattleManager battle;

        public GameObject panel;

        public Transform contentParent;

        public GameObject cardPrefab;

        public System.Action onClose;

        public void Open()
        {
            panel.SetActive(true);

            ShowRemaining();
        }

        public void Close()
        {
            panel.SetActive(false);
            onClose?.Invoke();
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

                CardDisplay display =
                    obj.GetComponent<CardDisplay>();

                if (display != null)
                {
                    display.Setup(card);
                }
                else
                {
                    CardView view = obj.GetComponent<CardView>();
                    if (view != null)
                    {
                        view.Setup(card, battle);
                        view.interactable = false;
                    }
                }

                CardHover hover =
                    obj.GetComponent<CardHover>();

                if (hover != null)
                {
                    hover.enabled = false;
                }
            }
        }
    }
}
