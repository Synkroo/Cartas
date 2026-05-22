using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using JuegoDeCartas.Managers;
using JuegoDeCartas.Articulos;

namespace JuegoDeCartas.UI
{
    public class ShopManager : MonoBehaviour
    {
        [Header("Shop Panel")]
        public GameObject shopPanel;

        [Header("UI References")]
        public TextMeshProUGUI dineroText;

        [Header("Canvas")]
        public Canvas menusCanvas;

        [Header("Settings")]
        public bool pauseTime = true;

        [Header("External References")]
        public DeckViewerUI deckViewer;

        public BattleManager battle;

        public GameManager gameManager;

        [Header("Item Pool")]
        public List<ArticuloData> itemPool = new List<ArticuloData>();

        [Header("Slots")]
        public Transform[] slotContainers = new Transform[3];
        public GameObject itemCardPrefab;

        static readonly float[] rarityWeights = { 0.50f, 0.35f, 0.15f };

        List<GameObject> spawnedItems = new List<GameObject>();
        GraphicRaycaster menusRaycaster;
        List<GraphicRaycaster> disabledRaycasters = new List<GraphicRaycaster>();

        void Awake()
        {
            if (menusCanvas != null)
                menusRaycaster = menusCanvas.GetComponent<GraphicRaycaster>()
                                 ?? menusCanvas.gameObject.AddComponent<GraphicRaycaster>();
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
            if (pauseTime)
                Time.timeScale = 0f;

            ClearSlots();
            PopulateSlots();

            shopPanel.SetActive(true);

            SetActiveAndBlockOthers();

            if (menusCanvas != null)
                menusCanvas.enabled = true;

            if (menusRaycaster != null)
                menusRaycaster.enabled = true;

            UpdateDineroUI();
        }

        void ClearSlots()
        {
            foreach (var go in spawnedItems)
                if (go != null) Destroy(go);
            spawnedItems.Clear();
        }

        void PopulateSlots()
        {
            if (itemCardPrefab == null) return;

            for (int i = 0; i < slotContainers.Length; i++)
            {
                if (slotContainers[i] == null) continue;

                ArticuloData selected = RollItem();

                GameObject itemGO = Instantiate(itemCardPrefab, slotContainers[i]);
                itemGO.transform.localPosition = Vector3.zero;
                itemGO.transform.localScale = Vector3.one;

                var display = itemGO.GetComponent<ShopItemDisplay>();
                if (display != null)
                    display.Setup(selected, this, battle);

                spawnedItems.Add(itemGO);
            }
        }

        ArticuloData RollItem()
        {
            if (itemPool.Count == 0) return null;

            Rareza rolledRarity = RollRarity();
            var candidates = itemPool.FindAll(a => a.rareza == rolledRarity);

            if (candidates.Count == 0)
            {
                for (Rareza fallback = rolledRarity - 1; fallback >= Rareza.Comun; fallback--)
                {
                    candidates = itemPool.FindAll(a => a.rareza == fallback);
                    if (candidates.Count > 0) break;
                }
            }

            if (candidates.Count == 0)
                candidates = itemPool;

            return candidates[Random.Range(0, candidates.Count)];
        }

        Rareza RollRarity()
        {
            float roll = Random.value;
            float cumulative = 0f;

            for (int i = 0; i < rarityWeights.Length; i++)
            {
                cumulative += rarityWeights[i];
                if (roll < cumulative)
                    return (Rareza)i;
            }

            return Rareza.Comun;
        }

        void SetActiveAndBlockOthers()
        {
            var all = FindObjectsByType<GraphicRaycaster>(FindObjectsSortMode.None);
            disabledRaycasters.Clear();
            foreach (var rc in all)
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
            foreach (var rc in disabledRaycasters)
                if (rc != null) rc.enabled = true;
            disabledRaycasters.Clear();

            shopPanel.SetActive(false);

            if (menusCanvas != null)
                menusCanvas.enabled = false;

            Time.timeScale = 1f;

            if (battle != null)
                battle.ContinueAfterShop();
        }

        public void UpdateDineroUI()
        {
            if (dineroText != null && gameManager != null)
                dineroText.text = gameManager.dinero.ToString() + "€";
        }

        public void OnSalir()
        {
            Close();
        }

        public void OnVerMazo()
        {
            if (deckViewer == null) return;

            shopPanel.SetActive(false);

            deckViewer.onClose -= OnDeckViewerClosed;
            deckViewer.onClose += OnDeckViewerClosed;
            deckViewer.Open();
        }

        void OnDeckViewerClosed()
        {
            deckViewer.onClose -= OnDeckViewerClosed;
            shopPanel.SetActive(true);
        }

        public void OnRestock()
        {
            var precioText = transform.Find("Cabecero/PanelRestock/Precio200")?.GetComponent<TextMeshProUGUI>();
            if (precioText == null || gameManager == null) return;

            string numeric = System.Text.RegularExpressions.Regex.Replace(precioText.text, @"[^\d]", "");
            if (!int.TryParse(numeric, out int cost)) return;

            if (gameManager.dinero < cost) return;

            gameManager.dinero -= cost;
            UpdateDineroUI();

            ClearSlots();
            PopulateSlots();
        }
    }
}
