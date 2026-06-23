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
using JuegoDeCartas.Characters;
using JuegoDeCartas.Challenges;
using JuegoDeCartas.Relics;

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
        public SubclassSelectionUI subclassSelectionUI;

        public GameObject shopPanel;
        public UITransitionAnimator shopTransition;

        [Header("Shop Content")]
        public List<GameObject> shopContentObjects = new List<GameObject>();

        public TextMeshProUGUI dineroText;

        public Canvas menusCanvas;

        public bool pauseTime = true;

        [Header("Controls")]
        public Button closeButton;
        public Button deckButton;
        public Button restockButton;
        public TextMeshProUGUI restockPriceText;

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

        [Header("Relics")]
        public RelicInventory relicInventory;
        public List<RelicData> relicPool = new List<RelicData>();
        public List<RelicShopOfferDisplay> relicOfferDisplays =
            new List<RelicShopOfferDisplay>();
        public RelicReplacementUI relicReplacementUI;

        [Header("Restock")]
        public int restockCost = 100;

        [Header("Slots")]
        public Transform[] slotContainers = new Transform[3];

        List<GameObject> spawnedItems = new List<GameObject>();
        readonly List<ItemPackOffer> offers = new List<ItemPackOffer>();
        readonly Dictionary<ItemPackOffer, ItemPackDisplay> displays = new Dictionary<ItemPackOffer, ItemPackDisplay>();
        readonly List<RelicData> relicOffers = new List<RelicData>();
        GraphicRaycaster menusRaycaster;
        List<GraphicRaycaster> disabledRaycasters = new List<GraphicRaycaster>();
        List<GraphicRaycaster> allRaycasters = new List<GraphicRaycaster>();
        bool raycastersCached;
        float previousTimeScale = 1f;
        bool opened;
        int lastInterestEarned;
        Coroutine goldPulse;
        RelicData pendingRelicPurchase;
        int pendingRelicPrice;

        public IReadOnlyList<ItemPackOffer> CurrentOffers => new ReadOnlyCollection<ItemPackOffer>(offers);
        public IReadOnlyList<RelicData> CurrentRelicOffers =>
            new ReadOnlyCollection<RelicData>(relicOffers);
        public int LastInterestEarned => lastInterestEarned;

        void Awake()
        {
            if (menusCanvas != null)
                menusRaycaster = menusCanvas.GetComponent<GraphicRaycaster>();

            if (menusCanvas != null && menusRaycaster == null)
                Debug.LogError("El Canvas de tienda necesita un GraphicRaycaster configurado en la escena.", this);

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
            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(OnSalir);
            }

            if (deckButton != null)
            {
                deckButton.onClick.RemoveAllListeners();
                deckButton.onClick.AddListener(OnVerMazo);
            }

            if (restockButton != null)
            {
                restockButton.onClick.RemoveAllListeners();
                restockButton.onClick.AddListener(OnRestock);
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

            if (ChallengeRunState.IsShopDisabled)
                lastInterestEarned = 0;
            else
                ApplyInterest();

            if (shopPanel != null)
                shopPanel.SetActive(true);
            if (shopTransition != null)
                shopTransition.PlayIn();

            SetActiveAndBlockOthers();

            if (menusCanvas != null)
                menusCanvas.enabled = true;

            if (menusRaycaster != null)
                menusRaycaster.enabled = true;

            if (!TryOpenSubclassSelection())
            {
                if (ChallengeRunState.IsShopDisabled)
                    Close();
                else
                    ShowPackShop();
            }
        }

        bool TryOpenSubclassSelection()
        {
            CharacterData character = CharacterRunState.SelectedCharacter;
            if (CharacterRunState.HasSubclass ||
                character == null ||
                character.subclasses == null ||
                character.subclasses.Count == 0 ||
                battle == null ||
                battle.waveManager == null ||
                battle.waveManager.CompletedCombatCount != battle.waveManager.SubclassSelectionCombat ||
                subclassSelectionUI == null)
            {
                return false;
            }

            SetShopContentVisible(false);
            if (subclassSelectionUI.Open(character, SelectSubclass))
                return true;

            SetShopContentVisible(true);
            return false;
        }

        void SelectSubclass(SubclassData subclass)
        {
            if (battle == null || !battle.ActivateSubclass(subclass))
            {
                if (!TryOpenSubclassSelection())
                    ShowPackShop();
                return;
            }

            if (ChallengeRunState.IsShopDisabled)
                Close();
            else
                ShowPackShop();
        }

        void ShowPackShop()
        {
            if (subclassSelectionUI != null)
                subclassSelectionUI.Close();

            SetShopContentVisible(true);
            GenerateAndPopulatePacks(offers.Count > 0);
            GenerateAndPopulateRelics(relicOffers.Count > 0);
            UpdateDineroUI();

            if (shopTitleText != null)
                shopTitleText.text = shopTitle;
            if (interestText != null)
                interestText.text = string.Format(interestFormat, lastInterestEarned);

            if (restockPriceText != null)
                restockPriceText.text = restockCost + currencySuffix;
        }

        void ClearSlots()
        {
            foreach (var go in spawnedItems)
                DestroySpawnedItem(go);
            spawnedItems.Clear();
        }

        void DestroySpawnedItem(GameObject item)
        {
            if (item == null)
                return;

            item.SetActive(false);
            item.transform.SetParent(null, false);
            if (Application.isPlaying)
                Destroy(item);
            else
                DestroyImmediate(item);
        }

        void GenerateAndPopulatePacks(bool keepReserved)
        {
            var previousOffers = keepReserved
                ? new List<ItemPackOffer>(offers)
                : null;
            var previousItems = keepReserved
                ? new List<GameObject>(spawnedItems)
                : null;
            offers.Clear();
            displays.Clear();
            spawnedItems.Clear();

            if (packPrefab == null)
                return;

            if (cardSelectionUI == null)
                cardSelectionUI = GetComponentInChildren<CardSelectionUI>(true);

            int count = slotContainers.Length;
            for (int i = 0; i < count; i++)
            {
                if (slotContainers[i] == null) continue;

                bool preserveReserved =
                    previousOffers != null &&
                    i < previousOffers.Count &&
                    previousOffers[i] != null &&
                    previousOffers[i].Reserved &&
                    !previousOffers[i].Claimed &&
                    previousItems != null &&
                    i < previousItems.Count &&
                    previousItems[i] != null;

                ItemPackOffer offer = preserveReserved
                    ? previousOffers[i]
                    : null;
                GameObject itemGO = preserveReserved
                    ? previousItems[i]
                    : null;

                if (offer == null)
                {
                    if (previousItems != null && i < previousItems.Count)
                        DestroySpawnedItem(previousItems[i]);

                    ItemPackData definition = ItemPackGenerator.RollDefinition(packDefinitions);
                    if (definition == null)
                        continue;

                    CollectionProgress.MarkPackSeen(definition);
                    offer = ItemPackGenerator.Generate(definition, itemPool);
                    itemGO = Instantiate(packPrefab, slotContainers[i]);
                }

                offers.Add(offer);

                itemGO.transform.SetParent(slotContainers[i], false);
                itemGO.transform.localPosition = Vector3.zero;
                itemGO.SetActive(true);
                ItemPackDisplay display = itemGO.GetComponent<ItemPackDisplay>();
                if (display != null)
                {
                    display.Setup(
                        offer,
                        OpenPack,
                        reservedOffer => ToggleReservation(reservedOffer)
                    );
                    if (!preserveReserved)
                        display.PlayEntrance(i * 0.08f);
                    displays[offer] = display;
                }

                spawnedItems.Add(itemGO);
            }

            if (previousItems != null)
            {
                for (int i = count; i < previousItems.Count; i++)
                    DestroySpawnedItem(previousItems[i]);
            }

            ProfilePrefs.Save();
        }

        void GenerateAndPopulateRelics(bool avoidCurrent)
        {
            if (relicInventory == null && battle != null)
                relicInventory = battle.relicInventory;

            List<RelicData> previous = avoidCurrent
                ? new List<RelicData>(relicOffers)
                : null;
            relicOffers.Clear();
            relicOffers.AddRange(RelicOfferGenerator.Roll(
                relicPool,
                relicInventory != null ? relicInventory.OwnedRelics : null,
                relicOfferDisplays.Count,
                previous
            ));

            for (int i = 0; i < relicOfferDisplays.Count; i++)
            {
                RelicShopOfferDisplay display = relicOfferDisplays[i];
                if (display == null)
                    continue;

                RelicData relic = i < relicOffers.Count
                    ? relicOffers[i]
                    : null;
                display.Setup(
                    relic,
                    this,
                    CanAcquireRelic(relic)
                );
                display.PlayEntrance(i * 0.08f);
            }
        }

        public void RefreshRelicOffers(bool avoidCurrent = false)
        {
            GenerateAndPopulateRelics(avoidCurrent);
        }

        public bool TryPurchaseRelic(RelicData relic)
        {
            if (relic == null ||
                gameManager == null ||
                relicInventory == null ||
                !relicOffers.Contains(relic) ||
                relicInventory.Contains(relic))
            {
                return false;
            }

            int price = Mathf.RoundToInt(
                relic.price * MissionRunState.ShopCostMultiplier
            );
            if (gameManager.dinero < price)
                return false;

            if (relicInventory.IsFull)
            {
                if (relicReplacementUI == null ||
                    !relicReplacementUI.IsConfigured)
                {
                    return false;
                }

                pendingRelicPurchase = relic;
                pendingRelicPrice = price;
                SetShopContentVisible(false);
                if (!relicReplacementUI.Open(
                    relic,
                    relicInventory.OwnedRelics,
                    relicToRemove =>
                        ConfirmRelicReplacement(relicToRemove),
                    CancelRelicReplacement
                ))
                {
                    ClearPendingRelicPurchase();
                    SetShopContentVisible(true);
                    return false;
                }

                return true;
            }

            return CompleteRelicPurchase(relic, price, null);
        }

        public bool ConfirmRelicReplacement(RelicData relicToRemove)
        {
            if (pendingRelicPurchase == null ||
                relicToRemove == null ||
                relicInventory == null ||
                !relicInventory.Contains(relicToRemove))
            {
                return false;
            }

            RelicData relic = pendingRelicPurchase;
            int price = pendingRelicPrice;
            bool completed = CompleteRelicPurchase(
                relic,
                price,
                relicToRemove
            );
            if (!completed)
                return false;

            if (relicReplacementUI != null)
                relicReplacementUI.Close();
            ClearPendingRelicPurchase();
            SetShopContentVisible(true);
            return true;
        }

        public void CancelRelicReplacement()
        {
            if (relicReplacementUI != null)
                relicReplacementUI.Close();
            ClearPendingRelicPurchase();
            SetShopContentVisible(true);
        }

        bool CanAcquireRelic(RelicData relic)
        {
            if (relicInventory == null ||
                relic == null ||
                relicInventory.Contains(relic))
            {
                return false;
            }

            return !relicInventory.IsFull ||
                   (relicReplacementUI != null &&
                    relicReplacementUI.IsConfigured);
        }

        bool CompleteRelicPurchase(
            RelicData relic,
            int price,
            RelicData relicToRemove)
        {
            if (relic == null ||
                gameManager == null ||
                relicInventory == null ||
                !relicOffers.Contains(relic) ||
                relicInventory.Contains(relic) ||
                gameManager.dinero < price)
            {
                return false;
            }

            gameManager.dinero -= price;
            bool acquired = relicToRemove == null
                ? relicInventory.TryAdd(relic)
                : relicInventory.TryReplace(relicToRemove, relic);
            if (!acquired)
            {
                gameManager.dinero += price;
                return false;
            }

            int index = relicOffers.IndexOf(relic);
            relicOffers[index] = null;
            if (index >= 0 &&
                index < relicOfferDisplays.Count &&
                relicOfferDisplays[index] != null)
            {
                relicOfferDisplays[index].SetPurchased();
            }

            CollectionProgress.MarkRelicAcquired(relic);
            UpdateDineroUI();
            return true;
        }

        void ClearPendingRelicPurchase()
        {
            pendingRelicPurchase = null;
            pendingRelicPrice = 0;
        }

        void OpenPack(ItemPackOffer offer)
        {
            TryPurchasePack(offer);
        }

        public bool ToggleReservation(ItemPackOffer offer)
        {
            if (offer == null || offer.Claimed || !offers.Contains(offer))
                return false;

            offer.Reserved = !offer.Reserved;
            if (displays.TryGetValue(offer, out ItemPackDisplay display) &&
                display != null)
            {
                display.RefreshReservation();
            }
            return true;
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

            SetShopContentVisible(false);

            if (!packSelectionUI.Open(
                offer,
                CanAcquireItem,
                item => TryClaimItem(offer, item),
                () => CancelPack(offer)))
            {
                gameManager.dinero += price;
                UpdateDineroUI();
                SetShopContentVisible(true);
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
            bool hasCardSelection = source != null &&
                                    source.Count > 0 &&
                                    cardSelectionUI != null &&
                                    cardSelectionUI.IsConfigured;
            if (item.tipoEfecto != TipoEfectoArticulo.DespertarEpifania)
                return hasCardSelection;

            return hasCardSelection &&
                   upgradeSelectionUI != null &&
                   upgradeSelectionUI.IsConfigured;
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

            return OpenCardSelectionForClaim(offer, item);
        }

        bool OpenCardSelectionForClaim(ItemPackOffer offer, ArticuloData item)
        {
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
                        upgradeSelectionUI.Show(selected, () =>
                        {
                            ItemEffectApplier.CompleteSelectedUpgrade(item, battle);
                            CompleteClaim(offer);
                        }, () => packSelectionUI.ShowCurrent()))
                    {
                        return;
                    }

                    if (item.tipoEfecto == TipoEfectoArticulo.DespertarEpifania)
                    {
                        if (upgradeSelectionUI != null &&
                            upgradeSelectionUI.IsConfigured &&
                            upgradeSelectionUI.ShowEpiphanies(selected, () =>
                            {
                                ItemEffectApplier.CompleteSelectedUpgrade(item, battle);
                                CompleteClaim(offer);
                            }, () => packSelectionUI.ShowCurrent()))
                        {
                            return;
                        }

                        packSelectionUI.ShowCurrent();
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
            offer.Reserved = false;
            offer.Claimed = true;
            if (displays.TryGetValue(offer, out ItemPackDisplay display) && display != null)
                display.SetClaimed(true);

            if (packSelectionUI != null)
                packSelectionUI.Close();
            if (shopPanel != null)
                shopPanel.SetActive(true);
            SetShopContentVisible(true);
        }

        void SetShopContentVisible(bool visible)
        {
            for (int i = 0; i < shopContentObjects.Count; i++)
            {
                GameObject contentObject = shopContentObjects[i];
                if (contentObject != null)
                    contentObject.SetActive(visible);
            }
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
            if (subclassSelectionUI != null)
                subclassSelectionUI.Close();
            if (relicReplacementUI != null)
                relicReplacementUI.CloseImmediate();
            ClearPendingRelicPurchase();
            SetShopContentVisible(true);
            if (menusCanvas != null)
                menusCanvas.enabled = false;
            if (pauseTime)
                Time.timeScale = previousTimeScale;
            if (battle != null)
                battle.ContinueAfterShop();
        }

        public int CalculateInterest(int gold)
        {
            int relicBonus = relicInventory != null
                ? relicInventory.GetInterestBonus()
                : 0;
            if (gold <= 0 || goldPerInterestStep <= 0 || interestPerStep <= 0)
                return relicBonus;

            int steps = gold / goldPerInterestStep;
            return Mathf.Min(maxInterest, steps * interestPerStep) +
                   relicBonus;
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
                    if (goldPulse != null)
                        StopCoroutine(goldPulse);
                    goldPulse = StartCoroutine(PulseGold());
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
            goldPulse = null;
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
            SetShopContentVisible(true);
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

            GenerateAndPopulatePacks(true);
            GenerateAndPopulateRelics(true);
            return true;
        }
    }
}
