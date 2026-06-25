using System.Collections.Generic;
using JuegoDeCartas.Articulos;
using JuegoDeCartas.Challenges;
using JuegoDeCartas.Characters;
using JuegoDeCartas.Enemies;
using JuegoDeCartas.Managers;
using JuegoDeCartas.Progression;
using JuegoDeCartas.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace JuegoDeCartas.Tests
{
    public class ChallengeModeTests
    {
        bool wasTemporary;
        int previousSlot;

        [SetUp]
        public void SetUp()
        {
            wasTemporary = ProfileManager.IsTemporary;
            previousSlot = ProfileManager.ActiveSlot;
            ProfileManager.Load(previousSlot);
            ChallengeRunState.Clear();
            CharacterRunState.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            ChallengeRunState.Clear();
            CharacterRunState.Clear();
            if (wasTemporary)
                ProfileManager.LoadTemporary();
            else
                ProfileManager.Load(previousSlot);
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
            ChallengeRunState.ConfigureChallenge(challenge);
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
                "Assets/_Project/Data/Characters/Caballero.asset"
            );
            CharacterData mage = AssetDatabase.LoadAssetAtPath<CharacterData>(
                "Assets/_Project/Data/Characters/Mago.asset"
            );
            ChallengeData challenge = ScriptableObject.CreateInstance<ChallengeData>();
            challenge.modifier = ChallengeModifier.SingleClass;
            challenge.requiredCharacter = knight;

            ChallengeRunState.ConfigureChallenge(challenge);

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
            ChallengeRunState.ConfigureChallenge(challenge);

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
        public void ChallengeAssetsAreSpecialMissionsAndSeedIsSeparate()
        {
            ChallengeData bosses = LoadChallenge("JefesConsecutivos");
            ChallengeData noShop = LoadChallenge("SinTienda");
            ChallengeData blood = LoadChallenge("SangrePorPoder");
            ChallengeData unstableDeck = LoadChallenge("MazoInestable");
            ChallengeData interest = LoadChallenge("InteresCompuesto");
            ChallengeData sealedPack = LoadChallenge("SobreSellado");
            ChallengeData enemyEchoes = LoadChallenge("EcosDelEnemigo");

            Assert.AreEqual(ChallengeModifier.BossRush, bosses.modifier);
            Assert.AreEqual(ChallengeDifficulty.Dificil, bosses.difficulty);
            Assert.AreEqual(4, bosses.combatCountOverride);
            Assert.NotNull(bosses.encounterMission);
            Assert.AreEqual(ChallengeModifier.NoShop, noShop.modifier);
            Assert.AreEqual(ChallengeDifficulty.Media, noShop.difficulty);
            Assert.NotNull(noShop.encounterMission);
            Assert.IsTrue(bosses.IsPlayable);
            Assert.IsTrue(noShop.IsPlayable);
            Assert.IsFalse(blood.IsPlayable);
            Assert.IsFalse(unstableDeck.IsPlayable);
            Assert.IsFalse(interest.IsPlayable);
            Assert.IsFalse(sealedPack.IsPlayable);
            Assert.IsFalse(enemyEchoes.IsPlayable);
            Assert.IsNull(AssetDatabase.LoadAssetAtPath<ChallengeData>(
                "Assets/_Project/Data/Challenges/SoloCaballero.asset"
            ));
            Assert.IsNull(AssetDatabase.LoadAssetAtPath<ChallengeData>(
                "Assets/_Project/Data/Challenges/SemillaLibre.asset"
            ));
        }

        [Test]
        public void SeededRunDoesNotBecomeAChallenge()
        {
            ChallengeRunState.ConfigureSeededRun("RUN-42");

            Assert.IsTrue(ChallengeRunState.IsSeededRun);
            Assert.IsFalse(ChallengeRunState.IsChallengeRun);
            Assert.IsNull(ChallengeRunState.SelectedChallenge);
            Assert.AreEqual("RUN-42", ChallengeRunState.SeedCode);
        }

        [Test]
        public void ChallengeCompletionIsStoredPerCharacter()
        {
            CharacterData knight = ScriptableObject.CreateInstance<CharacterData>();
            knight.name = "TestKnight";
            CharacterData mage = ScriptableObject.CreateInstance<CharacterData>();
            mage.name = "TestMage";
            ChallengeData challenge = ScriptableObject.CreateInstance<ChallengeData>();
            challenge.name = "TestChallenge_" + System.Guid.NewGuid().ToString("N");

            challenge.MarkCompleted(knight);

            Assert.IsTrue(challenge.IsCompleted);
            Assert.IsTrue(challenge.IsCompletedByCharacter(knight));
            Assert.IsFalse(challenge.IsCompletedByCharacter(mage));

            ProfilePrefs.DeleteKey("ChallengeCompleted_" + challenge.name);
            ProfilePrefs.DeleteKey("ChallengeCompleted_" + challenge.name + "_" + knight.name);
            ProfilePrefs.Save();
            Object.DestroyImmediate(knight);
            Object.DestroyImmediate(mage);
            Object.DestroyImmediate(challenge);
        }

        [Test]
        public void ConceptChallengesDoNotCountAsCompletion()
        {
            CharacterData hero = ScriptableObject.CreateInstance<CharacterData>();
            ChallengeData challenge = ScriptableObject.CreateInstance<ChallengeData>();
            challenge.implemented = false;

            challenge.MarkCompleted(hero);

            Assert.IsFalse(challenge.IsCompleted);
            Assert.IsFalse(challenge.IsCompletedByCharacter(hero));
            Assert.IsFalse(challenge.CountsForCompletion);
            Assert.IsFalse(challenge.IsPlayable);

            Object.DestroyImmediate(hero);
            Object.DestroyImmediate(challenge);
        }

        static ChallengeData LoadChallenge(string name)
        {
            ChallengeData challenge = AssetDatabase.LoadAssetAtPath<ChallengeData>(
                "Assets/_Project/Data/Challenges/" + name + ".asset"
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
