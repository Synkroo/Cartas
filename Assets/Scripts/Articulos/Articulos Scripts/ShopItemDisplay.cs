using TMPro;
using UnityEngine;
using UnityEngine.UI;
using JuegoDeCartas.Managers;
using JuegoDeCartas.Articulos;

namespace JuegoDeCartas.UI
{
    public class ShopItemDisplay : MonoBehaviour
    {
        ArticuloData item;
        ShopManager shopManager;
        BattleManager battle;
        Button button;

        static readonly Color colorComun = new Color(0f, 0.5f, 1f);
        static readonly Color colorRaro = new Color(0f, 0.7f, 0f);
        static readonly Color colorEpico = new Color(0.6f, 0f, 0.7f);
        static readonly Color colorFondoComun = new Color(0f, 0.25f, 0.5f, 0.5f);
        static readonly Color colorFondoRaro = new Color(0f, 0.3f, 0f, 0.5f);
        static readonly Color colorFondoEpico = new Color(0.25f, 0f, 0.3f, 0.5f);

        void Awake()
        {
            button = GetComponent<Button>();
            if (button != null)
                button.onClick.AddListener(OnBuy);
        }

        public void Setup(ArticuloData newItem, ShopManager manager, BattleManager battleManager)
        {
            item = newItem;
            shopManager = manager;
            battle = battleManager;

            var nombreText = transform.Find("Nombre/Text (TMP)")?.GetComponent<TextMeshProUGUI>();
            var precioText = transform.Find("Precio/Text (TMP)")?.GetComponent<TextMeshProUGUI>();
            var rarezaText = transform.Find("Rareza/Text (TMP)")?.GetComponent<TextMeshProUGUI>();
            var imagen = transform.Find("Imagen/Marco/Fondo/Sprite")?.GetComponent<Image>();
            var fondo = transform.Find("Panel")?.GetComponent<Image>();

            if (nombreText != null && item != null)
                nombreText.text = item.nombre;

            if (precioText != null && item != null)
                precioText.text = GetRarezaCost(item.rareza) + "€";

            if (rarezaText != null && item != null)
            {
                rarezaText.text = GetRarezaLabel(item.rareza);
                rarezaText.color = GetRarezaColor(item.rareza);
            }

            if (imagen != null && item != null && item.imagen != null)
                imagen.sprite = item.imagen;

            if (fondo != null && item != null)
                fondo.color = GetFondoColor(item.rareza);

            if (button != null)
                button.interactable = true;
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

            int cost = GetRarezaCost(item.rareza);
            if (battle.gameManager.dinero < cost) return;

            battle.gameManager.dinero -= cost;
            shopManager.UpdateDineroUI();

            AplicarEfecto();

            if (button != null)
                button.interactable = false;

            gameObject.SetActive(false);
        }

        void AplicarEfecto()
        {
            if (battle.player == null) return;

            switch (item.tipoEfecto)
            {
                case TipoEfectoArticulo.CurarVida:
                    battle.player.stats.health += item.cantidad;
                    if (battle.player.stats.health > battle.player.stats.maxHealth)
                        battle.player.stats.health = battle.player.stats.maxHealth;
                    break;

                case TipoEfectoArticulo.DarArmadura:
                    battle.player.stats.armor += item.cantidad;
                    break;

                case TipoEfectoArticulo.AumentarRobo:
                    battle.deckManager.cardsPerTurn += item.cantidad;
                    break;

                case TipoEfectoArticulo.AumentarVidaMax:
                    battle.player.stats.maxHealth += item.cantidad;
                    battle.player.stats.health += item.cantidad;
                    break;

                case TipoEfectoArticulo.AumentarManaMax:
                    battle.player.stats.maxMana += item.cantidad;
                    break;

                case TipoEfectoArticulo.AumentarMano:
                    battle.deckManager.cardsPerTurn =
                        Mathf.Min(battle.deckManager.cardsPerTurn + item.cantidad, 8);
                    break;

                case TipoEfectoArticulo.ArmaduraPorTurno:
                    battle.armorPerTurn += item.cantidad;
                    break;

                case TipoEfectoArticulo.RegeneracionVida:
                    battle.regenPerRound += item.cantidad;
                    break;
            }

            battle.UpdateUI();
        }
    }
}
