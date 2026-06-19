using System.Collections.Generic;
using System.Linq;
using JuegoDeCartas.Articulos;
using JuegoDeCartas.Characters;
using JuegoDeCartas.Enemies;
using JuegoDeCartas.Missions;
using JuegoDeCartas.Progression;
using JuegoDeCartas.Stats;
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

            Track("Collection_EnemySeen_" + enemy.name);
            Track("Collection_EnemyDefeated_" + enemy.name);
            Track("Collection_ItemSeen_" + item.name);
            Track("Collection_ItemUsed_" + item.name);

            Assert.IsFalse(CollectionProgress.IsEnemySeen(enemy));
            Assert.IsFalse(CollectionProgress.IsItemSeen(item));

            CollectionProgress.MarkEnemySeen(enemy);
            CollectionProgress.MarkEnemyDefeated(enemy);
            CollectionProgress.MarkItemSeen(item);
            CollectionProgress.MarkItemUsed(item);

            Assert.IsTrue(CollectionProgress.IsEnemySeen(enemy));
            Assert.AreEqual(1, CollectionProgress.GetEnemyDefeatedCount(enemy));
            Assert.IsTrue(CollectionProgress.IsItemSeen(item));
            Assert.AreEqual(1, CollectionProgress.GetItemUsedCount(item));

            Object.DestroyImmediate(enemy);
            Object.DestroyImmediate(item);
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
        public void AllSevenPackDefinitionsExist()
        {
            string[] guids = AssetDatabase.FindAssets("t:ItemPackData", new[] { "Assets/GameData/Packs" });
            List<ItemPackData> packs = guids
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<ItemPackData>)
                .Where(pack => pack != null)
                .ToList();

            Assert.AreEqual(7, packs.Count);
            Assert.IsTrue(packs.Any(pack => pack.packName == "Sobre de vitalidad"));
            Assert.IsTrue(packs.Any(pack => pack.packName == "Sobre arcano"));
            Assert.IsTrue(packs.Any(pack => pack.packName == "Sobre de forja"));
            Assert.IsTrue(packs.Any(pack => pack.packName == "Sobre del caos"));
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
            previousValues[key] = PlayerPrefs.HasKey(key)
                ? PlayerPrefs.GetInt(key)
                : null;
            PlayerPrefs.DeleteKey(key);
        }
    }
}
