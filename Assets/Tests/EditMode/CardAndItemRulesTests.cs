using System.Collections.Generic;
using JuegoDeCartas.Articulos;
using JuegoDeCartas.Cards;
using JuegoDeCartas.Managers;
using JuegoDeCartas.Effects;
using JuegoDeCartas.UI;
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
        public void CardAppliesConfiguredUpgradeOnlyOnce()
        {
            CardData data = ScriptableObject.CreateInstance<CardData>();
            data.cost = 2;
            data.upgradeOptions = new List<CardUpgradeOption>
            {
                new CardUpgradeOption
                {
                    upgradeName = "Rapida",
                    costReduction = 1,
                    reactivations = 1,
                    preventDestroyOnUse = true
                },
                new CardUpgradeOption { upgradeName = "Alternativa" }
            };

            Card card = new Card(data);

            Assert.IsTrue(card.ApplyUpgrade(0));
            Assert.IsFalse(card.ApplyUpgrade(1));
            Assert.AreEqual(1, card.effectiveCost);
            Assert.AreEqual(1, card.reactivationCount);
            Assert.IsTrue(card.preventDestroyOnUse);
            Assert.AreEqual("Rapida", card.SelectedUpgrade.upgradeName);

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

        [Test]
        public void HandRenderRemovesOldChildrenBeforeLayingOutNewCards()
        {
            GameObject root = new GameObject("HandRenderTest");
            GameObject handObject = new GameObject("Hand");
            handObject.transform.SetParent(root.transform);
            new GameObject("OldCardA").transform.SetParent(handObject.transform);
            new GameObject("OldCardB").transform.SetParent(handObject.transform);

            GameObject prefab = new GameObject(
                "CardPrefab",
                typeof(RectTransform),
                typeof(CardView));
            prefab.transform.SetParent(root.transform);

            CardData data = ScriptableObject.CreateInstance<CardData>();
            HandRenderer renderer = new HandRenderer
            {
                handParent = handObject.transform,
                cardPrefab = prefab
            };

            renderer.Render(new List<Card> { new Card(data) }, null);

            Assert.AreEqual(1, handObject.transform.childCount);
            Assert.AreEqual("CardPrefab(Clone)", handObject.transform.GetChild(0).name);

            Object.DestroyImmediate(data);
            Object.DestroyImmediate(root);
        }
    }
}
