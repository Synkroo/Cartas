using System.Collections.Generic;
using System.Linq;
using JuegoDeCartas.Articulos;
using JuegoDeCartas.Cards;
using JuegoDeCartas.Characters;
using JuegoDeCartas.Managers;
using JuegoDeCartas.Effects;
using JuegoDeCartas.Stats;
using JuegoDeCartas.UI;
using JuegoDeCartas.Enemies;
using JuegoDeCartas.Relics;
using JuegoDeCartas.Challenges;
using JuegoDeCartas.Missions;
using NUnit.Framework;
using UnityEditor;
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
        public void CardAppliesEpiphanyOnlyOnceAndCopiesItWithUpgrades()
        {
            CardData data = ScriptableObject.CreateInstance<CardData>();
            data.cost = 2;
            data.destroyOnUse = true;
            data.epiphanyOptions = new List<CardEpiphany>
            {
                new CardEpiphany
                {
                    epiphanyName = "Revelacion",
                    costReduction = 1,
                    reactivations = 1,
                    preventDestroyOnUse = true
                }
            };

            Card card = new Card(data);

            Assert.IsTrue(card.ApplyEpiphany());
            Assert.IsFalse(card.ApplyEpiphany());
            Assert.IsTrue(card.epiphanyUnlocked);
            Assert.AreEqual(1, card.effectiveCost);
            Assert.AreEqual(1, card.reactivationCount);
            Assert.IsFalse(card.effectiveDestroyOnUse);

            Card preserved = new Card(card, true);
            Card clean = new Card(card, false);
            Assert.IsTrue(preserved.epiphanyUnlocked);
            Assert.IsFalse(clean.epiphanyUnlocked);

            Object.DestroyImmediate(data);
        }

        [Test]
        public void EpiphanyDoesNotBlockTheCardsRegularUpgrade()
        {
            CardData data = ScriptableObject.CreateInstance<CardData>();
            data.epiphanyOptions = new List<CardEpiphany>
            {
                new CardEpiphany { epiphanyName = "Revelacion" }
            };
            data.upgradeOptions = new List<CardUpgradeOption>
            {
                new CardUpgradeOption { upgradeName = "Mejora normal" }
            };

            GameObject gameObject = new GameObject("IndependentEnhancements");
            BattleManager battle = gameObject.AddComponent<BattleManager>();
            battle.deckManager = gameObject.AddComponent<DeckManager>();
            Card card = new Card(data);
            card.ApplyEpiphany();
            battle.deckManager.hand.Add(card);

            ArticuloData item = ScriptableObject.CreateInstance<ArticuloData>();
            item.tipoEfecto = TipoEfectoArticulo.MejorarCarta;

            Assert.Contains(card, ItemEffectApplier.GetSelectionSource(item, battle));
            Assert.IsTrue(card.ApplyUpgrade(0));
            Assert.IsTrue(card.epiphanyUnlocked);
            Assert.AreEqual(0, card.selectedUpgradeIndex);

            Object.DestroyImmediate(item);
            Object.DestroyImmediate(data);
            Object.DestroyImmediate(gameObject);
        }

        [Test]
        public void EveryPlayableCardHasOneConfiguredUniqueEpiphany()
        {
            CardData[] cards = AssetDatabase
                .FindAssets(
                    "t:CardData",
                    new[] { "Assets/Scripts/Cartas/Cartas S.O" }
                )
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<CardData>)
                .Where(card => card != null)
                .ToArray();

            Assert.AreEqual(16, cards.Length);
            Assert.IsTrue(cards.All(card =>
                card.GetEpiphanyOptions().Count == 1 &&
                !string.IsNullOrWhiteSpace(
                    card.GetEpiphanyOptions()[0].epiphanyName) &&
                !string.IsNullOrWhiteSpace(
                    card.GetEpiphanyOptions()[0].description) &&
                !string.IsNullOrWhiteSpace(
                    card.GetEpiphanyOptions()[0].cardDescription) &&
                card.GetEpiphanyOptions()[0].bonusEffects.Count == 1 &&
                card.GetEpiphanyOptions()[0].bonusEffects[0] != null
            ));
            Assert.AreEqual(
                cards.Length,
                cards.Select(card =>
                    card.GetEpiphanyOptions()[0].epiphanyName
                ).Distinct().Count()
            );
        }

        [Test]
        public void UpgradeCardDescriptionUsesShortTextWhenConfigured()
        {
            CardUpgradeOption option = new CardUpgradeOption
            {
                description = "Descripcion extensa para el selector.",
                cardDescription = "Coste -1."
            };

            Assert.AreEqual("Coste -1.", option.GetCardDescription());

            option.cardDescription = "";
            Assert.AreEqual(
                "Descripcion extensa para el selector.",
                option.GetCardDescription()
            );
        }

        [Test]
        public void CardPrefabShowsFramesAndShortDescriptionWithoutStatusLabel()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefabs/CardPrefab.prefab"
            );
            Assert.NotNull(prefab);

            GameObject instance = Object.Instantiate(prefab);
            CardView view = instance.GetComponent<CardView>();
            CardData data = ScriptableObject.CreateInstance<CardData>();
            data.cardName = "Prueba";
            data.description = "Descripcion base.";
            data.upgradeOptions = new List<CardUpgradeOption>
            {
                new CardUpgradeOption
                {
                    upgradeName = "Ligera",
                    description = "Reduce su coste en 1.",
                    cardDescription = "Coste -1.",
                    costReduction = 1
                }
            };
            data.cost = 2;
            Card card = new Card(data);

            view.Setup(card, null);
            Assert.AreSame(view.normalFrameSprite, view.frameImage.sprite);
            object statusText = typeof(CardView)
                .GetField("upgradeStatusText")
                .GetValue(view);
            Assert.IsFalse(((Component)statusText).gameObject.activeSelf);

            card.ApplyUpgrade(0);
            view.Setup(card, null);
            Assert.AreSame(view.upgradedFrameSprite, view.frameImage.sprite);
            Assert.IsFalse(((Component)statusText).gameObject.activeSelf);
            object descriptionText = typeof(CardView)
                .GetField("descriptionText")
                .GetValue(view);
            StringAssert.Contains(
                "Coste -1.",
                (string)descriptionText
                    .GetType()
                    .GetProperty("text")
                    .GetValue(descriptionText)
            );

            data.epiphanyOptions = new List<CardEpiphany>
            {
                new CardEpiphany
                {
                    epiphanyName = "Nombre revelado",
                    description = "Descripcion larga.",
                    cardDescription = "Efecto unico."
                }
            };
            Assert.IsTrue(card.ApplyEpiphany(0));
            view.Setup(card, null);
            Assert.AreSame(view.epiphanyFrameSprite, view.frameImage.sprite);
            object nameText = typeof(CardView)
                .GetField("nameText")
                .GetValue(view);
            string epiphanyName = (string)nameText
                .GetType()
                .GetProperty("text")
                .GetValue(nameText);
            string epiphanyDescription = (string)descriptionText
                .GetType()
                .GetProperty("text")
                .GetValue(descriptionText);
            Assert.AreEqual("Nombre revelado", epiphanyName);
            Assert.AreEqual("Efecto unico.", epiphanyDescription);
            Assert.IsFalse(epiphanyDescription.Contains("Descripcion base."));
            Assert.IsFalse(((Component)statusText).gameObject.activeSelf);

            Object.DestroyImmediate(data);
            Object.DestroyImmediate(instance);
        }

        [Test]
        public void EpiphanyDescriptionShowsTheCardsFinalCombinedValues()
        {
            ModifyStatsEffect baseDamage =
                ScriptableObject.CreateInstance<ModifyStatsEffect>();
            baseDamage.modifiers.Add(new StatModifier
            {
                target = StatModifier.Target.Enemy,
                stat = StatType.Health,
                operation = StatModifier.Operation.Remove,
                amount = 10
            });
            EpiphanyEffect epiphanyBonus =
                ScriptableObject.CreateInstance<EpiphanyEffect>();
            epiphanyBonus.damage = 5;
            epiphanyBonus.armor = 3;

            CardData data = ScriptableObject.CreateInstance<CardData>();
            data.description = "Inflige 10 de daño.";
            data.effects.Add(baseDamage);
            data.epiphanyOptions = new List<CardEpiphany>
            {
                new CardEpiphany
                {
                    epiphanyName = "Filo revelado",
                    description = "Inflige 5 de daño adicional.",
                    bonusEffects = new List<CardEffect> { epiphanyBonus }
                }
            };
            Card card = new Card(data);

            string preview = CardDescriptionBuilder.BuildFinalDescription(
                card,
                data.epiphanyOptions[0]
            );
            Assert.AreEqual(
                "Inflige 15 de daño. Obtiene 3 de armadura.",
                preview
            );

            Assert.IsTrue(card.ApplyEpiphany(0));
            Assert.AreEqual(
                preview,
                CardDescriptionBuilder.BuildFinalDescription(card)
            );

            Object.DestroyImmediate(epiphanyBonus);
            Object.DestroyImmediate(baseDamage);
            Object.DestroyImmediate(data);
        }

        [Test]
        public void DamageRampOnlyAffectsCardDamageNotStatusTicks()
        {
            RunRandom.Initialize(123);
            CharacterRunState.Clear();

            GameObject root = CreateBattleObject(
                "DamageRampBattle",
                out BattleManager battle,
                out EnemyData enemyData
            );
            SubclassData subclass = ScriptableObject.CreateInstance<SubclassData>();
            CharacterData character = ScriptableObject.CreateInstance<CharacterData>();
            subclass.character = character;
            subclass.passiveType = SubclassPassiveType.DamageCardRamp;
            subclass.amount = 2;
            character.subclasses = new List<SubclassData> { subclass };
            CharacterRunState.Select(character);
            Assert.IsTrue(battle.ActivateSubclass(subclass));

            CombatMechanicEffect damage =
                ScriptableObject.CreateInstance<CombatMechanicEffect>();
            damage.action = CombatMechanicAction.DealDamage;
            damage.amount = 5;
            CardData data = ScriptableObject.CreateInstance<CardData>();
            data.cost = 0;
            data.effects = new List<CardEffect> { damage };
            Card first = new Card(data);
            Card second = new Card(data);
            battle.deckManager.hand.Add(first);
            battle.deckManager.hand.Add(second);

            battle.PlayCard(first);
            Assert.AreEqual(95, battle.enemy.stats.health);
            Assert.AreEqual(2, battle.CombatDamageBonus);

            battle.PlayCard(second);
            Assert.AreEqual(88, battle.enemy.stats.health);
            Assert.AreEqual(4, battle.CombatDamageBonus);

            battle.ApplyEnemyStatus(EnemyStatusType.Poison, 3);
            battle.ApplyEnemyTurnStartStatuses();
            Assert.AreEqual(85, battle.enemy.stats.health);

            CharacterRunState.Clear();
            Object.DestroyImmediate(data);
            Object.DestroyImmediate(damage);
            Object.DestroyImmediate(subclass);
            Object.DestroyImmediate(character);
            Object.DestroyImmediate(enemyData);
            Object.DestroyImmediate(root);
        }

        [Test]
        public void FreezeShatterAndBurnMoveCardsThroughExpectedZones()
        {
            RunRandom.Initialize(456);
            GameObject root = CreateBattleObject(
                "FreezeBurnBattle",
                out BattleManager battle,
                out EnemyData enemyData
            );
            CardData data = ScriptableObject.CreateInstance<CardData>();
            data.cost = 1;
            battle.deckManager.hand.Add(new Card(data));
            battle.deckManager.hand.Add(new Card(data));
            battle.deckManager.hand.Add(new Card(data));

            Card freezeTarget = battle.deckManager.hand[1];
            Assert.AreEqual(1, battle.FreezeCardsFromHand(1, freezeTarget));
            Assert.AreEqual(1, battle.FrozenCardCount);
            Assert.AreEqual(3, battle.deckManager.hand.Count);
            Assert.IsTrue(freezeTarget.frozen);

            Assert.AreEqual(1, battle.ShatterFrozenCards(1, 4, 1, 0, freezeTarget));
            Assert.AreEqual(0, battle.FrozenCardCount);
            Assert.AreEqual(3, battle.deckManager.hand.Count);
            Assert.AreEqual(96, battle.enemy.stats.health);
            Assert.IsTrue(freezeTarget.upgraded);
            Assert.AreEqual(0, freezeTarget.effectiveCost);

            Card burnTarget = battle.deckManager.hand[0];
            Assert.AreEqual(1, battle.BurnCardsFromHand(1, burnTarget));
            Assert.AreEqual(2, battle.deckManager.hand.Count);
            Assert.AreEqual(1, battle.deckManager.discard.Count);
            Assert.Contains(burnTarget, battle.deckManager.discard);
            Assert.IsFalse(burnTarget.frozen);
            Assert.AreEqual(1, battle.BurnedCardsThisTurn);
            Assert.AreEqual(1, battle.BurnedCardsThisCombat);

            Card resetTarget = battle.deckManager.hand[0];
            Assert.AreEqual(1, battle.FreezeCardsFromHand(1, resetTarget));
            battle.ResetTemporaryCombatEffects();
            Assert.AreEqual(0, battle.FrozenCardCount);
            Assert.IsFalse(resetTarget.frozen);
            Assert.AreEqual(2, battle.deckManager.discard.Count);

            Object.DestroyImmediate(data);
            Object.DestroyImmediate(enemyData);
            Object.DestroyImmediate(root);
        }

        [Test]
        public void FrozenCardsStayInHandUntilDurationEnds()
        {
            GameObject root = CreateBattleObject(
                "FreezeDurationBattle",
                out BattleManager battle,
                out EnemyData enemyData
            );
            CardData data = ScriptableObject.CreateInstance<CardData>();
            data.cost = 1;
            Card frozen = new Card(data);
            Card normal = new Card(data);
            battle.deckManager.hand.Add(frozen);
            battle.deckManager.hand.Add(normal);

            Assert.AreEqual(1, battle.FreezeCardsFromHand(1, frozen));

            battle.AdvanceFrozenCardsForPlayerTurnEnd();
            battle.deckManager.DiscardHand();
            Assert.Contains(frozen, battle.deckManager.hand);
            Assert.IsTrue(frozen.frozen);
            Assert.AreEqual(1, battle.deckManager.discard.Count);

            battle.AdvanceFrozenCardsForPlayerTurnEnd();
            battle.deckManager.DiscardHand();
            Assert.Contains(frozen, battle.deckManager.hand);
            Assert.IsTrue(frozen.frozen);

            battle.AdvanceFrozenCardsForPlayerTurnEnd();
            battle.deckManager.DiscardHand();
            Assert.IsFalse(frozen.frozen);
            Assert.IsFalse(battle.deckManager.hand.Contains(frozen));
            Assert.Contains(frozen, battle.deckManager.discard);
            Assert.AreEqual(0, battle.FrozenCardCount);

            Object.DestroyImmediate(data);
            Object.DestroyImmediate(enemyData);
            Object.DestroyImmediate(root);
        }

        [Test]
        public void MageIceSubclassGrantsArmorAndDoublesShatteredCardEffects()
        {
            CharacterRunState.Clear();
            GameObject root = CreateBattleObject(
                "MageIceBattle",
                out BattleManager battle,
                out EnemyData enemyData
            );
            SubclassData subclass = ScriptableObject.CreateInstance<SubclassData>();
            CharacterData character = ScriptableObject.CreateInstance<CharacterData>();
            subclass.character = character;
            subclass.passiveType = SubclassPassiveType.MageIceMastery;
            subclass.amount = 5;
            character.subclasses = new List<SubclassData> { subclass };
            CharacterRunState.Select(character);
            Assert.IsTrue(battle.ActivateSubclass(subclass));

            CombatMechanicEffect damage =
                ScriptableObject.CreateInstance<CombatMechanicEffect>();
            damage.action = CombatMechanicAction.DealDamage;
            damage.amount = 4;
            CardData data = ScriptableObject.CreateInstance<CardData>();
            data.cost = 1;
            data.effects = new List<CardEffect> { damage };
            Card frozen = new Card(data);
            battle.deckManager.hand.Add(frozen);

            Assert.AreEqual(1, battle.FreezeCardsFromHand(1, frozen));
            Assert.AreEqual(5, battle.player.stats.armor);
            Assert.AreEqual(5, battle.GetArmorAtPlayerTurnStart(0));

            Assert.AreEqual(1, battle.ShatterFrozenCards(1, 0, 0, 0, frozen));
            Assert.AreEqual(92, battle.enemy.stats.health);

            CharacterRunState.Clear();
            Object.DestroyImmediate(data);
            Object.DestroyImmediate(damage);
            Object.DestroyImmediate(subclass);
            Object.DestroyImmediate(character);
            Object.DestroyImmediate(enemyData);
            Object.DestroyImmediate(root);
        }

        [Test]
        public void MageFireSubclassDrawsAfterBurnAndExplodesRepeatedBurns()
        {
            CharacterRunState.Clear();
            GameObject root = CreateBattleObject(
                "MageFireBattle",
                out BattleManager battle,
                out EnemyData enemyData
            );
            SubclassData subclass = ScriptableObject.CreateInstance<SubclassData>();
            CharacterData character = ScriptableObject.CreateInstance<CharacterData>();
            subclass.character = character;
            subclass.passiveType = SubclassPassiveType.MageFireMastery;
            character.subclasses = new List<SubclassData> { subclass };
            CharacterRunState.Select(character);
            Assert.IsTrue(battle.ActivateSubclass(subclass));

            CombatMechanicEffect damage =
                ScriptableObject.CreateInstance<CombatMechanicEffect>();
            damage.action = CombatMechanicAction.DealDamage;
            damage.amount = 7;
            CardData burnedData = ScriptableObject.CreateInstance<CardData>();
            burnedData.cost = 1;
            burnedData.effects = new List<CardEffect> { damage };
            CardData fillerData = ScriptableObject.CreateInstance<CardData>();
            Card burned = new Card(burnedData);
            battle.deckManager.hand.Add(burned);
            battle.deckManager.deck.Add(new Card(fillerData));

            Assert.AreEqual(1, battle.BurnCardsFromHand(1, burned));
            Assert.IsTrue(burned.burned);
            Assert.AreEqual(1, burned.burnCount);
            Assert.AreEqual(1, battle.deckManager.hand.Count);
            Assert.AreEqual(100, battle.enemy.stats.health);

            battle.deckManager.discard.Remove(burned);
            battle.deckManager.hand.Add(burned);
            battle.deckManager.deck.Add(new Card(fillerData));

            Assert.AreEqual(1, battle.BurnCardsFromHand(1, burned));
            Assert.AreEqual(0, burned.burnCount);
            Assert.AreEqual(93, battle.enemy.stats.health);
            Assert.AreEqual(2, battle.BurnedCardsThisTurn);
            Assert.IsTrue(battle.deckManager.hand.Count >= 1);

            CharacterRunState.Clear();
            Object.DestroyImmediate(fillerData);
            Object.DestroyImmediate(burnedData);
            Object.DestroyImmediate(damage);
            Object.DestroyImmediate(subclass);
            Object.DestroyImmediate(character);
            Object.DestroyImmediate(enemyData);
            Object.DestroyImmediate(root);
        }

        [Test]
        public void MageElectricSubclassOverloadsWhenStunThresholdIsReached()
        {
            CharacterRunState.Clear();
            GameObject root = CreateBattleObject(
                "MageElectricBattle",
                out BattleManager battle,
                out EnemyData enemyData
            );
            SubclassData subclass = ScriptableObject.CreateInstance<SubclassData>();
            CharacterData character = ScriptableObject.CreateInstance<CharacterData>();
            subclass.character = character;
            subclass.passiveType = SubclassPassiveType.MageElectricOverload;
            subclass.amount = 10;
            subclass.percentage = 10;
            character.subclasses = new List<SubclassData> { subclass };
            CharacterRunState.Select(character);
            Assert.IsTrue(battle.ActivateSubclass(subclass));

            battle.ApplyEnemyStatus(EnemyStatusType.Stun, 4);
            Assert.AreEqual(100, battle.enemy.stats.health);

            battle.ApplyEnemyStatus(EnemyStatusType.Stun, 1);

            Assert.AreEqual(85, battle.enemy.stats.health);
            Assert.AreEqual(BattleManager.StunThreshold, battle.enemy.GetStatus(EnemyStatusType.Stun));
            Assert.IsTrue(battle.ConsumeEnemyStunForTurn());
            Assert.AreEqual(0, battle.enemy.GetStatus(EnemyStatusType.Stun));

            CharacterRunState.Clear();
            Object.DestroyImmediate(subclass);
            Object.DestroyImmediate(character);
            Object.DestroyImmediate(enemyData);
            Object.DestroyImmediate(root);
        }

        [Test]
        public void TargetedMechanicClickSelectsSourceThenAppliesToClickedCard()
        {
            GameObject root = CreateBattleObject(
                "TargetedMechanicBattle",
                out BattleManager battle,
                out EnemyData enemyData
            );
            CombatMechanicEffect freeze =
                ScriptableObject.CreateInstance<CombatMechanicEffect>();
            freeze.action = CombatMechanicAction.FreezeCards;
            freeze.amount = 1;
            CardData sourceData = ScriptableObject.CreateInstance<CardData>();
            sourceData.cost = 1;
            sourceData.effects = new List<CardEffect> { freeze };
            CardData targetData = ScriptableObject.CreateInstance<CardData>();
            targetData.cost = 0;
            Card source = new Card(sourceData);
            Card target = new Card(targetData);
            battle.deckManager.hand.Add(source);
            battle.deckManager.hand.Add(target);

            battle.HandleCardClicked(source);

            Assert.IsTrue(battle.IsCardAwaitingMechanicTarget(source));
            Assert.Contains(source, battle.deckManager.hand);
            Assert.AreEqual(3, battle.player.stats.mana);

            battle.HandleCardClicked(target);

            Assert.IsFalse(battle.IsCardAwaitingMechanicTarget(source));
            Assert.IsFalse(battle.deckManager.hand.Contains(source));
            Assert.Contains(source, battle.deckManager.discard);
            Assert.Contains(target, battle.deckManager.hand);
            Assert.IsTrue(target.frozen);
            Assert.AreEqual(2, battle.player.stats.mana);

            Object.DestroyImmediate(targetData);
            Object.DestroyImmediate(sourceData);
            Object.DestroyImmediate(freeze);
            Object.DestroyImmediate(enemyData);
            Object.DestroyImmediate(root);
        }

        [Test]
        public void EnemyStatusesTickControlAndModifyDamage()
        {
            GameObject root = CreateBattleObject(
                "StatusBattle",
                out BattleManager battle,
                out EnemyData enemyData
            );

            battle.ApplyEnemyStatus(EnemyStatusType.Poison, 4);
            battle.ApplyEnemyStatus(EnemyStatusType.Bleed, 3);
            battle.ApplyEnemyStatus(EnemyStatusType.Weakness, 3);
            battle.ApplyEnemyStatus(EnemyStatusType.Stun, BattleManager.StunThreshold);

            Assert.AreEqual(1, battle.enemy.GetStatus(EnemyStatusType.Poison));
            Assert.AreEqual(10, battle.ModifyEnemyAttackDamage(10));
            Assert.IsTrue(battle.ConsumeEnemyStunForTurn());
            Assert.AreEqual(0, battle.enemy.GetStatus(EnemyStatusType.Stun));

            battle.ApplyEnemyTurnStartStatuses();
            Assert.AreEqual(92, battle.enemy.stats.health);
            Assert.AreEqual(1, battle.enemy.GetStatus(EnemyStatusType.Poison));
            Assert.AreEqual(3, battle.enemy.GetStatus(EnemyStatusType.Bleed));
            Assert.AreEqual(3, battle.enemy.GetStatus(EnemyStatusType.Weakness));

            battle.ApplyEnemyTurnStartStatuses();
            Assert.AreEqual(84, battle.enemy.stats.health);
            Assert.AreEqual(0, battle.enemy.GetStatus(EnemyStatusType.Bleed));
            Assert.AreEqual(2, battle.enemy.GetStatus(EnemyStatusType.Weakness));

            Object.DestroyImmediate(enemyData);
            Object.DestroyImmediate(root);
        }

        [Test]
        public void WeaknessCriticalDamageConsumesStacksAndUsesSubclassBonus()
        {
            CharacterRunState.Clear();
            GameObject root = CreateBattleObject(
                "WeaknessBattle",
                out BattleManager battle,
                out EnemyData enemyData
            );
            SubclassData subclass = ScriptableObject.CreateInstance<SubclassData>();
            CharacterData character = ScriptableObject.CreateInstance<CharacterData>();
            subclass.character = character;
            subclass.passiveType = SubclassPassiveType.WeaknessCriticalDamage;
            subclass.amount = 8;
            character.subclasses = new List<SubclassData> { subclass };
            CharacterRunState.Select(character);
            Assert.IsTrue(battle.ActivateSubclass(subclass));

            battle.ApplyEnemyStatus(EnemyStatusType.Weakness, 3);
            int resolvedDamage = battle.DamageEnemyWithWeakness(16, 3, 2, true);

            Assert.AreEqual(18, resolvedDamage);
            Assert.AreEqual(82, battle.enemy.stats.health);
            Assert.AreEqual(0, battle.enemy.GetStatus(EnemyStatusType.Weakness));

            CharacterRunState.Clear();
            Object.DestroyImmediate(subclass);
            Object.DestroyImmediate(character);
            Object.DestroyImmediate(enemyData);
            Object.DestroyImmediate(root);
        }

        [Test]
        public void ReworkedClassAssetsExposeTheirBuildMechanics()
        {
            CharacterData knight = AssetDatabase.LoadAssetAtPath<CharacterData>(
                "Assets/GameData/Characters/Caballero.asset");
            CharacterData mage = AssetDatabase.LoadAssetAtPath<CharacterData>(
                "Assets/GameData/Characters/Mago.asset");
            CharacterData rogue = AssetDatabase.LoadAssetAtPath<CharacterData>(
                "Assets/GameData/Characters/Picaro.asset");

            CollectionAssert.AreEquivalent(
                new[]
                {
                    SubclassPassiveType.DamageCardRamp,
                    SubclassPassiveType.StackDamageBuffs,
                    SubclassPassiveType.ArmorToDamageAndRetain
                },
                knight.subclasses.Select(subclass => subclass.passiveType)
            );
            CollectionAssert.AreEquivalent(
                new[]
                {
                    SubclassPassiveType.MageIceMastery,
                    SubclassPassiveType.MageFireMastery,
                    SubclassPassiveType.MageElectricOverload
                },
                mage.subclasses.Select(subclass => subclass.passiveType)
            );
            CollectionAssert.AreEquivalent(
                new[]
                {
                    SubclassPassiveType.WeaknessCriticalDamage,
                    SubclassPassiveType.PoisonAmplifier,
                    SubclassPassiveType.BleedAmplifier
                },
                rogue.subclasses.Select(subclass => subclass.passiveType)
            );

            CardData spark = AssetDatabase.LoadAssetAtPath<CardData>(
                "Assets/Scripts/Cartas/Cartas S.O/Mago/Chispa.asset");
            CardData missile = AssetDatabase.LoadAssetAtPath<CardData>(
                "Assets/Scripts/Cartas/Cartas S.O/Mago/MisilArcano.asset");
            CardData barrier = AssetDatabase.LoadAssetAtPath<CardData>(
                "Assets/Scripts/Cartas/Cartas S.O/Mago/BarreraArcana.asset");
            CardData fireball = AssetDatabase.LoadAssetAtPath<CardData>(
                "Assets/Scripts/Cartas/Cartas S.O/Mago/BoladeFuego.asset");
            CardData dagger = AssetDatabase.LoadAssetAtPath<CardData>(
                "Assets/Scripts/Cartas/Cartas S.O/Picaro/Punal.asset");
            CardData plunder = AssetDatabase.LoadAssetAtPath<CardData>(
                "Assets/Scripts/Cartas/Cartas S.O/Picaro/Saqueo.asset");
            CardData execution = AssetDatabase.LoadAssetAtPath<CardData>(
                "Assets/Scripts/Cartas/Cartas S.O/Picaro/Ejecucion.asset");

            Assert.AreEqual("Descarga", spark.cardName);
            Assert.AreEqual("Rayo Canalizado", missile.cardName);
            Assert.AreEqual(2, missile.cost);
            Assert.AreEqual("Escarcha Arcana", barrier.cardName);
            Assert.AreEqual("Combustion", fireball.cardName);
            Assert.AreEqual("Corte Expuesto", dagger.cardName);
            Assert.AreEqual("Golpe de Saqueo", plunder.cardName);
            Assert.AreEqual(2, plunder.cost);
            Assert.AreEqual("Remate Preciso", execution.cardName);

            Assert.IsTrue(HasMechanic(
                spark,
                CombatMechanicAction.ApplyEnemyStatus,
                EnemyStatusType.Stun
            ));
            Assert.IsTrue(HasMechanic(barrier, CombatMechanicAction.FreezeCards));
            Assert.IsTrue(HasMechanic(fireball, CombatMechanicAction.BurnCards));
            Assert.IsTrue(HasMechanic(
                dagger,
                CombatMechanicAction.ApplyEnemyStatus,
                EnemyStatusType.Weakness
            ));
            Assert.IsTrue(
                HasMechanic(execution, CombatMechanicAction.CriticalDamageFromWeakness)
            );
        }

        [Test]
        public void EntityDamageConsumesArmorBeforeHealth()
        {
            Entity entity = new Entity();
            entity.stats.maxHealth = 20;
            entity.stats.health = 20;
            entity.stats.armor = 5;

            DamageResult result = entity.TakeDamage(8);

            Assert.AreEqual(5, result.armorAbsorbed);
            Assert.AreEqual(3, result.healthDamage);
            Assert.AreEqual(17, entity.stats.health);
            Assert.AreEqual(0, entity.stats.armor);
        }

        [Test]
        public void DamageResultCapsOverkillToHealthActuallyLost()
        {
            Entity entity = new Entity();
            entity.stats.maxHealth = 20;
            entity.stats.health = 3;
            entity.stats.armor = 2;

            DamageResult result = entity.TakeDamage(100);

            Assert.AreEqual(100, result.attemptedDamage);
            Assert.AreEqual(2, result.armorAbsorbed);
            Assert.AreEqual(3, result.healthDamage);
            Assert.IsTrue(result.defeated);
            Assert.AreEqual(0, entity.stats.health);
        }

        [Test]
        public void MultiHitCardDefersShopAndRewardsEnemyOnlyOnce()
        {
            MissionRunState.Clear();
            ChallengeRunState.Clear();
            RunRandom.Initialize(12345);

            GameObject root = new GameObject("MultiHitBattle");
            BattleManager battle = root.AddComponent<BattleManager>();
            battle.player = new Entity();
            battle.player.stats.maxHealth = 20;
            battle.player.stats.health = 10;
            battle.player.stats.maxMana = 3;
            battle.player.stats.mana = 3;
            battle.deckManager = root.AddComponent<DeckManager>();
            battle.turnManager = root.AddComponent<TurnManager>();
            battle.turnManager.battle = battle;
            battle.turnManager.currentTurn = TurnManager.Turn.Player;
            battle.statsTracker = root.AddComponent<GameStatsTracker>();

            GameManager gameManager = root.AddComponent<GameManager>();
            gameManager.dinero = 0;
            battle.gameManager = gameManager;

            ShopManager shop = root.AddComponent<ShopManager>();
            shop.gameManager = gameManager;
            shop.battle = battle;
            shop.pauseTime = false;
            shop.interestPerStep = 0;
            shop.shopPanel = new GameObject("ShopPanel");
            shop.shopPanel.transform.SetParent(root.transform);
            shop.shopPanel.SetActive(false);
            gameManager.shopManager = shop;

            RelicData lifeSteal = ScriptableObject.CreateInstance<RelicData>();
            lifeSteal.effectType = RelicEffectType.LifeSteal;
            lifeSteal.percentage = 50;
            RelicInventory relicInventory =
                root.AddComponent<RelicInventory>();
            battle.relicInventory = relicInventory;
            relicInventory.Initialize(battle);
            Assert.IsTrue(relicInventory.TryAdd(lifeSteal));

            EnemyData first = CreateEnemy("First", 3, 7);
            EnemyData second = CreateEnemy("Second", 20, 0);
            battle.waveManager.battleManager = battle;
            battle.waveManager.gameManager = gameManager;
            battle.waveManager.statsTracker = battle.statsTracker;
            battle.waveManager.normalEnemies =
                new List<EnemyData> { first };
            battle.waveManager.finalBoss = second;
            battle.waveManager.totalCombats = 2;
            int defeatedEvents = 0;
            battle.waveManager.OnEnemyDefeated += () => defeatedEvents++;
            battle.waveManager.Initialize();

            MultiHitProbeEffect effect =
                ScriptableObject.CreateInstance<MultiHitProbeEffect>();
            effect.firstDamage = 10;
            effect.secondDamage = 10;
            effect.shopPanel = shop.shopPanel;

            CardData cardData = ScriptableObject.CreateInstance<CardData>();
            cardData.cost = 0;
            cardData.effects = new List<CardEffect> { effect };
            Card card = new Card(cardData);
            battle.deckManager.hand.Add(card);

            battle.PlayCard(card);

            Assert.IsFalse(effect.shopWasOpenBetweenHits);
            Assert.IsTrue(shop.shopPanel.activeSelf);
            Assert.AreEqual(1, defeatedEvents);
            Assert.AreEqual(1, battle.statsTracker.enemiesDefeated);
            Assert.AreEqual(150, gameManager.dinero);
            Assert.AreEqual(3, battle.statsTracker.totalDamageDealt);
            Assert.AreEqual(11, battle.player.stats.health);

            Time.timeScale = 1f;
            Object.DestroyImmediate(cardData);
            Object.DestroyImmediate(effect);
            Object.DestroyImmediate(first);
            Object.DestroyImmediate(second);
            Object.DestroyImmediate(lifeSteal);
            Object.DestroyImmediate(root);
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
        public void CrossClassItemOffersOnlyCardsOutsideStartingClass()
        {
            CardData own = ScriptableObject.CreateInstance<CardData>();
            own.cardName = "Propia";
            CardData externalA = ScriptableObject.CreateInstance<CardData>();
            externalA.cardName = "Externa A";
            CardData externalB = ScriptableObject.CreateInstance<CardData>();
            externalB.cardName = "Externa B";

            GameObject gameObject = new GameObject("CrossClassBattle");
            BattleManager battle = gameObject.AddComponent<BattleManager>();
            battle.deckManager = gameObject.AddComponent<DeckManager>();
            battle.deckManager.startingDeck = new List<CardData> { own, own };

            ArticuloData item = ScriptableObject.CreateInstance<ArticuloData>();
            item.tipoEfecto = TipoEfectoArticulo.AgregarCartaOtraClase;
            item.cardPool = new List<CardData>
            {
                own,
                externalA,
                externalA,
                externalB
            };

            List<Card> choices = ItemEffectApplier.GetSelectionSource(item, battle);

            Assert.AreEqual(2, choices.Count);
            Assert.IsFalse(choices.Any(card => card.data == own));
            Assert.IsTrue(choices.Any(card => card.data == externalA));
            Assert.IsTrue(choices.Any(card => card.data == externalB));

            Object.DestroyImmediate(item);
            Object.DestroyImmediate(own);
            Object.DestroyImmediate(externalA);
            Object.DestroyImmediate(externalB);
            Object.DestroyImmediate(gameObject);
        }

        [Test]
        public void EpiphanyItemOnlyOffersCardsThatCanStillAwaken()
        {
            CardData availableData = ScriptableObject.CreateInstance<CardData>();
            availableData.epiphanyOptions = new List<CardEpiphany>
            {
                new CardEpiphany { epiphanyName = "Disponible" }
            };
            CardData missingData = ScriptableObject.CreateInstance<CardData>();
            CardData awakenedData = ScriptableObject.CreateInstance<CardData>();
            awakenedData.epiphanyOptions = new List<CardEpiphany>
            {
                new CardEpiphany { epiphanyName = "Ya usada" }
            };

            GameObject gameObject = new GameObject("EpiphanyBattle");
            BattleManager battle = gameObject.AddComponent<BattleManager>();
            battle.deckManager = gameObject.AddComponent<DeckManager>();
            Card available = new Card(availableData);
            Card missing = new Card(missingData);
            Card awakened = new Card(awakenedData);
            awakened.ApplyEpiphany();
            battle.deckManager.hand.AddRange(new[] { available, missing, awakened });

            ArticuloData item = ScriptableObject.CreateInstance<ArticuloData>();
            item.tipoEfecto = TipoEfectoArticulo.DespertarEpifania;

            List<Card> choices = ItemEffectApplier.GetSelectionSource(item, battle);

            Assert.AreEqual(1, choices.Count);
            Assert.AreSame(available, choices[0]);

            Object.DestroyImmediate(item);
            Object.DestroyImmediate(availableData);
            Object.DestroyImmediate(missingData);
            Object.DestroyImmediate(awakenedData);
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

        static EnemyData CreateEnemy(
            string name,
            int health,
            int armor)
        {
            EnemyData enemy = ScriptableObject.CreateInstance<EnemyData>();
            enemy.name = name;
            enemy.enemyName = name;
            enemy.maxHealth = health;
            enemy.startArmor = armor;
            enemy.minDamage = 0;
            enemy.maxDamage = 0;
            return enemy;
        }

        static GameObject CreateBattleObject(
            string name,
            out BattleManager battle,
            out EnemyData enemyData)
        {
            GameObject root = new GameObject(name);
            battle = root.AddComponent<BattleManager>();
            battle.player = new Entity();
            battle.player.stats.maxHealth = 30;
            battle.player.stats.health = 30;
            battle.player.stats.maxMana = 3;
            battle.player.stats.mana = 3;
            battle.deckManager = root.AddComponent<DeckManager>();
            battle.turnManager = root.AddComponent<TurnManager>();
            battle.turnManager.battle = battle;
            battle.turnManager.currentTurn = TurnManager.Turn.Player;

            enemyData = CreateEnemy(name + "Enemy", 100, 0);
            battle.waveManager.battleManager = battle;
            battle.waveManager.enemyWave = new List<EnemyData> { enemyData };
            battle.waveManager.Initialize();
            return root;
        }

        static bool HasMechanic(
            CardData card,
            CombatMechanicAction action,
            EnemyStatusType statusType = EnemyStatusType.Weakness)
        {
            return card != null &&
                   card.effects.Any(effect =>
                       effect is CombatMechanicEffect mechanic &&
                       mechanic.action == action &&
                       (action != CombatMechanicAction.ApplyEnemyStatus ||
                        mechanic.statusType == statusType));
        }
    }

    public class MultiHitProbeEffect : CardEffect
    {
        public int firstDamage;
        public int secondDamage;
        public GameObject shopPanel;
        public bool shopWasOpenBetweenHits;

        public override void Apply(BattleManager battle)
        {
            battle.DamageEnemy(firstDamage);
            shopWasOpenBetweenHits =
                shopPanel != null && shopPanel.activeSelf;
            battle.DamageEnemy(secondDamage);
        }
    }
}
