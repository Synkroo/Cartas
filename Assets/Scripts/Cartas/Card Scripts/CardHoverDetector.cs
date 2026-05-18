using UnityEngine;
using UnityEngine.EventSystems;

namespace JuegoDeCartas.Cards
{
    public class CardHoverDetector : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private CardHover hover;

        void Awake()
        {
            hover = GetComponent<CardHover>();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            hover.SetHover(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            hover.SetHover(false);
        }
    }
}
