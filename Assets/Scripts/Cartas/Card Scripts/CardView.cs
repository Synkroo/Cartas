using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using JuegoDeCartas.Managers;
using JuegoDeCartas.UI;

namespace JuegoDeCartas.Cards
{
    public class CardView : MonoBehaviour, IPointerClickHandler
    {
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI descriptionText;
        public TextMeshProUGUI costText;
        public TextMeshProUGUI upgradeStatusText;
        public Image backgroundImage;
        public Image artworkImage;
        public Image frameImage;

        [Header("Upgrade Presentation")]
        public Sprite normalFrameSprite;
        public Sprite upgradedFrameSprite;
        public Sprite epiphanyFrameSprite;
        public string upgradedDescriptionFormat = "{0}\n{1}";
        public string combinedUpgradeDescriptionFormat = "{0} {1}";
        public string reducedCostDescriptionFormat = "Coste -{0}.";
        public string reactivationDescriptionFormat = "{0} usos.";

        [Header("Runtime Presentation")]
        public Color normalBackgroundTint = new Color(1f, 1f, 1f, 0.39f);
        public Color frozenBackgroundTint = new Color(0.25f, 0.65f, 1f, 0.72f);
        public Color burnedBackgroundTint = new Color(1f, 0.26f, 0.18f, 0.72f);
        public Color selectedBackgroundTint = new Color(1f, 0.86f, 0.25f, 0.6f);
        public Color normalGraphicTint = Color.white;
        public Color frozenGraphicTint = new Color(0.58f, 0.82f, 1f, 1f);
        public Color burnedGraphicTint = new Color(1f, 0.58f, 0.5f, 1f);
        public Color selectedGraphicTint = new Color(1f, 0.92f, 0.55f, 1f);

        [Header("Text Fit")]
        public bool autoFitDescription = true;
        public float descriptionMinSize = 0.13f;
        public float descriptionMaxSize = 0.2f;

        public bool interactable = true;
        public bool inspectionEnabled = true;

        private Card card;
        private BattleManager battleManager;
        private Button button;
        private RectTransform rectTransform;
        private static CardView activeInspectionView;
        private static CombatTooltipUI activeInspectionTooltip;

        void Awake()
        {
            button = GetComponent<Button>();
            rectTransform = GetComponent<RectTransform>();
            if (backgroundImage == null)
                backgroundImage = GetComponent<Image>();

            if (button != null)
            {
                button.onClick.AddListener(OnClick);
            }
        }

        void Update()
        {
            if (activeInspectionView != this)
                return;

            if (!Input.GetMouseButtonDown(0) && !Input.GetMouseButtonDown(1))
                return;

            if (rectTransform != null &&
                RectTransformUtility.RectangleContainsScreenPoint(
                    rectTransform,
                    Input.mousePosition
                ))
            {
                return;
            }

            HideInspection();
        }

        void OnDisable()
        {
            if (activeInspectionView == this)
                HideInspection();
        }

        public void Setup(Card newCard, BattleManager manager)
        {
            card = newCard;
            battleManager = manager;

            if (card == null || card.data == null)
            {
                if (nameText != null) nameText.text = "";
                if (descriptionText != null) descriptionText.text = "";
                if (costText != null) costText.text = "";
                if (upgradeStatusText != null)
                    upgradeStatusText.gameObject.SetActive(false);
                if (artworkImage != null) artworkImage.enabled = false;
                ApplyRuntimePresentation();
                return;
            }

            CardEpiphany epiphany = card.Epiphany;
            if (nameText != null)
            {
                nameText.text = epiphany != null &&
                                !string.IsNullOrWhiteSpace(epiphany.epiphanyName)
                    ? epiphany.epiphanyName
                    : card.data.cardName;
            }
            if (descriptionText != null)
            {
                if (epiphany != null)
                {
                    descriptionText.text =
                        CardDescriptionBuilder.BuildFinalDescription(card);
                }
                else
                {
                    CardUpgradeOption upgrade = card.SelectedUpgrade;
                    string baseDescription = battleManager != null
                        ? battleManager.GetRuntimeCardDescription(card.data)
                        : card.data.description;
                    string upgradeDescription = GetUpgradeDescription(card, upgrade);
                    descriptionText.text = card.upgraded &&
                                           !string.IsNullOrWhiteSpace(upgradeDescription)
                        ? string.Format(
                            upgradedDescriptionFormat,
                            baseDescription,
                            upgradeDescription
                        )
                        : baseDescription;
                }
            }
            if (costText != null) costText.text = card.effectiveCost.ToString();
            if (artworkImage != null)
            {
                artworkImage.sprite = card.data.sprite;
                artworkImage.enabled = card.data.sprite != null;
            }

            if (frameImage != null)
            {
                Sprite frame = epiphany != null
                    ? epiphanyFrameSprite
                    : card.upgraded
                        ? upgradedFrameSprite
                        : normalFrameSprite;
                if (frame != null)
                    frameImage.sprite = frame;
            }

            if (upgradeStatusText != null)
            {
                upgradeStatusText.text = "";
                upgradeStatusText.gameObject.SetActive(false);
            }

            FitDescriptionText();
            ApplyRuntimePresentation();
        }

