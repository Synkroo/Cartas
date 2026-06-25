using JuegoDeCartas.Articulos;
using JuegoDeCartas.Cards;
using JuegoDeCartas.Managers;
using JuegoDeCartas.Missions;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace JuegoDeCartas.UI
{
    public class ShopItemDisplay : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        ArticuloData item;
        ShopManager shopManager;
        BattleManager battle;
        Button button;
        Vector3 originalScale;

        [Header("Selection")]
        public CardSelectionUI selectionUI;

        [Header("UI References")]
        public TextMeshProUGUI nombreText;
        public TextMeshProUGUI precioText;
        public TextMeshProUGUI rarezaText;
        public TextMeshProUGUI descripcionText;
        public Image imagenSprite;
        public Image fondoPanel;

        [Header("Costs")]
        public int commonCost = 200;
        public int rareCost = 400;
        public int epicCost = 800;
        public int fallbackCost = 300;
        public string currencySuffix = "\u20ac";

        [Header("Rarity Text")]
        public string commonLabel = "Comun";
        public string rareLabel = "Raro";
        public string epicLabel = "Epico";

        [Header("Rarity Colors")]
        public Color commonColor = new Color(0f, 0.5f, 1f);
        public Color rareColor = new Color(0f, 0.7f, 0f);
        public Color epicColor = new Color(0.6f, 0f, 0.7f);
        public Color commonBackgroundColor = new Color(0f, 0.25f, 0.5f, 0.5f);
        public Color rareBackgroundColor = new Color(0f, 0.3f, 0f, 0.5f);
        public Color epicBackgroundColor = new Color(0.25f, 0f, 0.3f, 0.5f);

        Coroutine hoverRoutine;

        void Awake()
        {
            button = GetComponent<Button>();
            if (button != null)
                button.onClick.AddListener(OnBuy);
            originalScale = transform.localScale;
        }

        public void Setup(ArticuloData newItem, ShopManager manager, BattleManager battleManager, CardSelectionUI cardSelUI = null)
        {
            item = newItem;
            shopManager = manager;
            battle = battleManager;
            if (cardSelUI != null) selectionUI = cardSelUI;

            if (item == null) return;

            ResolveUIReferences();

            if (nombreText != null)
                nombreText.text = item.nombre;

            if (precioText != null)
                precioText.text = GetRarezaCost(item.rareza) + currencySuffix;

            if (rarezaText != null)
            {
                rarezaText.text = GetRarezaLabel(item.rareza);
                rarezaText.color = GetRarezaColor(item.rareza);
            }

            if (imagenSprite != null && item.imagen != null)
                imagenSprite.sprite = item.imagen;

            if (fondoPanel != null)
                fondoPanel.color = GetFondoColor(item.rareza);

            if (descripcionText != null)
                descripcionText.text = item.descripcion;

            if (button != null)
                button.interactable = true;
        }

        void ResolveUIReferences()
        {
            if (nombreText == null)
                nombreText = transform.Find("Nombre/Text (TMP)")?.GetComponent<TextMeshProUGUI>();
            if (precioText == null)
                precioText = transform.Find("Precio/Text (TMP)")?.GetComponent<TextMeshProUGUI>();
            if (rarezaText == null)
                rarezaText = transform.Find("Rareza/Text (TMP)")?.GetComponent<TextMeshProUGUI>();
            if (descripcionText == null)
                descripcionText = transform.Find("Descripcion/Text (TMP)")?.GetComponent<TextMeshProUGUI>();
            if (imagenSprite == null)
                imagenSprite = transform.Find("Imagen/Marco/Fondo/Sprite")?.GetComponent<Image>();
            if (fondoPanel == null)
                fondoPanel = transform.Find("Panel")?.GetComponent<Image>();
        }

        void StartHover()
        {
            if (hoverRoutine != null)
                StopCoroutine(hoverRoutine);
            hoverRoutine = StartCoroutine(AnimateScale(1.06f));
        }

        void StopHover()
        {
            if (hoverRoutine != null)
                StopCoroutine(hoverRoutine);
            hoverRoutine = StartCoroutine(AnimateScale(1f));
        }

        IEnumerator AnimateScale(float multiplier)
        {
            float start = transform.localScale.x;
            float target = originalScale.x * multiplier;
            float dur = 0.15f;
            float t = 0;

            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                float s = Mathf.Lerp(start, target, t / dur);
                transform.localScale = Vector3.one * s;
                yield return null;
            }

            transform.localScale = Vector3.one * target;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            StartHover();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            StopHover();
        }

        int GetRarezaCost(Rareza r)
        {
            int baseCost = r switch
            {
                Rareza.Comun => commonCost,
                Rareza.Raro => rareCost,
                Rareza.Epico => epicCost,
                _ => fallbackCost
            };

            return Mathf.RoundToInt(baseCost * MissionRunState.ShopCostMultiplier);
        }

        Color GetRarezaColor(Rareza r)
        {
            return r switch
            {
                Rareza.Comun => commonColor,
                Rareza.Raro => rareColor,
                Rareza.Epico => epicColor,
                _ => commonColor
            };
        }

        Color GetFondoColor(Rareza r)
        {
            return r switch
            {
                Rareza.Comun => commonBackgroundColor,
                Rareza.Raro => rareBackgroundColor,
                Rareza.Epico => epicBackgroundColor,
                _ => commonBackgroundColor
            };
        }

        string GetRarezaLabel(Rareza r)
        {
            return r switch
            {
                Rareza.Comun => commonLabel,
                Rareza.Raro => rareLabel,
                Rareza.Epico => epicLabel,
                _ => ""
            };
        }

        public void OnBuy()
        {
            if (item == null || shopManager == null || battle == null || battle.gameManager == null) return;

            if (ItemEffectApplier.NeedsSelection(item.tipoEfecto))
            {
                if (selectionUI == null || !selectionUI.IsConfigured) return;

                List<Card> source = ItemEffectApplier.GetSelectionSource(item, battle);
                if (source == null || source.Count == 0) return;

                int cost = GetRarezaCost(item.rareza);
                if (battle.gameManager.dinero < cost) return;

                battle.gameManager.dinero -= cost;
                shopManager.UpdateDineroUI();

                int spent = cost;
                var capturedItem = item;
                var capturedBattle = battle;
                var capturedButton = button;
                var capturedGO = gameObject;

                if (item.tipoEfecto == TipoEfectoArticulo.MejorarCarta)
                {
                    selectionUI.OpenForSelection(source, item.descripcion,
                        (selected) =>
                        {
                            if (selected == null)
                            {
                                Refund(capturedBattle, shopManager, spent);
                                if (capturedButton != null)
                                    capturedButton.interactable = true;
                                return;
                            }

                            var upgradeUI = shopManager.upgradeSelectionUI;
                            if (upgradeUI != null && upgradeUI.IsConfigured && upgradeUI.Show(selected, () =>
                            {
                                ItemEffectApplier.CompleteSelectedUpgrade(capturedItem, capturedBattle);
                                if (capturedButton != null)
                                    capturedButton.interactable = false;
                                capturedGO.SetActive(false);
                            }, () =>
                            {
                                Refund(capturedBattle, shopManager, spent);
                                if (capturedButton != null)
                                    capturedButton.interactable = true;
                            }))
                            {
                                return;
                            }

                            ItemEffectApplier.ApplyToSelected(capturedItem, capturedBattle, selected);
                            if (capturedButton != null)
                                capturedButton.interactable = false;
                            capturedGO.SetActive(false);
                        },
                        () =>
                        {
                            Refund(capturedBattle, shopManager, spent);
                            if (capturedButton != null)
                                capturedButton.interactable = true;
                        }
                    );
                }
                else
                {
                    selectionUI.OpenForSelection(source, item.descripcion,
                        (selected) =>
                        {
                            if (selected == null)
                            {
                                Refund(capturedBattle, shopManager, spent);
                                if (capturedButton != null)
                                    capturedButton.interactable = true;
                                return;
                            }

                            ItemEffectApplier.ApplyToSelected(capturedItem, capturedBattle, selected);
                            if (capturedButton != null)
                                capturedButton.interactable = false;
                            capturedGO.SetActive(false);
                        },
                        () =>
                        {
                            Refund(capturedBattle, shopManager, spent);
                            if (capturedButton != null)
                                capturedButton.interactable = true;
                        }
                    );
                }
            }
            else
            {
                int cost = GetRarezaCost(item.rareza);
                if (battle.gameManager.dinero < cost) return;

                battle.gameManager.dinero -= cost;
                shopManager.UpdateDineroUI();

                ItemEffectApplier.Apply(item, battle);

                if (button != null)
                    button.interactable = false;

                gameObject.SetActive(false);
            }
        }

        static void Refund(BattleManager capturedBattle, ShopManager shopManager, int spent)
        {
            if (capturedBattle != null && capturedBattle.gameManager != null)
                capturedBattle.gameManager.dinero += spent;

            if (shopManager != null)
                shopManager.UpdateDineroUI();
        }
    }
}
