using System.Collections.Generic;
using JuegoDeCartas.Articulos;
using JuegoDeCartas.Challenges;
using JuegoDeCartas.Characters;
using JuegoDeCartas.Enemies;
using JuegoDeCartas.Managers;
using JuegoDeCartas.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace JuegoDeCartas.Tests
{
    public class ChallengeModeTests
    {
        [SetUp]
        public void SetUp()
        {
            ChallengeRunState.Clear();
            CharacterRunState.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            ChallengeRunState.Clear();
            CharacterRunState.Clear();
        }

        [Test]
        public void SameSeedProducesSameRandomSequence()
        {
            RunRandom.Initialize("RETO-123");
            int[] first =
            {
                RunRandom.Range(0, 1000),
                RunRandom.Range(0, 1000),
                RunRandom.Range(0, 1000),
                RunRandom.Range(0, 1000)
            };

            RunRandom.Initialize("reto-123");
            int[] second =
            {
                RunRandom.Range(0, 1000),
                RunRandom.Range(0, 1000),
                RunRandom.Range(0, 1000),
                RunRandom.Range(0, 1000)
            };

            CollectionAssert.AreEqual(first, second);
        }

        [Test]
        public void SeedControlsPackDefinitionAndContents()
        {
            ItemPackData common = CreatePack("Comun", Rareza.Comun, 2f);
            ItemPackData rare = CreatePack("Raro", Rareza.Raro, 1f);
            var definitions = new List<ItemPackData> { common, rare };
            var items = new List<ArticuloData>
            {
                CreateItem("A", Rareza.Comun),
                CreateItem("B", Rareza.Comun),
                CreateItem("C", Rareza.Raro)
            };

            RunRandom.Initialize("PACK-SEED");
            ItemPackData firstDefinition = ItemPackGenerator.RollDefinition(definitions);
            ItemPackOffer firstOffer = ItemPackGenerator.Generate(firstDefinition, items);

            RunRandom.Initialize("PACK-SEED");
            ItemPackData secondDefinition = ItemPackGenerator.RollDefinition(definitions);
            ItemPackOffer secondOffer = ItemPackGenerator.Generate(secondDefinition, items);

            Assert.AreSame(firstDefinition, secondDefinition);
            CollectionAssert.AreEqual(firstOffer.Contents, secondOffer.Contents);

            foreach (ArticuloData item in items)
                Object.DestroyImmediate(item);
            Object.DestroyImmediate(common);
            Object.DestroyImmediate(rare);
        }

        [Test]
        public void BossRushBuildsOnlyBossCombats()
        {
            ChallengeData challenge = ScriptableObject.CreateInstance<ChallengeData>();
            challenge.modifier = ChallengeModifier.BossRush;
            challenge.combatCountOverride = 4;
            ChallengeRunState.Configure(challenge, "BOSSES");
            ChallengeRunState.PrepareRun();

            EnemyData normal = ScriptableObject.CreateInstance<EnemyData>();
            EnemyData boss = ScriptableObject.CreateInstance<EnemyData>();
            boss.enemyTier = EnemyTier.Boss;

            WaveManager waves = new WaveManager
            {
                normalEnemies = new List<EnemyData> { normal },
                finalBoss = boss,
                totalCombats = 4
            };

            waves.Initialize();
            for (int i = 0; i < 4; i++)
            {
                Assert.NotNull(waves.enemy);
                Assert.AreSame(boss, waves.enemy.data);
                if (i < 3)
                    waves.SpawnNext();
            }

            Object.DestroyImmediate(normal);
            Object.DestroyImmediate(boss);
            Object.DestroyImmediate(challenge);
        }

        [Test]
        public void SingleClassChallengeRejectsOtherCharacters()
        {
            CharacterData knight = AssetDatabase.LoadAssetAtPath<CharacterData>(
                "Assets/GameData/Characters/Caballero.asset"
            );
            CharacterData mage = AssetDatabase.LoadAssetAtPath<CharacterData>(
                "Assets/GameData/Characters/Mago.asset"
            );
            ChallengeData challenge = ScriptableObject.CreateInstance<ChallengeData>();
            challenge.modifier = ChallengeModifier.SingleClass;
            challenge.requiredCharacter = knight;

            ChallengeRunState.Configure(challenge, "KNIGHT");

            Assert.IsTrue(ChallengeRunState.AllowsCharacter(knight));
            Assert.IsFalse(ChallengeRunState.AllowsCharacter(mage));
            Assert.AreSame(knight, ChallengeRunState.RequiredCharacter);

            Object.DestroyImmediate(challenge);
        }

        [Test]
        public void NoShopChallengeDoesNotGrantInterest()
        {
            ChallengeData challenge = ScriptableObject.CreateInstance<ChallengeData>();
            challenge.modifier = ChallengeModifier.NoShop;
            ChallengeRunState.Configure(challenge, "NO-SHOP");

            GameObject root = new GameObject("NoShopChallengeTest");
            ShopManager shop = root.AddComponent<ShopManager>();
            GameManager gameManager = root.AddComponent<GameManager>();
            BattleManager battle = root.AddComponent<BattleManager>();
            battle.deckManager = root.AddComponent<DeckManager>();
            battle.player = new Entity();
            shop.gameManager = gameManager;
            shop.battle = battle;
            shop.goldPerInterestStep = 100;
            shop.interestPerStep = 25;
            gameManager.dinero = 500;

            shop.Open();

            Assert.AreEqual(500, gameManager.dinero);
            Assert.AreEqual(0, shop.LastInterestEarned);

            Object.DestroyImmediate(root);
            Object.DestroyImmediate(challenge);
        }

        [Test]
        public void ChallengeAssetsCoverInitialModes()
        {
            ChallengeData seeded = LoadChallenge("SemillaLibre");
            ChallengeData bosses = LoadChallenge("JefesConsecutivos");
            ChallengeData noShop = LoadChallenge("SinTienda");
            ChallengeData knight = LoadChallenge("SoloCaballero");

            Assert.AreEqual(ChallengeModifier.SeededRun, seeded.modifier);
            Assert.AreEqual(ChallengeModifier.BossRush, bosses.modifier);
            Assert.AreEqual(4, bosses.combatCountOverride);
            Assert.AreEqual(ChallengeModifier.NoShop, noShop.modifier);
            Assert.AreEqual(ChallengeModifier.SingleClass, knight.modifier);
            Assert.NotNull(knight.requiredCharacter);
        }

        static ChallengeData LoadChallenge(string name)
        {
            ChallengeData challenge = AssetDatabase.LoadAssetAtPath<ChallengeData>(
                "Assets/GameData/Challenges/" + name + ".asset"
            );
            Assert.NotNull(challenge);
            return challenge;
        }

        static ItemPackData CreatePack(string name, Rareza rarity, float weight)
        {
            ItemPackData pack = ScriptableObject.CreateInstance<ItemPackData>();
            pack.packName = name;
            pack.choiceCount = 3;
            pack.shopAppearanceWeight = weight;
            pack.displayRarity = rarity;
            pack.rarityWeights = new List<PackRarityWeight>
            {
                new PackRarityWeight { rarity = rarity, weight = 1f }
            };
            return pack;
        }

        static ArticuloData CreateItem(string name, Rareza rarity)
        {
            ArticuloData item = ScriptableObject.CreateInstance<ArticuloData>();
            item.nombre = name;
            item.rareza = rarity;
            return item;
        }
    }
}
