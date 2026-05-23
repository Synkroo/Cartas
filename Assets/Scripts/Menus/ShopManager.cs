using System.Collections;
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

        [Header("Restock")]
        public int restockCost = 200;

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

            var restockPrecioText = transform.Find("Cabecero/PanelRestock/Precio200")?.GetComponent<TextMeshProUGUI>();
            if (restockPrecioText != null)
                restockPrecioText.text = restockCost + "€";
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
                if (selected == null) continue;

                GameObject itemGO = Instantiate(itemCardPrefab, slotContainers[i]);
                itemGO.transform.localPosition = Vector3.zero;

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

        int lastDinero = -1;

        public void UpdateDineroUI()
        {
            if (dineroText != null && gameManager != null)
            {
                dineroText.text = gameManager.dinero.ToString() + "€";
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
            if (gameManager == null) return;
            if (gameManager.dinero < restockCost) return;

            gameManager.dinero -= restockCost;
            UpdateDineroUI();

            ClearSlots();
            PopulateSlots();
        }
    }
}
