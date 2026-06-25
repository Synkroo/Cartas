using System.Collections.Generic;
using UnityEngine;
using JuegoDeCartas.Cards;
using JuegoDeCartas.UI;

namespace JuegoDeCartas.Managers
{
    [System.Serializable]
    public class HandRenderer
    {
        public Transform handParent;
        public GameObject cardPrefab;

        public void Render(List<Card> hand, BattleManager battle)
        {
            if (handParent == null || cardPrefab == null || hand == null)
                return;

            for (int i = handParent.childCount - 1; i >= 0; i--)
            {
                Transform child = handParent.GetChild(i);
                child.SetParent(null, false);
                if (Application.isPlaying)
                    Object.Destroy(child.gameObject);
                else
                    Object.DestroyImmediate(child.gameObject);
            }

            foreach (var card in hand)
            {
                if (card == null || card.data == null)
                    continue;

                GameObject obj = Object.Instantiate(cardPrefab, handParent);
                obj.transform.localScale = new Vector3(0.8f, obj.transform.localScale.y, obj.transform.localScale.z);
                CardView view = obj.GetComponent<CardView>();
                if (view != null)
                    view.Setup(card, battle);

                var hover = obj.GetComponent<CardHover>();
                if (hover != null)
                {
                    hover.originalScale = obj.transform.localScale;
                    hover.RefreshState();
                }
            }

            HandLayout layout = handParent.GetComponentInParent<HandLayout>();
            if (layout != null)
                layout.MarkDirty();
        }
    }
}
