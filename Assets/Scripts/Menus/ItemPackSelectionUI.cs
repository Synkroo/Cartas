using System;
using TMPro;
using UnityEngine;
using JuegoDeCartas.Articulos;
using JuegoDeCartas.Progression;

namespace JuegoDeCartas.UI
{
    public class ItemPackSelectionUI : MonoBehaviour
    {
        public GameObject panel;
        public TextMeshProUGUI titleText;
        public Transform content;
        public GameObject itemChoicePrefab;

        ItemPackOffer currentOffer;
        Func<ArticuloData, bool> canSelect;
        Action<ArticuloData> onSelected;

        public bool IsConfigured => panel != null && content != null && itemChoicePrefab != null;

        void Awake()
        {
            if (panel == null)
                panel = gameObject;
        }

        public bool Open(
            ItemPackOffer offer,
            Func<ArticuloData, bool> selectionValidator,
            Action<ArticuloData> selected)
        {
            if (!IsConfigured || offer == null)
                return false;

            currentOffer = offer;
            canSelect = selectionValidator;
            onSelected = selected;
            Render();
            panel.SetActive(true);
            return true;
        }

        public void ShowCurrent()
        {
            if (currentOffer == null || panel == null)
                return;
            Render();
            panel.SetActive(true);
        }

        public void Hide()
        {
            if (panel != null)
                panel.SetActive(false);
        }

        public void Close()
        {
            Hide();
            currentOffer = null;
            canSelect = null;
            onSelected = null;
        }

        void Render()
        {
            for (int i = content.childCount - 1; i >= 0; i--)
            {
                GameObject child = content.GetChild(i).gameObject;
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
                PackItemChoiceDisplay display = instance.GetComponent<PackItemChoiceDisplay>();
                if (display != null)
                    display.Setup(item, canSelect == null || canSelect(item), Select);
            }

            ProfilePrefs.Save();
        }

        void Select(ArticuloData item)
        {
            onSelected?.Invoke(item);
        }
    }
}
