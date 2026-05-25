using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using JuegoDeCartas.Managers;
using JuegoDeCartas.Cards;
using JuegoDeCartas.Articulos;

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

        static readonly Color colorComun = new Color(0f, 0.5f, 1f);
        static readonly Color colorRaro = new Color(0f, 0.7f, 0f);
        static readonly Color colorEpico = new Color(0.6f, 0f, 0.7f);
        static readonly Color colorFondoComun = new Color(0f, 0.25f, 0.5f, 0.5f);
        static readonly Color colorFondoRaro = new Color(0f, 0.3f, 0f, 0.5f);
        static readonly Color colorFondoEpico = new Color(0.25f, 0f, 0.3f, 0.5f);

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
                precioText.text = GetRarezaCost(item.rareza) + "€";

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
            return r switch
            {
                Rareza.Comun => 300,
                Rareza.Raro => 500,
                Rareza.Epico => 1000,
                _ => 300
            };
        }

        Color GetRarezaColor(Rareza r)
        {
            return r switch
            {
                Rareza.Comun => colorComun,
                Rareza.Raro => colorRaro,
                Rareza.Epico => colorEpico,
                _ => colorComun
            };
        }

        Color GetFondoColor(Rareza r)
        {
            return r switch
            {
                Rareza.Comun => colorFondoComun,
                Rareza.Raro => colorFondoRaro,
                Rareza.Epico => colorFondoEpico,
                _ => colorFondoComun
            };
        }

        string GetRarezaLabel(Rareza r)
        {
            return r switch
            {
                Rareza.Comun => "Comun",
                Rareza.Raro => "Raro",
                Rareza.Epico => "Epico",
                _ => ""
            };
        }

        public void OnBuy()
        {
            if (item == null || shopManager == null || battle == null) return;

            if (ItemEffectApplier.NeedsSelection(item.tipoEfecto))
            {
                if (selectionUI == null) return;

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
                selectionUI.OpenForSelection(source, item.descripcion,
                    (selected) =>
                    {
                        ItemEffectApplier.ApplyToSelected(capturedItem, capturedBattle, selected);
                        if (capturedButton != null)
                            capturedButton.interactable = false;
                        capturedGO.SetActive(false);
                    },
                    () =>
                    {
                        capturedBattle.gameManager.dinero += spent;
                        if (shopManager != null)
                            shopManager.UpdateDineroUI();
                        if (capturedButton != null)
                            capturedButton.interactable = true;
                    }
                );
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
    }
}
