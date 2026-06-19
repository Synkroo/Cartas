using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using JuegoDeCartas.Managers;
using JuegoDeCartas.Articulos;
using JuegoDeCartas.Cards;
using JuegoDeCartas.Missions;
using JuegoDeCartas.Progression;

namespace JuegoDeCartas.UI
{
    public class ShopManager : MonoBehaviour
    {
        [Header("External References")]
        public DeckViewerUI deckViewer;

        public BattleManager battle;

        public GameManager gameManager;

        public CardSelectionUI cardSelectionUI;

        public UpgradeSelectionUI upgradeSelectionUI;

        public GameObject shopPanel;
        public UITransitionAnimator shopTransition;

        public TextMeshProUGUI dineroText;

        public Canvas menusCanvas;

        public bool pauseTime = true;

        [Header("Text")]
        public TextMeshProUGUI shopTitleText;
        public string shopTitle = "Elige un sobre";
        public string currencySuffix = " oro";
        public TextMeshProUGUI interestText;
        public string interestFormat = "Intereses: +{0} oro";

        [Header("Interest")]
        [Min(1)] public int goldPerInterestStep = 100;
        [Min(0)] public int interestPerStep = 25;
        [Min(0)] public int maxInterest = 125;

        [Header("Item Pool")]
        public List<ArticuloData> itemPool = new List<ArticuloData>();

        [Header("Packs")]
        public List<ItemPackData> packDefinitions = new List<ItemPackData>();
        public GameObject packPrefab;
        public ItemPackSelectionUI packSelectionUI;

        [Header("Restock")]
        public int restockCost = 100;

        [Header("Slots")]
        public Transform[] slotContainers = new Transform[3];

        List<GameObject> spawnedItems = new List<GameObject>();
        readonly List<ItemPackOffer> offers = new List<ItemPackOffer>();
        readonly Dictionary<ItemPackOffer, ItemPackDisplay> displays = new Dictionary<ItemPackOffer, ItemPackDisplay>();
        GraphicRaycaster menusRaycaster;
        List<GraphicRaycaster> disabledRaycasters = new List<GraphicRaycaster>();
        List<GraphicRaycaster> allRaycasters = new List<GraphicRaycaster>();
        bool raycastersCached;
        float previousTimeScale = 1f;
        bool opened;
        int lastInterestEarned;

        public IReadOnlyList<ItemPackOffer> CurrentOffers => new ReadOnlyCollection<ItemPackOffer>(offers);
        public int LastInterestEarned => lastInterestEarned;

        void Awake()
        {
            if (menusCanvas != null)
                menusRaycaster = menusCanvas.GetComponent<GraphicRaycaster>()
                                 ?? menusCanvas.gameObject.AddComponent<GraphicRaycaster>();

            if (shopTitleText == null)
                shopTitleText = transform.Find("Cabecero/TituloText")?.GetComponent<TextMeshProUGUI>();
        }

        void CacheRaycasters()
        {
            if (raycastersCached) return;
            allRaycasters.Clear();
            allRaycasters.AddRange(FindObjectsByType<GraphicRaycaster>(FindObjectsInactive.Include));
            raycastersCached = true;
        }

        void OnEnable()
        {
            var salirBtn = transform.Find("BotonSalir")?.GetComponent<Button>();
            if (salirBtn != null)
            {
                salirBtn.onClick.RemoveAllListeners();
                salirBtn.onClick.AddListener(OnSalir);
            }

            var verMazoBtn = transform.Find("BotonVerMazo")?.GetComponent<Button>();
            if (verMazoBtn != null)
            {
                verMazoBtn.onClick.RemoveAllListeners();
                verMazoBtn.onClick.AddListener(OnVerMazo);
            }

            var restockBtn = transform.Find("Cabecero/PanelRestock")?.GetComponent<Button>();
            if (restockBtn != null)
            {
                restockBtn.onClick.RemoveAllListeners();
                restockBtn.onClick.AddListener(OnRestock);
            }
        }

