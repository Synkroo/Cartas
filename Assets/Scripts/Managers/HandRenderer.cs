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
            foreach (Transform child in handParent)
                Object.Destroy(child.gameObject);

            foreach (var card in hand)
            {
                GameObject obj = Object.Instantiate(cardPrefab, handParent);
                obj.transform.localScale = new Vector3(0.8f, obj.transform.localScale.y, obj.transform.localScale.z);
                obj.GetComponent<CardView>().Setup(card, battle);
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
