using System.Collections.Generic;
using System.Linq;
using JuegoDeCartas.Articulos;
using JuegoDeCartas.Cards;
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
            data.description = "Inflige 10 de dano.";
            data.effects.Add(baseDamage);
            data.epiphanyOptions = new List<CardEpiphany>
            {
                new CardEpiphany
                {
                    epiphanyName = "Filo revelado",
                    description = "Inflige 5 de dano adicional.",
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
