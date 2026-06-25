using System.Collections.Generic;
using UnityEngine;
using JuegoDeCartas.Managers;
using JuegoDeCartas.Cards;
using JuegoDeCartas.Progression;
using JuegoDeCartas.Challenges;

namespace JuegoDeCartas.Articulos
{
    public static class ItemEffectApplier
    {
        public static void Apply(ArticuloData item, BattleManager battle)
        {
            if (item == null || battle == null || battle.player == null || battle.deckManager == null)
                return;

            CollectionProgress.MarkItemUsed(item);

            switch (item.tipoEfecto)
            {
                case TipoEfectoArticulo.CurarVida:
                    battle.player.stats.health += item.cantidad;
                    if (battle.player.stats.health > battle.player.stats.maxHealth)
                        battle.player.stats.health = battle.player.stats.maxHealth;
                    battle.player.stats.Clamp();
                    break;

                case TipoEfectoArticulo.DarArmadura:
                    battle.GainPlayerArmor(item.cantidad);
                    break;

                case TipoEfectoArticulo.AumentarRobo:
                    battle.deckManager.cardsPerTurn += item.cantidad;
                    battle.deckManager.cardsPerTurn = Mathf.Max(0, battle.deckManager.cardsPerTurn);
                    break;

                case TipoEfectoArticulo.AumentarVidaMax:
                    battle.player.stats.maxHealth += item.cantidad;
                    battle.player.stats.health += item.cantidad;
                    battle.player.stats.Clamp();
                    break;

                case TipoEfectoArticulo.AumentarManaMax:
                    battle.player.stats.maxMana += item.cantidad;
                    battle.player.stats.Clamp();
                    break;

                case TipoEfectoArticulo.AumentarMano:
                    battle.deckManager.cardsPerTurn =
                        Mathf.Clamp(battle.deckManager.cardsPerTurn + item.cantidad, 0, 8);
                    break;

                case TipoEfectoArticulo.ArmaduraPorTurno:
                    battle.armorPerTurn += item.cantidad;
                    break;

                case TipoEfectoArticulo.RegeneracionVida:
                    battle.regenPerRound += item.cantidad;
                    break;

                case TipoEfectoArticulo.AgregarCartaAleatoria:
                {
                    List<CardData> pool = GetValidCardDataPool(battle);
                    if (pool.Count == 0)
                        break;

                    for (int i = 0; i < item.cantidad; i++)
                    {
                        var rand = pool[RunRandom.Range(0, pool.Count)];
                        battle.deckManager.hand.Add(new Card(rand));
                    }
                    battle.RenderHand();
                    battle.deckManager.OnDeckChanged?.Invoke();
                    break;
                }

                case TipoEfectoArticulo.MejorarCarta:
                {
                    var candidates = GetSelectionSource(item, battle);
                    int count = Mathf.Min(item.cantidad, candidates.Count);
                    for (int i = 0; i < count; i++)
                    {
                        var card = candidates[RunRandom.Range(0, candidates.Count)];
                        candidates.Remove(card);
                        card.ReduceCost();
                    }
                    battle.RenderHand();
                    battle.deckManager.OnDeckChanged?.Invoke();
                    break;
                }

                case TipoEfectoArticulo.DuplicarCarta:
                case TipoEfectoArticulo.DuplicarCartaMejoras:
                {
                    var candidates = GetSelectionSource(item, battle);
                    int count = Mathf.Min(item.cantidad, candidates.Count);
                    for (int i = 0; i < count; i++)
                    {
                        var source = candidates[RunRandom.Range(0, candidates.Count)];
                        battle.deckManager.hand.Add(new Card(
                            source,
                            item.tipoEfecto == TipoEfectoArticulo.DuplicarCartaMejoras
                        ));
                    }
                    battle.RenderHand();
                    battle.deckManager.OnDeckChanged?.Invoke();
                    break;
                }

                case TipoEfectoArticulo.ReducirCoste:
                {
                    List<Card> cards = GetSelectionSource(item, battle);
                    int count = Mathf.Min(item.cantidad, cards.Count);
                    for (int i = 0; i < count; i++)
                    {
                        Card card = cards[RunRandom.Range(0, cards.Count)];
                        cards.Remove(card);
                        card.ReduceCost();
                    }
                    battle.RenderHand();
                    battle.deckManager.OnDeckChanged?.Invoke();
                    break;
                }
            }

            battle.UpdateUI();
        }

        public static bool NeedsSelection(TipoEfectoArticulo tipo)
        {
            return tipo == TipoEfectoArticulo.AgregarCartaEleccion
                || tipo == TipoEfectoArticulo.AgregarCartaOtraClase
                || tipo == TipoEfectoArticulo.MejorarCarta
                || tipo == TipoEfectoArticulo.DuplicarCarta
                || tipo == TipoEfectoArticulo.DuplicarCartaMejoras
                || tipo == TipoEfectoArticulo.ReducirCoste
                || tipo == TipoEfectoArticulo.DescartarCarta
                || tipo == TipoEfectoArticulo.DespertarEpifania;
        }

