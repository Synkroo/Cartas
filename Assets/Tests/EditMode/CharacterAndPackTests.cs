using System.Collections.Generic;
using System.Linq;
using JuegoDeCartas.Articulos;
using JuegoDeCartas.Cards;
using JuegoDeCartas.Characters;
using JuegoDeCartas.Managers;
using JuegoDeCartas.Missions;
using JuegoDeCartas.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace JuegoDeCartas.Tests
{
    public class CharacterAndPackTests
    {
        [SetUp]
        public void SetUp()
        {
            CharacterRunState.Clear();
            MissionRunState.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            CharacterRunState.Clear();
            MissionRunState.Clear();
        }

        [Test]
        public void CreatedCharactersHaveExpectedStatsAndDecks()
        {
            CharacterData knight = AssetDatabase.LoadAssetAtPath<CharacterData>(
                "Assets/GameData/Characters/Caballero.asset");
            CharacterData mage = AssetDatabase.LoadAssetAtPath<CharacterData>(
                "Assets/GameData/Characters/Mago.asset");
            CharacterData rogue = AssetDatabase.LoadAssetAtPath<CharacterData>(
                "Assets/GameData/Characters/Picaro.asset");

            Assert.NotNull(knight);
            Assert.NotNull(mage);
            Assert.NotNull(rogue);

            Assert.AreEqual(50, knight.maxHealth);
            Assert.AreEqual(3, knight.maxMana);
            Assert.AreEqual(10, knight.startingDeck.Count);
            Assert.IsTrue(knight.unlockedByDefault);

            Assert.AreEqual(40, mage.maxHealth);
            Assert.AreEqual(4, mage.maxMana);
            Assert.AreEqual(10, mage.startingDeck.Count);
            Assert.NotNull(mage.unlockCondition);

            Assert.IsTrue(rogue.comingSoon);
            Assert.IsFalse(rogue.IsSelectable);
            Assert.NotNull(rogue.unlockCondition);
        }

        [Test]
        public void CompletingMediumDifficultyMeetsMageUnlockCondition()
        {
            const string globalKey = "AnyMissionCompleted_Media";
            bool hadGlobal = PlayerPrefs.HasKey(globalKey);
            int previousGlobal = PlayerPrefs.GetInt(globalKey, 0);

            MissionData mission = ScriptableObject.CreateInstance<MissionData>();
            mission.name = "CodexUnlockTestMission";
            var condition = ScriptableObject.CreateInstance<DifficultyCompletionUnlockCondition>();
            condition.requiredDifficulty = MissionDifficulty.Media;
            condition.missions = new List<MissionData> { mission };

            string missionKey = "MissionCompleted_" + mission.name + "_Media";
            PlayerPrefs.DeleteKey(missionKey);
            PlayerPrefs.DeleteKey(globalKey);

            Assert.IsFalse(condition.IsMet());
            mission.MarkCompleted(MissionDifficulty.Media);
            Assert.IsTrue(condition.IsMet());

            PlayerPrefs.DeleteKey(missionKey);
            if (hadGlobal)
                PlayerPrefs.SetInt(globalKey, previousGlobal);
            else
                PlayerPrefs.DeleteKey(globalKey);
            PlayerPrefs.Save();

            Object.DestroyImmediate(condition);
            Object.DestroyImmediate(mission);
        }

        [Test]
        public void BattleAppliesSelectedCharacterStatsDeckAndGold()
        {
            CardData card = ScriptableObject.CreateInstance<CardData>();
            CharacterData character = ScriptableObject.CreateInstance<CharacterData>();
            character.maxHealth = 40;
            character.maxMana = 4;
            character.cardsPerTurn = 5;
            character.startingGold = 450;
            character.startingDeck = new List<CardData> { card, card };

            GameObject root = new GameObject("BattleTest");
            BattleManager battle = root.AddComponent<BattleManager>();
            battle.deckManager = root.AddComponent<DeckManager>();
            battle.gameManager = root.AddComponent<GameManager>();
            battle.player = new Entity();

            CharacterRunState.Select(character);
            battle.ApplySelectedCharacter();

            Assert.AreEqual(40, battle.player.stats.maxHealth);
            Assert.AreEqual(40, battle.player.stats.health);
            Assert.AreEqual(4, battle.player.stats.maxMana);
            Assert.AreEqual(4, battle.player.stats.mana);
            Assert.AreEqual(5, battle.deckManager.cardsPerTurn);
            Assert.AreEqual(2, battle.deckManager.startingDeck.Count);
            Assert.AreEqual(450, battle.gameManager.dinero);

            Object.DestroyImmediate(root);
            Object.DestroyImmediate(character);
            Object.DestroyImmediate(card);
        }

        [Test]
        public void PackGeneratorReturnsRequestedCountWithoutDuplicates()
        {
            ItemPackData pack = ScriptableObject.CreateInstance<ItemPackData>();
            pack.choiceCount = 5;
            pack.displayRarity = Rareza.Epico;
            pack.rarityWeights = new List<PackRarityWeight>
            {
                new PackRarityWeight { rarity = Rareza.Epico, weight = 100f }
            };

            List<ArticuloData> pool = new List<ArticuloData>
            {
                CreateItem("C1", Rareza.Comun),
                CreateItem("C2", Rareza.Comun),
                CreateItem("R1", Rareza.Raro),
                CreateItem("R2", Rareza.Raro),
                CreateItem("E1", Rareza.Epico)
            };

            ItemPackOffer offer = ItemPackGenerator.Generate(pack, pool, 42);

            Assert.AreEqual(5, offer.Contents.Count);
            Assert.AreEqual(5, offer.Contents.Distinct().Count());
            Assert.AreEqual(Rareza.Epico, offer.Contents[0].rareza);
            CollectionAssert.AreEquivalent(pool, offer.Contents);

            DestroyItems(pool);
            Object.DestroyImmediate(pack);
        }

        [Test]
        public void PackAssetsMatchConfiguredPricesCountsAndWeights()
        {
            ItemPackData common = LoadPack("SobreComun");
            ItemPackData rare = LoadPack("SobreRaro");
            ItemPackData epic = LoadPack("SobreEpico");
            ItemPackData vitality = LoadPack("SobreVitalidad");
            ItemPackData arcane = LoadPack("SobreArcano");
            ItemPackData forge = LoadPack("SobreForja");
            ItemPackData chaos = LoadPack("SobreCaos");

            AssertPack(common, 200, 3, 24f, 85f, 15f, 0f);
            AssertPack(rare, 400, 4, 12f, 15f, 75f, 10f);
            AssertPack(epic, 800, 5, 4f, 0f, 25f, 75f);
            AssertPack(vitality, 250, 4, 20f, 60f, 35f, 5f);
            AssertPack(arcane, 350, 3, 16f, 45f, 45f, 10f);
            AssertPack(forge, 450, 3, 14f, 10f, 65f, 25f);
            AssertPack(chaos, 550, 5, 10f, 20f, 45f, 35f);
        }

        [Test]
        public void ShopPackRollUsesAppearanceWeightsAndAllowsRepeatedTypes()
        {
            ItemPackData common = CreatePackDefinition("Comun", 3);
            ItemPackData rare = CreatePackDefinition("Raro", 4);
            ItemPackData epic = CreatePackDefinition("Epico", 5);
            common.shopAppearanceWeight = 1f;
            rare.shopAppearanceWeight = 0f;
            epic.shopAppearanceWeight = 0f;
            var definitions = new List<ItemPackData> { common, rare, epic };

            for (int i = 0; i < 10; i++)
                Assert.AreSame(common, ItemPackGenerator.RollDefinition(definitions, i));

            Object.DestroyImmediate(common);
            Object.DestroyImmediate(rare);
            Object.DestroyImmediate(epic);
        }

        [Test]
        public void BuyingAndClaimingPackChargesOnceAndAppliesItem()
        {
            ShopFixture fixture = CreateShopFixture();
            fixture.gameManager.dinero = 300;
            fixture.battle.player.stats.maxHealth = 20;
            fixture.battle.player.stats.health = 10;

            ArticuloData heal = CreateItem("Curacion", Rareza.Comun);
            heal.tipoEfecto = TipoEfectoArticulo.CurarVida;
            heal.cantidad = 5;
            ItemPackOffer offer = CreateOffer(heal, 200);

            Assert.IsTrue(fixture.shop.TryPurchasePack(offer));
            Assert.AreEqual(100, fixture.gameManager.dinero);
            Assert.IsTrue(fixture.packPanel.activeSelf);

            Assert.IsTrue(fixture.shop.TryClaimItem(offer, heal));
            Assert.AreEqual(15, fixture.battle.player.stats.health);
            Assert.IsTrue(offer.Claimed);
            Assert.IsFalse(fixture.shop.TryClaimItem(offer, heal));
            Assert.AreEqual(100, fixture.gameManager.dinero);

            fixture.Destroy();
            Object.DestroyImmediate(heal);
            Object.DestroyImmediate(offer.Definition);
        }

        [Test]
        public void PackPurchaseFailsWithoutEnoughGold()
        {
            ShopFixture fixture = CreateShopFixture();
            fixture.gameManager.dinero = 199;
            ArticuloData heal = CreateItem("Curacion", Rareza.Comun);
            heal.tipoEfecto = TipoEfectoArticulo.CurarVida;
            ItemPackOffer offer = CreateOffer(heal, 200);

            Assert.IsFalse(fixture.shop.TryPurchasePack(offer));
            Assert.AreEqual(199, fixture.gameManager.dinero);
            Assert.IsFalse(fixture.packPanel.activeSelf);

            fixture.Destroy();
            Object.DestroyImmediate(heal);
            Object.DestroyImmediate(offer.Definition);
        }

        [Test]
        public void CancellingCardSelectionReturnsToPackChoices()
        {
            ShopFixture fixture = CreateShopFixture(true);
            fixture.gameManager.dinero = 300;

            CardData cardData = ScriptableObject.CreateInstance<CardData>();
            fixture.battle.deckManager.hand.Add(new Card(cardData));

            ArticuloData duplicate = CreateItem("Duplicar", Rareza.Raro);
            duplicate.tipoEfecto = TipoEfectoArticulo.DuplicarCarta;
            duplicate.cantidad = 1;
            ItemPackOffer offer = CreateOffer(duplicate, 200);

            Assert.IsTrue(fixture.shop.TryPurchasePack(offer));
            Assert.IsTrue(fixture.shop.TryClaimItem(offer, duplicate));
            Assert.IsFalse(fixture.packPanel.activeSelf);
            Assert.IsTrue(fixture.cardPanel.activeSelf);

            fixture.cardSelection.Close();

            Assert.IsTrue(fixture.packPanel.activeSelf);
            Assert.IsFalse(fixture.cardPanel.activeSelf);
            Assert.IsFalse(offer.Claimed);

            fixture.Destroy();
            Object.DestroyImmediate(duplicate);
            Object.DestroyImmediate(offer.Definition);
            Object.DestroyImmediate(cardData);
        }

        [Test]
        public void RestockRequiresGoldAndRegeneratesAllPacks()
        {
            ShopFixture fixture = CreateShopFixture();
            fixture.shop.packPrefab = fixture.packPrefab;
            fixture.shop.packDefinitions = new List<ItemPackData>
            {
                CreatePackDefinition("Comun", 3),
                CreatePackDefinition("Raro", 4),
                CreatePackDefinition("Epico", 5)
            };
            fixture.shop.itemPool = new List<ArticuloData>
            {
                CreateItem("A", Rareza.Comun),
                CreateItem("B", Rareza.Comun),
                CreateItem("C", Rareza.Raro),
                CreateItem("D", Rareza.Raro),
                CreateItem("E", Rareza.Epico)
            };
            fixture.shop.slotContainers = new[]
            {
                new GameObject("Slot1").transform,
                new GameObject("Slot2").transform,
                new GameObject("Slot3").transform
            };
            for (int i = 0; i < fixture.shop.slotContainers.Length; i++)
                fixture.shop.slotContainers[i].SetParent(fixture.root.transform);

            fixture.shop.restockCost = 100;
            fixture.gameManager.dinero = 99;
            Assert.IsFalse(fixture.shop.TryRestock());
            Assert.AreEqual(99, fixture.gameManager.dinero);

            fixture.gameManager.dinero = 500;
            Assert.IsTrue(fixture.shop.TryRestock());
            Assert.AreEqual(400, fixture.gameManager.dinero);
            Assert.AreEqual(3, fixture.shop.CurrentOffers.Count);
            for (int i = 0; i < fixture.shop.CurrentOffers.Count; i++)
            {
                ItemPackOffer offer = fixture.shop.CurrentOffers[i];
                Assert.NotNull(offer.Definition);
                Assert.AreEqual(offer.Definition.choiceCount, offer.Contents.Count);
            }

            foreach (ItemPackData definition in fixture.shop.packDefinitions)
                Object.DestroyImmediate(definition);
            DestroyItems(fixture.shop.itemPool);
            fixture.Destroy();
        }

        static ShopFixture CreateShopFixture(bool withCardSelection = false)
        {
            var fixture = new ShopFixture();
            fixture.root = new GameObject("ShopFixture");
            fixture.shop = fixture.root.AddComponent<ShopManager>();
            fixture.gameManager = fixture.root.AddComponent<GameManager>();
            fixture.battle = fixture.root.AddComponent<BattleManager>();
            fixture.battle.deckManager = fixture.root.AddComponent<DeckManager>();
            fixture.battle.player = new Entity();
            fixture.shop.gameManager = fixture.gameManager;
            fixture.shop.battle = fixture.battle;

            fixture.shopPanel = new GameObject("ShopPanel");
            fixture.shopPanel.transform.SetParent(fixture.root.transform);
            fixture.shop.shopPanel = fixture.shopPanel;

            fixture.packPanel = new GameObject("PackPanel");
            fixture.packPanel.transform.SetParent(fixture.root.transform);
            fixture.packPanel.SetActive(false);
            fixture.packContent = new GameObject("PackContent");
            fixture.packContent.transform.SetParent(fixture.packPanel.transform);
            fixture.choicePrefab = new GameObject(
                "ChoicePrefab", typeof(RectTransform), typeof(Button), typeof(PackItemChoiceDisplay));
            fixture.choicePrefab.transform.SetParent(fixture.root.transform);

            fixture.packSelection = fixture.packPanel.AddComponent<ItemPackSelectionUI>();
            fixture.packSelection.panel = fixture.packPanel;
            fixture.packSelection.content = fixture.packContent.transform;
            fixture.packSelection.itemChoicePrefab = fixture.choicePrefab;
            fixture.shop.packSelectionUI = fixture.packSelection;

            fixture.packPrefab = new GameObject(
                "PackPrefab", typeof(RectTransform), typeof(Button), typeof(ItemPackDisplay));
            fixture.packPrefab.transform.SetParent(fixture.root.transform);

            if (withCardSelection)
            {
                fixture.cardPanel = new GameObject("CardPanel");
                fixture.cardPanel.transform.SetParent(fixture.root.transform);
                fixture.cardPanel.SetActive(false);
                GameObject cardContent = new GameObject("CardContent");
                cardContent.transform.SetParent(fixture.cardPanel.transform);
                GameObject cardPrefab = new GameObject(
                    "CardPrefab", typeof(RectTransform), typeof(Button), typeof(CardView));
                cardPrefab.transform.SetParent(fixture.root.transform);

                fixture.cardSelection = fixture.cardPanel.AddComponent<CardSelectionUI>();
                fixture.cardSelection.panel = fixture.cardPanel;
                fixture.cardSelection.contentParent = cardContent.transform;
                fixture.cardSelection.cardPrefab = cardPrefab;
                fixture.cardSelection.battle = fixture.battle;
                fixture.shop.cardSelectionUI = fixture.cardSelection;
            }

            return fixture;
        }

        static ItemPackOffer CreateOffer(ArticuloData item, int price)
        {
            ItemPackData definition = ScriptableObject.CreateInstance<ItemPackData>();
            definition.price = price;
            definition.choiceCount = 1;
            return new ItemPackOffer(definition, new List<ArticuloData> { item });
        }

        static ItemPackData CreatePackDefinition(string name, int count)
        {
            ItemPackData definition = ScriptableObject.CreateInstance<ItemPackData>();
            definition.packName = name;
            definition.choiceCount = count;
            definition.shopAppearanceWeight = 1f;
            definition.rarityWeights = new List<PackRarityWeight>
            {
                new PackRarityWeight { rarity = Rareza.Comun, weight = 1f }
            };
            return definition;
        }

        static ArticuloData CreateItem(string name, Rareza rarity)
        {
            ArticuloData item = ScriptableObject.CreateInstance<ArticuloData>();
            item.nombre = name;
            item.rareza = rarity;
            return item;
        }

        static ItemPackData LoadPack(string name)
        {
            return AssetDatabase.LoadAssetAtPath<ItemPackData>("Assets/GameData/Packs/" + name + ".asset");
        }

        static void AssertPack(
            ItemPackData pack,
            int price,
            int count,
            float appearanceWeight,
            float commonWeight,
            float rareWeight,
            float epicWeight)
        {
            Assert.NotNull(pack);
            Assert.AreEqual(price, pack.price);
            Assert.AreEqual(count, pack.choiceCount);
            Assert.AreEqual(appearanceWeight, pack.shopAppearanceWeight);
            Assert.AreEqual(commonWeight, GetWeight(pack, Rareza.Comun));
            Assert.AreEqual(rareWeight, GetWeight(pack, Rareza.Raro));
            Assert.AreEqual(epicWeight, GetWeight(pack, Rareza.Epico));
        }

        static float GetWeight(ItemPackData pack, Rareza rarity)
        {
            PackRarityWeight weight = pack.rarityWeights.FirstOrDefault(value => value.rarity == rarity);
            return weight != null ? weight.weight : 0f;
        }

        static void DestroyItems(IEnumerable<ArticuloData> items)
        {
            foreach (ArticuloData item in items)
                Object.DestroyImmediate(item);
        }

        sealed class ShopFixture
        {
            public GameObject root;
            public ShopManager shop;
            public GameManager gameManager;
            public BattleManager battle;
            public GameObject shopPanel;
            public GameObject packPanel;
            public GameObject packContent;
            public GameObject choicePrefab;
            public GameObject packPrefab;
            public GameObject cardPanel;
            public CardSelectionUI cardSelection;
            public ItemPackSelectionUI packSelection;

            public void Destroy()
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}
