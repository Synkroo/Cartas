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

        [Header("Layout")]
        public Vector2 minimumGridSpacing = new Vector2(30f, 40f);
        public Vector2 fallbackGridCellSize = new Vector2(210f, 310f);
        public Vector3 cardScale = new Vector3(60f, 90f, 60f);
        public bool normalizeContentTransform = true;
        public bool enforceFixedColumns = true;
        [Min(1)] public int fixedColumnCount = 5;
        public bool wrapCardsInLayoutCells = true;
        public Vector3 cardSlotScale = new Vector3(0.8f, 0.8f, 0.8f);

        void OnEnable()
        {
            var totalesBtn = transform.Find("Panel/TopBar/Totales")?.GetComponent<Button>();
            if (totalesBtn != null)
            {
                totalesBtn.onClick.RemoveAllListeners();
                totalesBtn.onClick.AddListener(ShowTotal);
            }

            if (panel != null && panel.activeInHierarchy)
                ShowRemaining();
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
            if (battle == null || battle.deckManager == null)
                return;

            RenderCards(battle.deckManager.deck);
        }

        public void ShowDiscard()
        {
            if (battle == null || battle.deckManager == null)
                return;

            RenderCards(battle.deckManager.discard);
        }

        public void ShowTotal()
        {
            if (battle == null || battle.deckManager == null)
                return;

            var dm = battle.deckManager;
            var all = new List<Card>(dm.deck.Count + dm.hand.Count + dm.discard.Count);
            all.AddRange(dm.deck);
            all.AddRange(dm.hand);
            all.AddRange(dm.discard);
            RenderCards(all);
        }

        void RenderCards(List<Card> cards)
        {
            if (contentParent == null || cardPrefab == null)
                return;

            NormalizeContentTransform();

            GridLayoutGroup grid = contentParent != null
                ? contentParent.GetComponent<GridLayoutGroup>()
                : null;
            if (grid != null)
            {
                if (grid.cellSize.x <= 0.01f || grid.cellSize.y <= 0.01f)
                {
                    grid.cellSize = fallbackGridCellSize;
                }

                grid.spacing = new Vector2(
                    Mathf.Max(grid.spacing.x, minimumGridSpacing.x),
                    Mathf.Max(grid.spacing.y, minimumGridSpacing.y)
                );

                if (enforceFixedColumns)
                {
                    grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                    grid.constraintCount = Mathf.Max(1, fixedColumnCount);
                }
            }

            foreach (Transform child in contentParent)
            {
                Destroy(child.gameObject);
            }

            foreach (var card in cards)
            {
                Transform parent = contentParent;
                if (wrapCardsInLayoutCells)
                    parent = CreateCardSlot(grid).transform;

                GameObject obj = Instantiate(cardPrefab, parent);
                ConfigureCardTransform(obj);

                CardView view = obj.GetComponentInChildren<CardView>(true);
                if (view != null)
                {
                    view.Setup(card, battle);
                    view.interactable = false;
                }

                CardHover hover = obj.GetComponentInChildren<CardHover>(true);

                if (hover != null)
                {
                    hover.enabled = true;
                    hover.originalScale = obj.transform.localScale;
                    hover.RefreshState();
                }
            }

            Canvas.ForceUpdateCanvases();
        }

        void NormalizeContentTransform()
        {
            if (!normalizeContentTransform || contentParent == null)
                return;

            RectTransform rect = contentParent as RectTransform;
            if (rect == null)
                return;

            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = Vector2.zero;
            rect.offsetMin = new Vector2(0f, rect.offsetMin.y);
            rect.offsetMax = new Vector2(0f, rect.offsetMax.y);
        }

        GameObject CreateCardSlot(GridLayoutGroup grid)
        {
            GameObject slot = new GameObject(
                "Card Slot",
                typeof(RectTransform),
                typeof(LayoutElement)
            );
            slot.transform.SetParent(contentParent, false);

            RectTransform rect = slot.transform as RectTransform;
            if (rect != null)
            {
                Vector2 size = grid != null ? grid.cellSize : fallbackGridCellSize;
                rect.localScale = cardSlotScale;
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = size;
            }

            LayoutElement layout = slot.GetComponent<LayoutElement>();
            if (layout != null)
            {
                Vector2 size = grid != null ? grid.cellSize : fallbackGridCellSize;
                layout.preferredWidth = size.x;
                layout.preferredHeight = size.y;
                layout.flexibleWidth = 0f;
                layout.flexibleHeight = 0f;
            }

            return slot;
        }

        void ConfigureCardTransform(GameObject obj)
        {
            if (obj == null)
                return;

            obj.transform.localScale = cardScale;
            obj.transform.localRotation = Quaternion.identity;

            RectTransform rect = obj.transform as RectTransform;
            if (rect == null)
                return;

            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
        }
    }
}
