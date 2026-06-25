using System.Collections.Generic;
using JuegoDeCartas.Cards;
using JuegoDeCartas.Managers;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace JuegoDeCartas.UI
{
    public class CardInspectionOverlayUI : MonoBehaviour, IPointerClickHandler
    {
        const string ResourcePath = "CardInspectionOverlay";
        const string PreferredCanvasName = "CardInspectionOverlayCanvas";

        [Header("References")]
        public RectTransform cardAnchor;
        public GameObject cardPrefab;
        public RectTransform detailsPanel;
        public TextMeshProUGUI detailsText;

        [Header("Presentation")]
        public Vector2 previewCardSize = new Vector2(180f, 260f);
        public Vector3 previewCardScale = new Vector3(1.1f, 1.1f, 1.1f);
        public bool useResponsiveSizing = true;
        public Vector2 maxViewportUsage = new Vector2(0.22f, 0.58f);
        public float minPreviewScale = 0.9f;
        public float maxPreviewScale = 1.15f;

        GameObject currentCardInstance;

        public static bool TryShow(Card card, BattleManager battle, Component source)
        {
            if (card == null || card.data == null)
                return false;

            Transform parent = ResolveParent(source);
            if (parent == null)
                return false;

            CardInspectionOverlayUI overlay =
                FindAnyObjectByType<CardInspectionOverlayUI>(
                    FindObjectsInactive.Include
                );

            if (overlay == null)
            {
                CardInspectionOverlayUI prefab =
                    Resources.Load<CardInspectionOverlayUI>(ResourcePath);
                if (prefab == null)
                    return false;

                overlay = Instantiate(prefab, parent);
            }

            AttachToScreenOverlayCanvas(overlay, parent);
            overlay.Open(card, battle);
            return true;
        }

        static Transform ResolveParent(Component source)
        {
            Canvas screenCanvas = FindBestScreenCanvas(source);
            if (screenCanvas != null && screenCanvas.rootCanvas != null)
                return screenCanvas.rootCanvas.transform;

            Canvas canvas = source != null
                ? source.GetComponentInParent<Canvas>()
                : null;
            if (canvas != null &&
                canvas.rootCanvas != null &&
                canvas.rootCanvas.renderMode != RenderMode.WorldSpace)
            {
                return canvas.rootCanvas.transform;
            }

            Canvas fallback =
                FindAnyObjectByType<Canvas>(FindObjectsInactive.Include);
            if (fallback != null &&
                fallback.rootCanvas != null &&
                fallback.rootCanvas.renderMode != RenderMode.WorldSpace)
            {
                return fallback.rootCanvas.transform;
            }

            return null;
        }

        static Canvas FindBestScreenCanvas(Component source)
        {
            Canvas canvas = FindBestScreenCanvas(source, true);
            return canvas != null ? canvas : FindBestScreenCanvas(source, false);
        }

        static Canvas FindBestScreenCanvas(
            Component source,
            bool requireSameScene)
        {
            Scene sourceScene = source != null
                ? source.gameObject.scene
                : default;
            Canvas[] canvases = FindObjectsByType<Canvas>(
                FindObjectsInactive.Exclude
            );
            HashSet<Canvas> visitedRoots = new HashSet<Canvas>();
            Canvas best = null;
            int bestScore = int.MinValue;

            foreach (Canvas candidate in canvases)
            {
                if (candidate == null)
                    continue;

                Canvas root = candidate.rootCanvas;
                if (root == null ||
                    !visitedRoots.Add(root) ||
                    !root.isActiveAndEnabled ||
                    !root.gameObject.activeInHierarchy ||
                    root.renderMode == RenderMode.WorldSpace)
                {
                    continue;
                }

                if (requireSameScene &&
                    sourceScene.IsValid() &&
                    root.gameObject.scene != sourceScene)
                {
                    continue;
                }

                int score = root.sortingOrder;
                if (root.renderMode == RenderMode.ScreenSpaceOverlay)
                    score += 10000;
                if (root.name == PreferredCanvasName)
                    score += 20000;

                if (score <= bestScore)
                    continue;

                best = root;
                bestScore = score;
            }

            return best;
        }

        static void AttachToScreenOverlayCanvas(
            CardInspectionOverlayUI overlay,
            Transform parent)
        {
            if (overlay == null || parent == null)
                return;

            if (overlay.transform.parent != parent)
                overlay.transform.SetParent(parent, false);

            RectTransform rectTransform = overlay.transform as RectTransform;
            if (rectTransform == null)
                return;

            rectTransform.localScale = Vector3.one;
            rectTransform.localRotation = Quaternion.identity;
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
        }

        public void Open(Card card, BattleManager battle)
        {
            if (card == null || card.data == null)
                return;

            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            ClearCurrentCard();

            if (cardAnchor == null || cardPrefab == null)
                return;

            Card previewCard = new Card(card, true)
            {
                frozen = card.frozen
            };

            UpdateDetails(previewCard, battle);

            currentCardInstance = Instantiate(cardPrefab, cardAnchor);
            RectTransform cardRect = currentCardInstance.transform as RectTransform;
            Vector3 resolvedScale = ResolvePreviewScale();
            if (cardAnchor != null)
                cardAnchor.sizeDelta = new Vector2(
                    previewCardSize.x * resolvedScale.x,
                    previewCardSize.y * resolvedScale.y
                );

            if (cardRect != null)
            {
                cardRect.anchorMin = new Vector2(0.5f, 0.5f);
                cardRect.anchorMax = new Vector2(0.5f, 0.5f);
                cardRect.pivot = new Vector2(0.5f, 0.5f);
                cardRect.anchoredPosition = Vector2.zero;
                cardRect.sizeDelta = previewCardSize;
                cardRect.localScale = resolvedScale;
            }

            CardView view = currentCardInstance.GetComponentInChildren<CardView>();
            if (view != null)
            {
                view.Setup(previewCard, battle);
                view.interactable = false;
                view.inspectionEnabled = false;
            }

            foreach (Graphic graphic in currentCardInstance
                         .GetComponentsInChildren<Graphic>(true))
            {
                graphic.raycastTarget = false;
            }
        }

        public void Close()
        {
            ClearCurrentCard();
            gameObject.SetActive(false);
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Close();
                return;
            }

            if (Input.GetMouseButtonDown(0) && !PointerIsInsideOverlayContent())
            {
                Close();
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!PointerIsInsideOverlayContent())
                Close();
        }

        void ClearCurrentCard()
        {
            if (currentCardInstance == null)
                return;

            Destroy(currentCardInstance);
            currentCardInstance = null;
        }

        void UpdateDetails(Card card, BattleManager battle)
        {
            if (detailsPanel != null)
                detailsPanel.gameObject.SetActive(detailsText != null);

            if (detailsText == null)
                return;

            string title = card != null && card.data != null
                ? card.data.cardName
                : "Carta";
            string description = CardDescriptionBuilder.BuildInspectionDescription(
                card,
                battle
            );

            detailsText.text = $"{title}\n\n{description}";
        }

        Vector3 ResolvePreviewScale()
        {
            if (!useResponsiveSizing)
                return previewCardScale;

            Canvas.ForceUpdateCanvases();
            RectTransform overlayRect = transform as RectTransform;
            if (overlayRect == null ||
                overlayRect.rect.width <= 0f ||
                overlayRect.rect.height <= 0f ||
                previewCardSize.x <= 0f ||
                previewCardSize.y <= 0f)
            {
                return previewCardScale;
            }

            float maxWidth = overlayRect.rect.width * maxViewportUsage.x;
            float maxHeight = overlayRect.rect.height * maxViewportUsage.y;
            float scale = Mathf.Min(
                maxWidth / previewCardSize.x,
                maxHeight / previewCardSize.y
            );
            scale = Mathf.Clamp(scale, minPreviewScale, maxPreviewScale);
            return new Vector3(scale, scale, scale);
        }

        bool PointerIsInsideOverlayContent()
        {
            return PointerIsInside(cardAnchor) || PointerIsInside(detailsPanel);
        }

        static bool PointerIsInside(RectTransform rectTransform)
        {
            return rectTransform != null &&
                   RectTransformUtility.RectangleContainsScreenPoint(
                       rectTransform,
                       Input.mousePosition
                   );
        }
    }
}
