using System.Collections.Generic;
using System.Linq;
using JuegoDeCartas.Articulos;
using JuegoDeCartas.Characters;
using JuegoDeCartas.Enemies;
using JuegoDeCartas.Missions;
using JuegoDeCartas.Progression;
using JuegoDeCartas.Stats;
using JuegoDeCartas.Cards;
using JuegoDeCartas.Relics;
using JuegoDeCartas.Challenges;
using JuegoDeCartas.UI;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace JuegoDeCartas.Tests
{
    public class CollectionProgressTests
    {
        readonly Dictionary<string, int?> previousValues = new Dictionary<string, int?>();

        [TearDown]
        public void TearDown()
        {
            foreach (KeyValuePair<string, int?> entry in previousValues)
            {
                if (entry.Value.HasValue)
                    PlayerPrefs.SetInt(entry.Key, entry.Value.Value);
                else
                    PlayerPrefs.DeleteKey(entry.Key);
            }
            previousValues.Clear();
            PlayerPrefs.Save();
        }

        [Test]
        public void DiscoveryAndUsageArePersisted()
        {
            EnemyData enemy = ScriptableObject.CreateInstance<EnemyData>();
            enemy.name = "CollectionEnemyTest";
            ArticuloData item = ScriptableObject.CreateInstance<ArticuloData>();
            item.name = "CollectionItemTest";
            RelicData relic = ScriptableObject.CreateInstance<RelicData>();
            relic.name = "CollectionRelicTest";

            Track("Collection_EnemySeen_" + enemy.name);
            Track("Collection_EnemyDefeated_" + enemy.name);
            Track("Collection_ItemSeen_" + item.name);
            Track("Collection_ItemUsed_" + item.name);
            Track("Collection_RelicSeen_" + relic.name);
            Track("Collection_RelicAcquired_" + relic.name);

            Assert.IsFalse(CollectionProgress.IsEnemySeen(enemy));
            Assert.IsFalse(CollectionProgress.IsItemSeen(item));
            Assert.IsFalse(CollectionProgress.IsRelicSeen(relic));

            CollectionProgress.MarkEnemySeen(enemy);
            CollectionProgress.MarkEnemyDefeated(enemy);
            CollectionProgress.MarkItemSeen(item);
            CollectionProgress.MarkItemUsed(item);
            CollectionProgress.MarkRelicAcquired(relic);

            Assert.IsTrue(CollectionProgress.IsEnemySeen(enemy));
            Assert.AreEqual(1, CollectionProgress.GetEnemyDefeatedCount(enemy));
            Assert.IsTrue(CollectionProgress.IsItemSeen(item));
            Assert.AreEqual(1, CollectionProgress.GetItemUsedCount(item));
            Assert.IsTrue(CollectionProgress.IsRelicSeen(relic));
            Assert.AreEqual(1, CollectionProgress.GetRelicAcquiredCount(relic));

            Object.DestroyImmediate(enemy);
            Object.DestroyImmediate(item);
            Object.DestroyImmediate(relic);
        }

        [Test]
        public void CardUsageIsPersistedPerCard()
        {
            CardData card = ScriptableObject.CreateInstance<CardData>();
            card.name = "CollectionCardTest";
            Track("Collection_CardUsed_" + card.name);

            Assert.AreEqual(0, CollectionProgress.GetCardUsedCount(card));

            CollectionProgress.MarkCardUsed(card);
            CollectionProgress.MarkCardUsed(card);

            Assert.AreEqual(2, CollectionProgress.GetCardUsedCount(card));
            Object.DestroyImmediate(card);
        }

        [Test]
        public void LegacyCollectionKeyMigratesBeforeAssetRename()
        {
            CardData card = ScriptableObject.CreateInstance<CardData>();
            card.name = "LegacyCardName";
            SetContentId(card, "stable-card-id");

            string legacyKey =
                "Collection_CardUsed_LegacyCardName";
            string stableKey =
                "Collection_CardUsed_stable-card-id";
            Track(legacyKey);
            Track(stableKey);
            ProfilePrefs.SetInt(legacyKey, 7);

            Assert.AreEqual(7, CollectionProgress.GetCardUsedCount(card));
            Assert.IsTrue(
                PlayerPrefs.HasKey(ProfileManager.ScopedKey(stableKey))
            );

            card.name = "RenamedCard";
            Assert.AreEqual(7, CollectionProgress.GetCardUsedCount(card));

            Object.DestroyImmediate(card);
        }

        [Test]
        public void LegacyKeyMigratesForInactiveProfileSlot()
        {
            const int slot = 1;
            const string legacyKey = "LegacyInactiveSlotValue";
            const string stableKey = "StableInactiveSlotValue";
            string legacyScoped =
                ProfileManager.ScopedKeyForSlot(slot, legacyKey);
            string stableScoped =
                ProfileManager.ScopedKeyForSlot(slot, stableKey);
            int? previousLegacy = PlayerPrefs.HasKey(legacyScoped)
                ? PlayerPrefs.GetInt(legacyScoped)
                : null;
            int? previousStable = PlayerPrefs.HasKey(stableScoped)
                ? PlayerPrefs.GetInt(stableScoped)
                : null;

            try
            {
                PlayerPrefs.DeleteKey(stableScoped);
                PlayerPrefs.SetInt(legacyScoped, 19);

                Assert.AreEqual(
                    19,
                    ProfilePrefs.GetIntForSlotMigrating(
                        slot,
                        stableKey,
                        legacyKey,
                        0
                    )
                );
                Assert.AreEqual(19, PlayerPrefs.GetInt(stableScoped));
            }
            finally
            {
                RestoreInt(legacyScoped, previousLegacy);
                RestoreInt(stableScoped, previousStable);
            }
        }

        [Test]
        public void EpiphanyDiscoveryIsPersistedPerCardAndOption()
        {
            CardData card = ScriptableObject.CreateInstance<CardData>();
            card.name = "CollectionEpiphanyTest";
            card.epiphanyOptions = new List<CardEpiphany>
            {
                new CardEpiphany { epiphanyName = "Primera" },
                new CardEpiphany { epiphanyName = "Segunda" }
            };
            Track(
                "Collection_EpiphanySeen_" +
                CollectionProgress.GetEpiphanyCollectionId(card, 0)
            );
            Track(
                "Collection_EpiphanySeen_" +
                CollectionProgress.GetEpiphanyCollectionId(card, 1)
            );

            Assert.IsFalse(CollectionProgress.IsEpiphanySeen(card, 0));
            Assert.IsFalse(CollectionProgress.IsEpiphanySeen(card, 1));

            CollectionProgress.MarkEpiphanySeen(card, 1);

            Assert.IsFalse(CollectionProgress.IsEpiphanySeen(card, 0));
            Assert.IsTrue(CollectionProgress.IsEpiphanySeen(card, 1));
            Object.DestroyImmediate(card);
        }

        [Test]
        public void SubclassDiscoveryIsPersistedPerProfile()
        {
            SubclassData subclass = ScriptableObject.CreateInstance<SubclassData>();
            subclass.name = "CollectionSubclassTest";
            Track("Collection_SubclassSeen_" + subclass.name);

            Assert.IsFalse(CollectionProgress.IsSubclassSeen(subclass));
            CollectionProgress.MarkSubclassSeen(subclass);
            Assert.IsTrue(CollectionProgress.IsSubclassSeen(subclass));

            Object.DestroyImmediate(subclass);
        }

        [Test]
        public void ConceptChallengesDoNotReduceProfileCompletion()
        {
            CharacterData hero =
                ScriptableObject.CreateInstance<CharacterData>();
            hero.name = "CompletionHero";
            hero.unlockedByDefault = true;
            SetContentId(hero, "completion-hero");

            ChallengeData implemented =
                ScriptableObject.CreateInstance<ChallengeData>();
            implemented.name = "ImplementedChallenge";
            implemented.implemented = true;
            SetContentId(implemented, "implemented-challenge");

            ChallengeData concept =
                ScriptableObject.CreateInstance<ChallengeData>();
            concept.name = "ConceptChallenge";
            concept.implemented = false;
            SetContentId(concept, "concept-challenge");

            Track(
                "ChallengeCompleted_implemented-challenge"
            );
            Track(
                "ChallengeCompleted_implemented-challenge_completion-hero"
            );
            implemented.MarkCompleted(hero);

            GameObject root = new GameObject("ProfileCompletion");
            ProfileMenu menu = root.AddComponent<ProfileMenu>();
            menu.heroes = new List<CharacterData> { hero };
            menu.challenges = new List<ChallengeData>
            {
                implemented,
                concept
            };

            Assert.AreEqual(1f, menu.CalculateActiveCompletion());

            Object.DestroyImmediate(root);
            Object.DestroyImmediate(concept);
            Object.DestroyImmediate(implemented);
            Object.DestroyImmediate(hero);
        }

        [Test]
        public void EndGameStatsUseIndependentColumnsWithoutTabs()
        {
            GameObject gameObject = new GameObject("StatsColumns");
            GameStatsTracker stats = gameObject.AddComponent<GameStatsTracker>();
            stats.enemiesDefeated = 14;
            stats.maxEnemyDamage = 16;
            stats.totalDamageDealt = 1234;

            string primary = stats.GetPrimaryStatsText();
            string secondary = stats.GetSecondaryStatsText();

            StringAssert.Contains("Enemigos derrotados: 14", primary);
            StringAssert.Contains("DaÃ±o maximo enemigo: 16", secondary);
            Assert.IsFalse(primary.Contains("\t"));
            Assert.IsFalse(secondary.Contains("\t"));

            Object.DestroyImmediate(gameObject);
        }

        [Test]
        public void HeroRecordsKeepTotalsAndMaximums()
        {
            CharacterData hero = ScriptableObject.CreateInstance<CharacterData>();
            hero.name = "CollectionHeroTest";
            string[] categories =
            {
                "HeroRuns", "HeroWins", "HeroMaxCardDamage", "HeroMaxRunDamage",
                "HeroMaxArmor", "HeroEnemiesDefeated", "HeroCardsUsed"
            };
            for (int i = 0; i < categories.Length; i++)
                Track("Collection_" + categories[i] + "_" + hero.name);

            GameObject gameObject = new GameObject("Stats");
            GameStatsTracker stats = gameObject.AddComponent<GameStatsTracker>();
            stats.maxCardDamage = 24;
            stats.totalDamageDealt = 170;
            stats.maxArmor = 31;
            stats.enemiesDefeated = 8;
            stats.cardsUsed = 22;

            CollectionProgress.RegisterRunStarted(hero);
            CollectionProgress.RegisterRunFinished(hero, stats, true);
            stats.maxCardDamage = 12;
            stats.totalDamageDealt = 90;
            CollectionProgress.RegisterRunFinished(hero, stats, false);

            Assert.AreEqual(1, CollectionProgress.GetHeroRuns(hero));
            Assert.AreEqual(1, CollectionProgress.GetHeroWins(hero));
            Assert.AreEqual(24, CollectionProgress.GetHeroMaxCardDamage(hero));
            Assert.AreEqual(170, CollectionProgress.GetHeroMaxRunDamage(hero));
            Assert.AreEqual(31, CollectionProgress.GetHeroMaxArmor(hero));
            Assert.AreEqual(16, CollectionProgress.GetHeroEnemiesDefeated(hero));
            Assert.AreEqual(44, CollectionProgress.GetHeroCardsUsed(hero));

            Object.DestroyImmediate(gameObject);
            Object.DestroyImmediate(hero);
        }

        [Test]
        public void MissionCompletionIsStoredPerCharacter()
        {
            MissionData mission = ScriptableObject.CreateInstance<MissionData>();
            mission.name = "CharacterMedalMissionTest";
            CharacterData knight = ScriptableObject.CreateInstance<CharacterData>();
            knight.name = "CharacterMedalKnightTest";
            CharacterData mage = ScriptableObject.CreateInstance<CharacterData>();
            mage.name = "CharacterMedalMageTest";

            Track("MissionCompleted_" + mission.name + "_Media");
            Track("MissionCompleted_" + mission.name + "_Media_" + knight.name);
            Track("MissionCompleted_" + mission.name + "_Media_" + mage.name);
            Track("AnyMissionCompleted_Media");
            Track("AnyMissionCompleted_Media_" + knight.name);
            Track("AnyMissionCompleted_Media_" + mage.name);

            mission.MarkCompleted(MissionDifficulty.Media, knight);

            Assert.IsTrue(mission.IsDifficultyCompleted(MissionDifficulty.Media));
            Assert.IsTrue(mission.IsDifficultyCompletedByCharacter(MissionDifficulty.Media, knight));
            Assert.IsFalse(mission.IsDifficultyCompletedByCharacter(MissionDifficulty.Media, mage));
            Assert.IsTrue(MissionData.IsAnyDifficultyCompletedByCharacter(MissionDifficulty.Media, knight));

            Object.DestroyImmediate(mission);
            Object.DestroyImmediate(knight);
            Object.DestroyImmediate(mage);
        }

        [Test]
        public void ThemedPackOnlyGeneratesAllowedEffects()
        {
            ItemPackData pack = ScriptableObject.CreateInstance<ItemPackData>();
            pack.choiceCount = 3;
            pack.allowedEffects = new List<TipoEfectoArticulo>
            {
                TipoEfectoArticulo.CurarVida,
                TipoEfectoArticulo.RegeneracionVida
            };
            pack.rarityWeights = new List<PackRarityWeight>
            {
                new PackRarityWeight { rarity = Rareza.Comun, weight = 1f }
            };

            List<ArticuloData> pool = new List<ArticuloData>
            {
                CreateItem("Heal", TipoEfectoArticulo.CurarVida),
                CreateItem("Regen", TipoEfectoArticulo.RegeneracionVida),
                CreateItem("Mana", TipoEfectoArticulo.AumentarManaMax)
            };

            ItemPackOffer offer = ItemPackGenerator.Generate(pack, pool, 10);

            Assert.AreEqual(2, offer.Contents.Count);
            Assert.IsTrue(offer.Contents.All(item => pack.allowedEffects.Contains(item.tipoEfecto)));

            for (int i = 0; i < pool.Count; i++)
                Object.DestroyImmediate(pool[i]);
            Object.DestroyImmediate(pack);
        }

        [Test]
        public void AllNinePackDefinitionsExist()
        {
            string[] guids = AssetDatabase.FindAssets("t:ItemPackData", new[] { "Assets/_Project/Data/Packs" });
            List<ItemPackData> packs = guids
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<ItemPackData>)
                .Where(pack => pack != null)
                .ToList();

            Assert.AreEqual(9, packs.Count);
            Assert.IsTrue(packs.Any(pack => pack.packName == "Sobre de vitalidad"));
            Assert.IsTrue(packs.Any(pack => pack.packName == "Sobre arcano"));
            Assert.IsTrue(packs.Any(pack => pack.packName == "Sobre de forja"));
            Assert.IsTrue(packs.Any(pack => pack.packName == "Sobre del caos"));
            Assert.IsTrue(packs.Any(pack => pack.packName == "Sobre de epifania"));
            Assert.IsTrue(packs.Any(pack => pack.packName == "Sobre de arquetipos"));
        }

        static ArticuloData CreateItem(string name, TipoEfectoArticulo effect)
        {
            ArticuloData item = ScriptableObject.CreateInstance<ArticuloData>();
            item.name = name;
            item.tipoEfecto = effect;
            return item;
        }

        void Track(string key)
        {
            ProfileManager.Load(0);
            string scopedKey = ProfileManager.ScopedKey(key);
            previousValues[scopedKey] = PlayerPrefs.HasKey(scopedKey)
                ? PlayerPrefs.GetInt(scopedKey)
                : null;
            PlayerPrefs.DeleteKey(scopedKey);
        }

        static void SetContentId(
            StableContentData asset,
            string contentId)
        {
            typeof(StableContentData)
                .GetField(
                    "contentId",
                    BindingFlags.Instance | BindingFlags.NonPublic
                )
                .SetValue(asset, contentId);
        }

        static void RestoreInt(string key, int? value)
        {
            if (value.HasValue)
                PlayerPrefs.SetInt(key, value.Value);
            else
                PlayerPrefs.DeleteKey(key);
        }
    }
}
