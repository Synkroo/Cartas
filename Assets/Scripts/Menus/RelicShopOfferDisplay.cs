using JuegoDeCartas.Missions;
using JuegoDeCartas.Relics;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JuegoDeCartas.UI
{
    public class RelicShopOfferDisplay : MonoBehaviour
    {
        public Button button;
        public Image iconImage;
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI descriptionText;
        public TextMeshProUGUI priceText;
        public GameObject unavailableState;
        public string currencySuffix = " oro";

        [Header("Animation")]
        public UITransitionAnimator transition;

        RelicData relic;
        ShopManager shop;
        MenuButtonMotion motion;

        void Awake()
        {
            if (button == null)
                button = GetComponent<Button>();
            motion = GetComponent<MenuButtonMotion>();
            if (button != null)
                button.onClick.AddListener(Buy);
        }

        public void Setup(
            RelicData newRelic,
            ShopManager shopManager,
            bool canPurchase)
        {
            relic = newRelic;
            shop = shopManager;
            bool hasRelic = relic != null;

            if (nameText != null)
                nameText.text = hasRelic ? relic.relicName : "Agotada";
            if (descriptionText != null)
                descriptionText.text = hasRelic ? relic.description : "";
            if (priceText != null)
            {
                int price = hasRelic
                    ? Mathf.RoundToInt(
                        relic.price * MissionRunState.ShopCostMultiplier
                    )
                    : 0;
                priceText.text = hasRelic ? price + currencySuffix : "";
            }
            if (iconImage != null)
            {
                iconImage.sprite = hasRelic ? relic.icon : null;
                iconImage.enabled = hasRelic && relic.icon != null;
            }
            if (button != null)
                button.interactable = hasRelic && canPurchase;
            if (unavailableState != null)
                unavailableState.SetActive(!hasRelic || !canPurchase);
            if (motion != null)
                motion.enabled = hasRelic;

            gameObject.SetActive(hasRelic);
        }

        public void PlayEntrance(float delay = 0f)
        {
            if (transition == null || !gameObject.activeSelf)
                return;

            transition.CaptureCurrentAsVisible();
            transition.PlayIn(delay);
        }

        public void SetPurchased()
        {
            relic = null;
            if (button != null)
                button.interactable = false;
            if (motion != null)
                motion.enabled = false;

            if (Application.isPlaying &&
                transition != null &&
                gameObject.activeInHierarchy)
            {
                transition.PlayOut(() => gameObject.SetActive(false));
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        void Buy()
        {
            if (relic != null && shop != null)
                shop.TryPurchaseRelic(relic);
        }
    }
}
