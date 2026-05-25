using System.Collections.Generic;
using UnityEngine;
using JuegoDeCartas.Managers;
using JuegoDeCartas.Cards;

namespace JuegoDeCartas.Articulos
{
    public static class ItemEffectApplier
    {
        public static void Apply(ArticuloData item, BattleManager battle)
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

                case TipoEfectoArticulo.AgregarCartaAleatoria:
                {
                    var pool = battle.deckManager.startingDeck;
                    for (int i = 0; i < item.cantidad; i++)
                    {
                        var rand = pool[Random.Range(0, pool.Count)];
                        battle.deckManager.hand.Add(new Card(rand));
                    }
                    battle.RenderHand();
                    break;
                }

                case TipoEfectoArticulo.AgregarCartaEleccion:
                {
                    var pool = battle.deckManager.startingDeck;
                    for (int i = 0; i < item.cantidad; i++)
                    {
                        var rand = pool[Random.Range(0, pool.Count)];
                        battle.deckManager.hand.Add(new Card(rand));
                    }
                    battle.RenderHand();
                    break;
                }

                case TipoEfectoArticulo.MejorarCarta:
                {
                    var hand = battle.deckManager.hand;
                    int count = Mathf.Min(item.cantidad, hand.Count);
                    for (int i = 0; i < count; i++)
                    {
                        var card = hand[Random.Range(0, hand.Count)];
                        card.data.cost = Mathf.Max(0, card.data.cost - 1);
                    }
                    battle.RenderHand();
                    break;
                }

                case TipoEfectoArticulo.DuplicarCarta:
                case TipoEfectoArticulo.DuplicarCartaMejoras:
                {
                    var hand = battle.deckManager.hand;
                    int count = Mathf.Min(item.cantidad, hand.Count);
                    for (int i = 0; i < count; i++)
                    {
                        var source = hand[Random.Range(0, hand.Count)];
                        battle.deckManager.hand.Add(new Card(source.data));
                    }
                    battle.RenderHand();
                    break;
                }

                case TipoEfectoArticulo.ReducirCoste:
                {
                    var hand = battle.deckManager.hand;
                    if (hand.Count == 0) break;
                    var card = hand[Random.Range(0, hand.Count)];
                    card.data.cost = Mathf.Max(0, card.data.cost - item.cantidad);
                    battle.RenderHand();
                    break;
                }
            }

            battle.UpdateUI();
        }

        public static bool NeedsSelection(TipoEfectoArticulo tipo)
        {
            return tipo == TipoEfectoArticulo.AgregarCartaEleccion
                || tipo == TipoEfectoArticulo.MejorarCarta
                || tipo == TipoEfectoArticulo.DuplicarCarta
                || tipo == TipoEfectoArticulo.DuplicarCartaMejoras
                || tipo == TipoEfectoArticulo.ReducirCoste;
        }

        public static List<Card> GetSelectionSource(ArticuloData item, BattleManager battle)
        {
            var dm = battle.deckManager;
            var all = new List<Card>(dm.deck.Count + dm.hand.Count + dm.discard.Count);
            all.AddRange(dm.deck);
            all.AddRange(dm.hand);
            all.AddRange(dm.discard);
            return all;
        }

        public static void ApplyToSelected(ArticuloData item, BattleManager battle, Card selected)
        {
            switch (item.tipoEfecto)
            {
                case TipoEfectoArticulo.AgregarCartaEleccion:
                    battle.deckManager.hand.Add(new Card(selected.data));
                    battle.RenderHand();
                    break;

                case TipoEfectoArticulo.MejorarCarta:
                {
                    int count = item.cantidad;
                    selected.data.cost = Mathf.Max(0, selected.data.cost - count);
                    battle.RenderHand();
                    break;
                }

                case TipoEfectoArticulo.DuplicarCarta:
                case TipoEfectoArticulo.DuplicarCartaMejoras:
                {
                    int count = Mathf.Min(item.cantidad, battle.deckManager.hand.Count);
                    for (int i = 0; i < count; i++)
                        battle.deckManager.hand.Add(new Card(selected.data));
                    battle.RenderHand();
                    break;
                }

                case TipoEfectoArticulo.ReducirCoste:
                    selected.data.cost = Mathf.Max(0, selected.data.cost - item.cantidad);
                    battle.RenderHand();
                    break;
            }

            battle.UpdateUI();
        }
    }
}