        public void Open()
        {
            if (opened)
                return;

            opened = true;
            previousTimeScale = Time.timeScale;

            if (pauseTime)
                Time.timeScale = 0f;

            ApplyInterest();

            if (shopPanel != null)
                shopPanel.SetActive(true);
            if (shopTransition != null)
                shopTransition.PlayIn();

            ClearSlots();
            GenerateAndPopulatePacks();

            SetActiveAndBlockOthers();

            if (menusCanvas != null)
                menusCanvas.enabled = true;

            if (menusRaycaster != null)
                menusRaycaster.enabled = true;

            UpdateDineroUI();

            if (shopTitleText != null)
                shopTitleText.text = shopTitle;
            if (interestText != null)
                interestText.text = string.Format(interestFormat, lastInterestEarned);

            var restockPrecioText = transform.Find("Cabecero/PanelRestock/Precio200")?.GetComponent<TextMeshProUGUI>();
            if (restockPrecioText != null)
                restockPrecioText.text = restockCost + currencySuffix;
        }

        void ClearSlots()
        {
            foreach (var go in spawnedItems)
                if (go != null) Destroy(go);
            spawnedItems.Clear();
        }

        void GenerateAndPopulatePacks()
        {
            offers.Clear();
            displays.Clear();

            if (packPrefab == null)
                return;

            if (cardSelectionUI == null)
                cardSelectionUI = GetComponentInChildren<CardSelectionUI>(true);

            int count = slotContainers.Length;
            for (int i = 0; i < count; i++)
            {
                if (slotContainers[i] == null) continue;

                ItemPackData definition = ItemPackGenerator.RollDefinition(packDefinitions);
                if (definition == null) continue;

                CollectionProgress.MarkPackSeen(definition);
                ItemPackOffer offer = ItemPackGenerator.Generate(definition, itemPool);
                offers.Add(offer);

                GameObject itemGO = Instantiate(packPrefab, slotContainers[i]);
                itemGO.transform.localPosition = Vector3.zero;
                ItemPackDisplay display = itemGO.GetComponent<ItemPackDisplay>();
                if (display != null)
                {
                    display.Setup(offer, OpenPack);
                    display.PlayEntrance(i * 0.08f);
                    displays[offer] = display;
                }

                spawnedItems.Add(itemGO);
            }

            ProfilePrefs.Save();
        }

        void OpenPack(ItemPackOffer offer)
        {
            TryPurchasePack(offer);
        }

        public bool TryPurchasePack(ItemPackOffer offer)
        {
            if (offer == null || offer.Claimed || offer.Definition == null || gameManager == null)
                return false;
            if (packSelectionUI == null || !packSelectionUI.IsConfigured)
                return false;
            if (!offer.Contents.Exists(CanAcquireItem))
                return false;

            int price = Mathf.RoundToInt(offer.Definition.price * MissionRunState.ShopCostMultiplier);
            if (gameManager.dinero < price)
                return false;

            gameManager.dinero -= price;
            UpdateDineroUI();

            if (shopPanel != null)
                shopPanel.SetActive(false);

            if (!packSelectionUI.Open(
                offer,
                CanAcquireItem,
                item => TryClaimItem(offer, item),
                () => CancelPack(offer)))
            {
                gameManager.dinero += price;
                UpdateDineroUI();
                if (shopPanel != null)
                    shopPanel.SetActive(true);
                return false;
            }

            return true;
        }

        bool CanAcquireItem(ArticuloData item)
        {
            if (item == null || battle == null)
                return false;
            if (!ItemEffectApplier.NeedsSelection(item.tipoEfecto))
                return true;

            List<Card> source = ItemEffectApplier.GetSelectionSource(item, battle);
            return source != null && source.Count > 0 && cardSelectionUI != null && cardSelectionUI.IsConfigured;
        }

        public bool TryClaimItem(ItemPackOffer offer, ArticuloData item)
        {
            if (offer == null || offer.Claimed || item == null || battle == null || !offer.Contents.Contains(item))
                return false;

            if (!ItemEffectApplier.NeedsSelection(item.tipoEfecto))
            {
                ItemEffectApplier.Apply(item, battle);
                CompleteClaim(offer);
                return true;
            }

            List<Card> source = ItemEffectApplier.GetSelectionSource(item, battle);
            if (source == null || source.Count == 0 || cardSelectionUI == null)
                return false;

            packSelectionUI.Hide();
            cardSelectionUI.OpenForSelection(
                source,
                item.descripcion,
                selected =>
                {
                    if (selected == null)
                    {
                        packSelectionUI.ShowCurrent();
                        return;
                    }

                    if (item.tipoEfecto == TipoEfectoArticulo.MejorarCarta &&
                        upgradeSelectionUI != null &&
                        upgradeSelectionUI.IsConfigured &&
                        upgradeSelectionUI.Show(selected, () => CompleteClaim(offer)))
                    {
                        return;
                    }

                    ItemEffectApplier.ApplyToSelected(item, battle, selected);
                    CompleteClaim(offer);
                },
                () => packSelectionUI.ShowCurrent()
            );
            return true;
        }

