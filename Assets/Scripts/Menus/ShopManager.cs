using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using JuegoDeCartas.Managers;

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
        }

        public void Open()
        {
            if (pauseTime)
                Time.timeScale = 0f;

            shopPanel.SetActive(true);

            SetActiveAndBlockOthers();

            if (menusCanvas != null)
                menusCanvas.enabled = true;

            if (menusRaycaster != null)
                menusRaycaster.enabled = true;

            UpdateDineroUI();
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
    }
}
