using TMPro;
using UnityEngine;

namespace JuegoDeCartas.UI
{
    public class CombatTooltipUI : MonoBehaviour
    {
        public GameObject root;
        public TextMeshProUGUI titleText;
        public TextMeshProUGUI descriptionText;

        object currentOwner;

        public void Show(string title, string description)
        {
            Show(null, title, description);
        }

        public void Show(object owner, string title, string description)
        {
            if (titleText != null)
                titleText.text = title;

            if (descriptionText != null)
                descriptionText.text = description;

            if (root != null)
                root.SetActive(true);
            else
                gameObject.SetActive(true);

            currentOwner = owner;
        }

        public void Hide()
        {
            Hide(null);
        }

        public void Hide(object owner)
        {
            if (owner != null && currentOwner != null && owner != currentOwner)
                return;

            currentOwner = null;

            HideImmediately();
        }

        void HideImmediately()
        {
            if (root != null)
                root.SetActive(false);
            else
                gameObject.SetActive(false);
        }
    }
}