        void CompleteClaim(ItemPackOffer offer)
        {
            CompleteOffer(offer);
        }

        public bool CancelPack(ItemPackOffer offer)
        {
            if (offer == null || offer.Claimed || !offers.Contains(offer))
                return false;

            CompleteOffer(offer);
            return true;
        }

        void CompleteOffer(ItemPackOffer offer)
        {
            offer.Claimed = true;
            if (displays.TryGetValue(offer, out ItemPackDisplay display) && display != null)
                display.SetClaimed(true);

            if (packSelectionUI != null)
                packSelectionUI.Close();
            if (shopPanel != null)
                shopPanel.SetActive(true);
        }

        void SetActiveAndBlockOthers()
        {
            CacheRaycasters();
            disabledRaycasters.Clear();
            foreach (var rc in allRaycasters)
            {
                if (rc != menusRaycaster && rc.enabled)
                {
                    rc.enabled = false;
                    disabledRaycasters.Add(rc);
                }
            }
        }

        public void Close()
        {
            if (!opened)
                return;

            opened = false;

            if (shopTransition != null && shopPanel != null && shopPanel.activeSelf)
                shopTransition.PlayOut(FinishClose);
            else
                FinishClose();
        }

        void FinishClose()
        {
            foreach (var rc in disabledRaycasters)
                if (rc != null) rc.enabled = true;
            disabledRaycasters.Clear();

            if (shopPanel != null)
                shopPanel.SetActive(false);
            if (menusCanvas != null)
                menusCanvas.enabled = false;
            if (pauseTime)
                Time.timeScale = previousTimeScale;
            if (battle != null)
                battle.ContinueAfterShop();
        }

        public int CalculateInterest(int gold)
        {
            if (gold <= 0 || goldPerInterestStep <= 0 || interestPerStep <= 0)
                return 0;

            int steps = gold / goldPerInterestStep;
            return Mathf.Min(maxInterest, steps * interestPerStep);
        }

        void ApplyInterest()
        {
            lastInterestEarned = gameManager != null
                ? CalculateInterest(gameManager.dinero)
                : 0;

            if (lastInterestEarned <= 0 || gameManager == null)
                return;

            gameManager.dinero += lastInterestEarned;
            if (battle != null && battle.statsTracker != null)
                battle.statsTracker.RegisterInterestEarned(lastInterestEarned);
        }

        int lastDinero = -1;

        public void UpdateDineroUI()
        {
            if (dineroText != null && gameManager != null)
            {
                dineroText.text = gameManager.dinero + currencySuffix;
                if (gameManager.dinero != lastDinero)
                {
                    lastDinero = gameManager.dinero;
                    StopCoroutine(PulseGold());
                    StartCoroutine(PulseGold());
                }
            }
        }

        IEnumerator PulseGold()
        {
            float dur = 0.3f;
            float t = 0;
            while (t < dur)
            {
                float p = t / dur;
                float s = 1f + Mathf.Sin(p * Mathf.PI) * 0.15f;
                dineroText.transform.localScale = Vector3.one * s;
                t += Time.unscaledDeltaTime;
                yield return null;
            }
            dineroText.transform.localScale = Vector3.one;
        }

        public void OnSalir()
        {
            Close();
        }

        public void OnVerMazo()
        {
            if (deckViewer == null) return;

            if (shopPanel != null)
                shopPanel.SetActive(false);

            deckViewer.onClose -= OnDeckViewerClosed;
            deckViewer.onClose += OnDeckViewerClosed;
            deckViewer.Open();
        }

        void OnDeckViewerClosed()
        {
            deckViewer.onClose -= OnDeckViewerClosed;
            if (shopPanel != null)
                shopPanel.SetActive(true);
        }

        public void OnRestock()
        {
            TryRestock();
        }

        public bool TryRestock()
        {
            if (gameManager == null)
                return false;
            if (gameManager.dinero < restockCost)
                return false;

            gameManager.dinero -= restockCost;
            UpdateDineroUI();

            ClearSlots();
            GenerateAndPopulatePacks();
            return true;
        }
    }
}
