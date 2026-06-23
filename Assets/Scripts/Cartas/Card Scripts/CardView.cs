using TMPro;
using UnityEngine;
using UnityEngine.UI;
using JuegoDeCartas.Managers;

namespace JuegoDeCartas.Cards
{
    public class CardView : MonoBehaviour
    {
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI descriptionText;
        public TextMeshProUGUI costText;
        public TextMeshProUGUI upgradeStatusText;
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

        public bool interactable = true;

        private Card card;
        private BattleManager battleManager;
        private Button button;

        void Awake()
        {
            button = GetComponent<Button>();

            if (button != null)
            {
                button.onClick.AddListener(OnClick);
            }
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

            battleManager.PlayCard(card);
        }
    }
}
