using System;
using TMPro;
using UnityEngine;
using JuegoDeCartas.Articulos;
using JuegoDeCartas.Progression;
using UnityEngine.UI;

namespace JuegoDeCartas.UI
{
    public class ItemPackSelectionUI : MonoBehaviour
    {
        public GameObject panel;
        public TextMeshProUGUI titleText;
        public Transform content;
        public GameObject itemChoicePrefab;
        public Button cancelButton;
        public UITransitionAnimator transition;

        ItemPackOffer currentOffer;
        Func<ArticuloData, bool> canSelect;
        Action<ArticuloData> onSelected;
        Action onCancelled;

        public bool IsConfigured => panel != null && content != null && itemChoicePrefab != null;

        void Awake()
        {
            if (panel == null)
                panel = gameObject;
            if (cancelButton != null)
                cancelButton.onClick.AddListener(CancelSelection);
        }

        public bool Open(
            ItemPackOffer offer,
            Func<ArticuloData, bool> selectionValidator,
            Action<ArticuloData> selected,
            Action cancelled = null)
        {
            if (!IsConfigured || offer == null)
                return false;

            currentOffer = offer;
            canSelect = selectionValidator;
            onSelected = selected;
            onCancelled = cancelled;
            panel.SetActive(true);
            Render();
            if (transition != null)
                transition.PlayIn();
            return true;
        }

        public void ShowCurrent()
        {
            if (currentOffer == null || panel == null)
                return;
            panel.SetActive(true);
            Render();
            if (transition != null)
                transition.PlayIn();
        }

        public void Hide()
        {
            if (panel == null)
                return;

            if (transition != null && panel.activeSelf)
                transition.PlayOut(() => panel.SetActive(false));
            else
                panel.SetActive(false);
        }

        public void Close()
        {
            Hide();
            currentOffer = null;
            canSelect = null;
            onSelected = null;
            onCancelled = null;
        }

        void Render()
        {
            for (int i = content.childCount - 1; i >= 0; i--)
            {
                Transform childTransform = content.GetChild(i);
                childTransform.SetParent(null, false);
                GameObject child = childTransform.gameObject;
                if (Application.isPlaying)
                    Destroy(child);
                else
                    DestroyImmediate(child);
            }

            if (titleText != null)
                titleText.text = currentOffer.Definition != null ? currentOffer.Definition.packName : "Sobre";

            for (int i = 0; i < currentOffer.Contents.Count; i++)
            {
                ArticuloData item = currentOffer.Contents[i];
                if (item == null)
                    continue;

                CollectionProgress.MarkItemSeen(item);
                GameObject instance = Instantiate(itemChoicePrefab, content);
                instance.SetActive(true);
                PackItemChoiceDisplay display = instance.GetComponent<PackItemChoiceDisplay>();
                if (display != null)
                    display.Setup(item, canSelect == null || canSelect(item), Select, i * 0.06f);
            }

            ProfilePrefs.Save();
        }

        void Select(ArticuloData item)
        {
            onSelected?.Invoke(item);
        }

        public void CancelSelection()
        {
            onCancelled?.Invoke();
        }
    }
}
