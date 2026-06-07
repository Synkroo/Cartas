using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace JuegoDeCartas.UI
{
    public class CombatStatusIconUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public GameObject root;
        public Image iconImage;
        public TextMeshProUGUI valueText;
        public CombatTooltipUI tooltip;

        string title;
        string description;
        Color iconBaseColor = Color.white;
        bool cachedIconBaseColor;

        public void SetStatus(bool visible, Sprite sprite, string value, string newTitle, string newDescription)
        {
            if (root != null)
                root.SetActive(visible);
            else
                gameObject.SetActive(visible);

            if (iconImage != null)
            {
                CacheBaseIconColor();
                iconImage.sprite = sprite;
                iconImage.enabled = visible;
                iconImage.color = HasAssignedSprite(iconImage)
                    ? iconBaseColor
                    : new Color(iconBaseColor.r, iconBaseColor.g, iconBaseColor.b, 0f);
            }

            if (valueText != null)
                valueText.text = value;

            title = newTitle;
            description = newDescription;
        }

        public void SetStatus(bool visible, string value, string newTitle, string newDescription)
        {
            SetStatus(visible, iconImage != null ? iconImage.sprite : null, value, newTitle, newDescription);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (tooltip == null || string.IsNullOrEmpty(title))
                return;

            tooltip.Show(this, title, description);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (tooltip == null)
                return;

            tooltip.Hide(this);
        }

        void OnDisable()
        {
            if (tooltip != null)
                tooltip.Hide(this);
        }

        static bool HasAssignedSprite(Image image)
        {
            return image != null && (image.sprite != null || image.overrideSprite != null);
        }

        void CacheBaseIconColor()
        {
            if (cachedIconBaseColor || iconImage == null)
                return;

            iconBaseColor = iconImage.color;
            cachedIconBaseColor = true;
        }
    }
}
