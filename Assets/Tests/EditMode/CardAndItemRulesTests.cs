using System.Collections.Generic;
using JuegoDeCartas.Articulos;
using JuegoDeCartas.Cards;
using JuegoDeCartas.Managers;
using NUnit.Framework;
using UnityEngine;

namespace JuegoDeCartas.Tests
{
    public class CardAndItemRulesTests
    {
        [Test]
        public void EffectiveCostNeverDropsBelowZero()
        {
            CardData data = ScriptableObject.CreateInstance<CardData>();
            data.cost = 1;

            Card card = new Card(data);
            card.ReduceCost(5);

            Assert.AreEqual(0, card.effectiveCost);
            Assert.AreEqual(1, card.costReduction);

            Object.DestroyImmediate(data);
        }

        [Test]
        public void CardCopyCanPreserveOrDiscardUpgrades()
        {
            CardData data = ScriptableObject.CreateInstance<CardData>();
            data.cost = 3;

            Card upgraded = new Card(data);
            upgraded.ReduceCost();
            upgraded.AddReactivation();

            Card copiedWithUpgrades = new Card(upgraded, true);
            Card copiedWithoutUpgrades = new Card(upgraded, false);

            Assert.AreEqual(2, copiedWithUpgrades.effectiveCost);
            Assert.AreEqual(1, copiedWithUpgrades.reactivationCount);
            Assert.IsTrue(copiedWithUpgrades.upgraded);

            Assert.AreEqual(3, copiedWithoutUpgrades.effectiveCost);
            Assert.AreEqual(0, copiedWithoutUpgrades.reactivationCount);
            Assert.IsFalse(copiedWithoutUpgrades.upgraded);

            Object.DestroyImmediate(data);
        }

        [Test]
        public void EntityDamageConsumesArmorBeforeHealth()
        {
            Entity entity = new Entity();
            entity.stats.maxHealth = 20;
            entity.stats.health = 20;
            entity.stats.armor = 5;

            int dealt = entity.TakeDamage(8);

            Assert.AreEqual(3, dealt);
            Assert.AreEqual(17, entity.stats.health);
            Assert.AreEqual(0, entity.stats.armor);
        }

        [Test]
        public void DuplicatingWithUpgradesKeepsSelectedCardState()
        {
            CardData data = ScriptableObject.CreateInstance<CardData>();
            data.cost = 2;

            GameObject gameObject = new GameObject("Battle");
            BattleManager battle = gameObject.AddComponent<BattleManager>();
            battle.deckManager = gameObject.AddComponent<DeckManager>();
            battle.player = new Entity();

            Card selected = new Card(data);
            selected.ReduceCost();
            selected.AddReactivation();
            battle.deckManager.hand.Add(selected);

            ArticuloData item = ScriptableObject.CreateInstance<ArticuloData>();
            item.tipoEfecto = TipoEfectoArticulo.DuplicarCartaMejoras;
            item.cantidad = 1;

            ItemEffectApplier.ApplyToSelected(item, battle, selected);

            Assert.AreEqual(2, battle.deckManager.hand.Count);
            Card copy = battle.deckManager.hand[1];
            Assert.AreEqual(1, copy.effectiveCost);
            Assert.AreEqual(1, copy.reactivationCount);
            Assert.IsTrue(copy.upgraded);

            Object.DestroyImmediate(item);
            Object.DestroyImmediate(data);
            Object.DestroyImmediate(gameObject);
        }

        [Test]
        public void AddCardChoiceUsesUniqueStartingDeckData()
        {
            CardData first = ScriptableObject.CreateInstance<CardData>();
            first.cardName = "A";
            CardData second = ScriptableObject.CreateInstance<CardData>();
            second.cardName = "B";

            GameObject gameObject = new GameObject("Battle");
            BattleManager battle = gameObject.AddComponent<BattleManager>();
            battle.deckManager = gameObject.AddComponent<DeckManager>();
            battle.deckManager.startingDeck = new List<CardData> { first, first, second };

            ArticuloData item = ScriptableObject.CreateInstance<ArticuloData>();
            item.tipoEfecto = TipoEfectoArticulo.AgregarCartaEleccion;

            List<Card> choices = ItemEffectApplier.GetSelectionSource(item, battle);

            Assert.AreEqual(2, choices.Count);

            Object.DestroyImmediate(item);
            Object.DestroyImmediate(first);
            Object.DestroyImmediate(second);
            Object.DestroyImmediate(gameObject);
        }
    }
}