        string GetUpgradeDescription(Card currentCard, CardUpgradeOption upgrade)
        {
            if (upgrade != null)
                return upgrade.GetCardDescription();

            if (currentCard.costReduction > 0 &&
                currentCard.reactivationCount > 0)
            {
                return string.Format(
                    reducedCostDescriptionFormat,
                    currentCard.costReduction
                ) + " " + string.Format(
                    reactivationDescriptionFormat,
                    currentCard.reactivationCount + 1
                );
            }

            if (currentCard.costReduction > 0)
            {
                return string.Format(
                    reducedCostDescriptionFormat,
                    currentCard.costReduction
                );
            }

            if (currentCard.reactivationCount > 0)
            {
                return string.Format(
                    reactivationDescriptionFormat,
                    currentCard.reactivationCount + 1
                );
            }

            return "";
        }

        public void OnClick()
        {
            if (!interactable)
                return;

            if (battleManager == null || card == null)
                return;

            battleManager.HandleCardClicked(card);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!inspectionEnabled)
                return;

            if (eventData == null ||
                eventData.button != PointerEventData.InputButton.Right)
            {
                return;
            }

            ShowInspection();
        }

        void ShowInspection()
        {
            if (card == null || card.data == null)
                return;

            if (CardInspectionOverlayUI.TryShow(card, battleManager, this))
                return;

            if (activeInspectionTooltip == null)
            {
                activeInspectionTooltip =
                    FindAnyObjectByType<CombatTooltipUI>(
                        FindObjectsInactive.Include
                    );
            }

            if (activeInspectionTooltip == null)
                return;

            activeInspectionView = this;
            activeInspectionTooltip.Show(
                this,
                GetDisplayName(),
                CardDescriptionBuilder.BuildInspectionDescription(
                    card,
                    battleManager
                )
            );
        }

        void HideInspection()
        {
            if (activeInspectionTooltip != null)
                activeInspectionTooltip.Hide(this);

            activeInspectionView = null;
        }

        string GetDisplayName()
        {
            CardEpiphany epiphany = card != null ? card.Epiphany : null;
            return epiphany != null &&
                   !string.IsNullOrWhiteSpace(epiphany.epiphanyName)
                ? epiphany.epiphanyName
                : card?.data != null
                    ? card.data.cardName
                    : "";
        }

        void FitDescriptionText()
        {
            if (!autoFitDescription || descriptionText == null)
                return;

            descriptionText.enableAutoSizing = true;
            descriptionText.fontSizeMin = Mathf.Max(0.01f, descriptionMinSize);
            descriptionText.fontSizeMax = Mathf.Max(
                descriptionText.fontSizeMin,
                descriptionMaxSize
            );
            descriptionText.overflowMode = TextOverflowModes.Ellipsis;
        }

        public void ApplyInspectionDescription(
            string fullDescription,
            float minSize,
            float maxSize,
            Vector2 descriptionSize,
            Vector2 descriptionPosition)
        {
            if (descriptionText == null)
                return;

            descriptionText.text = fullDescription;
            descriptionText.enableAutoSizing = true;
            descriptionText.fontSizeMin = Mathf.Max(0.01f, minSize);
            descriptionText.fontSizeMax = Mathf.Max(
                descriptionText.fontSizeMin,
                maxSize
            );
            descriptionText.overflowMode = TextOverflowModes.Overflow;
            descriptionText.alignment = TextAlignmentOptions.TopLeft;

            RectTransform descriptionRect =
                descriptionText.transform as RectTransform;
            if (descriptionRect == null)
                return;

            descriptionRect.sizeDelta = descriptionSize;
            descriptionRect.anchoredPosition = descriptionPosition;
        }

        void ApplyRuntimePresentation()
        {
            bool isFrozen = card != null && card.frozen;
            bool isBurned = card != null && card.burned;
            bool isTargetSource = battleManager != null &&
                                  battleManager.IsCardAwaitingMechanicTarget(card);

            Color backgroundTint = isTargetSource
                ? selectedBackgroundTint
                : isFrozen
                    ? frozenBackgroundTint
                    : isBurned
                        ? burnedBackgroundTint
                        : normalBackgroundTint;
            Color graphicTint = isTargetSource
                ? selectedGraphicTint
                : isFrozen
                    ? frozenGraphicTint
                    : isBurned
                        ? burnedGraphicTint
                        : normalGraphicTint;

            if (backgroundImage != null)
                backgroundImage.color = backgroundTint;
            if (frameImage != null)
                frameImage.color = graphicTint;
            if (artworkImage != null)
                artworkImage.color = graphicTint;
        }
    }
}
