using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using JuegoDeCartas.Managers;
using JuegoDeCartas.Cards;

namespace JuegoDeCartas.UI
{
    public class CardSelectionUI : MonoBehaviour
    {
        [Header("References")]
        public BattleManager battle;

        public GameObject panel;

        public Transform contentParent;

        public GameObject cardPrefab;

        public Action<Card> onCardSelected;
        public Action onCancel;
        public Action onClose;

        TextMeshProUGUI tituloText;
        GridLayoutGroup grid;

        void Awake()
        {
            panel = gameObject;
            contentParent = transform.Find("Scroll View/Viewport/Content");
            tituloText = transform.Find("Cabecero/Titulo")?.GetComponent<TextMeshProUGUI>();
            if (contentParent != null) grid = contentParent.GetComponent<GridLayoutGroup>();

            if (battle == null)
                battle = FindObjectOfType<BattleManager>();

            if (cardPrefab == null)
            {
#if UNITY_EDITOR
                cardPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Scripts/Prefabs/CardPrefab.prefab");
#endif
            }

            var volverBtn = transform.Find("Cabecero/BotonVolver")?.GetComponent<Button>();
            if (volverBtn != null)
            {
                volverBtn.onClick.RemoveAllListeners();
                volverBtn.onClick.AddListener(Close);
            }
        }

        public void OpenForSelection(List<Card> cards, string title, Action<Card> onSelect, Action onCancelAction)
        {
            onCardSelected = onSelect;
            onCancel = onCancelAction;

            if (tituloText != null)
            tituloText.text = title;
            onCardSelected = onSelect;
            onCancel = onCancelAction;

            int totalCards = cards.Count;
            int columns = 5;
            int rows = Mathf.CeilToInt((float)totalCards / columns);

            panel.SetActive(true);
            Canvas.ForceUpdateCanvases();

            if (grid != null)
            {
                float cols = grid.constraintCount;
                var contentRT = contentParent as RectTransform;
                float cellW = (contentRT.rect.width - (cols - 1) * grid.spacing.x) / cols;
                if (cellW > 0)
                    grid.cellSize = new Vector2(cellW, cellW / 0.7f);
            }

            foreach (var card in cards)
            {
                GameObject obj = Instantiate(cardPrefab, contentParent);

                CardView view = obj.GetComponent<CardView>();
                if (view != null)
                {
                    view.Setup(card, battle);

                    Button btn = obj.GetComponent<Button>();
                    if (btn == null)
                        btn = obj.AddComponent<Button>();

                    Card captured = card;
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => SelectCard(captured));
                }

                obj.transform.localScale = new Vector3(0.7f, 1.05f, 0.7f);

                CardHover hover = obj.GetComponent<CardHover>();
                if (hover != null)
                {
                    hover.enabled = true;
                    hover.hoverScale = 1.1f;
                    hover.originalScale = obj.transform.localScale;
                    hover.RefreshState();
                }

                if (view != null && view.costText != null)
                    view.costText.fontSize = 0.3f;

                var img = obj.GetComponent<Image>();
                if (img != null)
                {
                    img.color = new Color(1, 1, 1, 0);
                    img.raycastTarget = true;
                }

                foreach (var graphic in obj.GetComponentsInChildren<Graphic>(true))
                    if (graphic.gameObject != obj)
                        graphic.raycastTarget = false;
            }

            Canvas.ForceUpdateCanvases();
        }

        void SelectCard(Card card)
        {
            onCancel = null;
            onCardSelected?.Invoke(card);
            Close();
        }

        public void Close()
        {
            Debug.Log("[CardSelectionUI] Close() called, panel=" + panel.name);
            panel.SetActive(false);
            onCardSelected = null;
            var cancel = onCancel;
            onCancel = null;
            onClose?.Invoke();
            cancel?.Invoke();
        }
    }
}
