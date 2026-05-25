using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
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

        void OnEnable()
        {
            var totalesBtn = transform.Find("Panel/TopBar/Totales")?.GetComponent<Button>();
            if (totalesBtn != null)
            {
                totalesBtn.onClick.RemoveAllListeners();
                totalesBtn.onClick.AddListener(ShowTotal);
            }
        }

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

        public void ShowTotal()
        {
            var dm = battle.deckManager;
            var all = new List<Card>(dm.deck.Count + dm.hand.Count + dm.discard.Count);
            all.AddRange(dm.deck);
            all.AddRange(dm.hand);
            all.AddRange(dm.discard);
            RenderCards(all);
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

                CardView view = obj.GetComponent<CardView>();
                if (view != null)
                {
                    view.Setup(card, battle);
                    view.interactable = false;
                }

                CardHover hover =
                    obj.GetComponent<CardHover>();

                if (hover != null)
                {
                    hover.enabled = true;
                    hover.RefreshState();
                }
            }
        }
    }
}