        public static List<Card> GetSelectionSource(ArticuloData item, BattleManager battle)
        {
            if (item == null || battle == null || battle.deckManager == null)
                return new List<Card>();

            if (item.tipoEfecto == TipoEfectoArticulo.AgregarCartaEleccion)
                return BuildUniqueCardChoices(GetValidCardDataPool(battle));

            if (item.tipoEfecto == TipoEfectoArticulo.AgregarCartaOtraClase)
                return BuildUniqueCardChoices(GetExternalCardDataPool(item, battle));

            var dm = battle.deckManager;
            var all = new List<Card>(dm.deck.Count + dm.hand.Count + dm.discard.Count);
            all.AddRange(dm.deck);
            all.AddRange(dm.hand);
            all.AddRange(dm.discard);
            all.RemoveAll(c => c == null || c.data == null);

            var tipo = item.tipoEfecto;

            if (tipo == TipoEfectoArticulo.MejorarCarta)
                all.RemoveAll(c => c.selectedUpgradeIndex >= 0);

            if (tipo == TipoEfectoArticulo.DespertarEpifania)
            {
                all.RemoveAll(c =>
                    c.epiphanyUnlocked ||
                    c.data.GetEpiphanyOptions().Count == 0
                );
            }

            if (tipo == TipoEfectoArticulo.ReducirCoste)
                all.RemoveAll(c => c.effectiveCost <= 0);

            return all;
        }

        public static void ApplyToSelected(ArticuloData item, BattleManager battle, Card selected)
        {
            if (item == null || battle == null || battle.deckManager == null || selected == null)
                return;

            CollectionProgress.MarkItemUsed(item);

            switch (item.tipoEfecto)
            {
                case TipoEfectoArticulo.AgregarCartaEleccion:
                case TipoEfectoArticulo.AgregarCartaOtraClase:
                    battle.deckManager.hand.Add(new Card(selected.data));
                    battle.RenderHand();
                    battle.deckManager.OnDeckChanged?.Invoke();
                    break;

                case TipoEfectoArticulo.DespertarEpifania:
                    if (selected.ApplyEpiphany())
                    {
                        battle.RenderHand();
                        battle.deckManager.OnDeckChanged?.Invoke();
                    }
                    break;

                case TipoEfectoArticulo.MejorarCarta:
                {
                    selected.ReduceCost(item.cantidad);
                    battle.RenderHand();
                    battle.deckManager.OnDeckChanged?.Invoke();
                    break;
                }

                case TipoEfectoArticulo.DuplicarCarta:
                case TipoEfectoArticulo.DuplicarCartaMejoras:
                {
                    int count = Mathf.Max(1, item.cantidad);
                    for (int i = 0; i < count; i++)
                    {
                        battle.deckManager.hand.Add(new Card(
                            selected,
                            item.tipoEfecto == TipoEfectoArticulo.DuplicarCartaMejoras
                        ));
                    }
                    battle.RenderHand();
                    battle.deckManager.OnDeckChanged?.Invoke();
                    break;
                }

                case TipoEfectoArticulo.ReducirCoste:
                    selected.ReduceCost(item.cantidad);
                    battle.RenderHand();
                    battle.deckManager.OnDeckChanged?.Invoke();
                    break;

                case TipoEfectoArticulo.DescartarCarta:
                    RemoveSelectedCard(battle, selected);
                    break;
            }

            battle.UpdateUI();
        }

        public static void CompleteSelectedUpgrade(ArticuloData item, BattleManager battle)
        {
            if (item == null || battle == null || battle.deckManager == null)
                return;

            CollectionProgress.MarkItemUsed(item);
            battle.RenderHand();
            battle.deckManager.OnDeckChanged?.Invoke();
            battle.UpdateUI();
        }

        static void RemoveSelectedCard(BattleManager battle, Card selected)
        {
            if (battle == null || selected == null)
                return;

            var deckManager = battle.deckManager;

            if (deckManager.hand.Remove(selected) ||
                deckManager.deck.Remove(selected) ||
                deckManager.discard.Remove(selected))
            {
                battle.RenderHand();
                deckManager.OnDeckChanged?.Invoke();
            }
        }

        static List<CardData> GetValidCardDataPool(BattleManager battle)
        {
            var pool = new List<CardData>();
            if (battle == null || battle.deckManager == null || battle.deckManager.startingDeck == null)
                return pool;

            for (int i = 0; i < battle.deckManager.startingDeck.Count; i++)
            {
                CardData cardData = battle.deckManager.startingDeck[i];
                if (cardData != null)
                    pool.Add(cardData);
            }

            return pool;
        }

        static List<CardData> GetExternalCardDataPool(
            ArticuloData item,
            BattleManager battle)
        {
            var pool = new List<CardData>();
            if (item == null || item.cardPool == null)
                return pool;

            var ownCards = new HashSet<CardData>();
            if (battle != null &&
                battle.deckManager != null &&
                battle.deckManager.startingDeck != null)
            {
                for (int i = 0; i < battle.deckManager.startingDeck.Count; i++)
                {
                    CardData ownCard = battle.deckManager.startingDeck[i];
                    if (ownCard != null)
                        ownCards.Add(ownCard);
                }
            }

            for (int i = 0; i < item.cardPool.Count; i++)
            {
                CardData candidate = item.cardPool[i];
                if (candidate != null &&
                    !ownCards.Contains(candidate) &&
                    !pool.Contains(candidate))
                {
                    pool.Add(candidate);
                }
            }

            return pool;
        }

        static List<Card> BuildUniqueCardChoices(List<CardData> cardDataPool)
        {
            var choices = new List<Card>();
            var seen = new HashSet<CardData>();

            for (int i = 0; i < cardDataPool.Count; i++)
            {
                CardData cardData = cardDataPool[i];
                if (cardData != null && seen.Add(cardData))
                    choices.Add(new Card(cardData));
            }

            return choices;
        }
    }
}
