using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JuegoDeCartas.UI
{
    public class CollectionEntryUI : MonoBehaviour
    {
        public Button button;
        public Image image;
        public Image background;
        public TextMeshProUGUI nameText;
        public GameObject lockedOverlay;
        public Color discoveredColor = new Color(0.13f, 0.17f, 0.23f, 1f);
        public Color undiscoveredColor = new Color(0.16f, 0.18f, 0.22f, 1f);

        Action onSelected;

        void Awake()
        {
            if (button == null)
                button = GetComponent<Button>();
            if (button != null)
                button.onClick.AddListener(Select);
        }

        public void Setup(string displayName, Sprite sprite, bool discovered, Action selected)
        {
            onSelected = selected;

            if (nameText != null)
                nameText.text = discovered ? displayName : "???";

            if (image != null)
            {
                image.sprite = sprite;
                image.enabled = sprite != null;
                image.color = discovered ? Color.white : Color.black;
            }

            if (background != null)
                background.color = discovered ? discoveredColor : undiscoveredColor;
            if (lockedOverlay != null)
            {
                lockedOverlay.SetActive(!discovered);
                Graphic graphic = lockedOverlay.GetComponent<Graphic>();
                if (graphic != null)
                    graphic.raycastTarget = false;
            }
        }

        void Select()
        {
            onSelected?.Invoke();
        }
    }
}
