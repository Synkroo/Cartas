using UnityEngine;
using JuegoDeCartas.Articulos;
using JuegoDeCartas.Characters;
using JuegoDeCartas.Enemies;
using JuegoDeCartas.Stats;
using JuegoDeCartas.Cards;
using JuegoDeCartas.Relics;

namespace JuegoDeCartas.Progression
{
    public static class CollectionProgress
    {
        const string Prefix = "Collection_";

        public static void MarkEnemySeen(EnemyData enemy)
        {
            SetFlag("EnemySeen", enemy);
        }

        public static void MarkEnemyDefeated(EnemyData enemy)
        {
            if (enemy == null)
                return;

            MarkEnemySeen(enemy);
            Add("EnemyDefeated", enemy, 1);
            ProfilePrefs.Save();
        }

        public static bool IsEnemySeen(EnemyData enemy) => GetFlag("EnemySeen", enemy);
        public static int GetEnemyDefeatedCount(EnemyData enemy) =>
            ProfileManager.IsTemporary ? 99 : GetInt("EnemyDefeated", enemy);

        public static void MarkItemSeen(ArticuloData item)
        {
            SetFlag("ItemSeen", item);
        }

        public static void MarkItemUsed(ArticuloData item)
        {
            if (item == null)
                return;

            MarkItemSeen(item);
            Add("ItemUsed", item, 1);
            ProfilePrefs.Save();
        }

        public static bool IsItemSeen(ArticuloData item) => GetFlag("ItemSeen", item);
        public static int GetItemUsedCount(ArticuloData item) =>
            ProfileManager.IsTemporary ? 25 : GetInt("ItemUsed", item);

        public static void MarkPackSeen(ItemPackData pack)
        {
            SetFlag("PackSeen", pack);
        }

        public static bool IsPackSeen(ItemPackData pack) => GetFlag("PackSeen", pack);

        public static void MarkRelicSeen(RelicData relic)
        {
            SetFlag("RelicSeen", relic);
        }

        public static void MarkRelicAcquired(RelicData relic)
        {
            if (relic == null)
                return;

            MarkRelicSeen(relic);
            Add("RelicAcquired", relic, 1);
            ProfilePrefs.Save();
        }

        public static bool IsRelicSeen(RelicData relic) =>
            GetFlag("RelicSeen", relic);

        public static int GetRelicAcquiredCount(RelicData relic) =>
            ProfileManager.IsTemporary ? 25 : GetInt("RelicAcquired", relic);

        public static void MarkSubclassSeen(SubclassData subclass)
        {
            SetFlag("SubclassSeen", subclass);
        }

        public static bool IsSubclassSeen(SubclassData subclass) => GetFlag("SubclassSeen", subclass);

        public static void MarkCardUsed(CardData card)
        {
            if (card == null)
                return;

            Add("CardUsed", card, 1);
            ProfilePrefs.Save();
        }

        public static int GetCardUsedCount(CardData card) =>
            ProfileManager.IsTemporary ? 999 : GetInt("CardUsed", card);

        public static void MarkEpiphanySeen(CardData card, int optionIndex)
        {
            if (card == null || optionIndex < 0 ||
                optionIndex >= card.GetEpiphanyOptions().Count)
            {
                return;
            }

            ProfilePrefs.SetInt(EpiphanyKey(card, optionIndex), 1);
            ProfilePrefs.Save();
        }

        public static bool IsEpiphanySeen(CardData card, int optionIndex)
        {
            return card != null &&
                   optionIndex >= 0 &&
                   optionIndex < card.GetEpiphanyOptions().Count &&
                   (ProfileManager.IsTemporary ||
                    ProfilePrefs.GetInt(EpiphanyKey(card, optionIndex), 0) == 1);
        }

        public static string GetEpiphanyCollectionId(CardData card, int optionIndex)
        {
            return card != null ? card.name + "_" + optionIndex : "";
        }

        public static void RegisterRunStarted(CharacterData character)
        {
            Add("HeroRuns", character, 1);
            ProfilePrefs.Save();
        }

        public static void RegisterRunFinished(CharacterData character, GameStatsTracker stats, bool completed)
        {
            if (character == null)
                return;

            if (completed)
                Add("HeroWins", character, 1);

            if (stats != null)
            {
                SetMaximum("HeroMaxCardDamage", character, stats.maxCardDamage);
                SetMaximum("HeroMaxRunDamage", character, stats.totalDamageDealt);
                SetMaximum("HeroMaxArmor", character, stats.maxArmor);
                Add("HeroEnemiesDefeated", character, stats.enemiesDefeated);
                Add("HeroCardsUsed", character, stats.cardsUsed);
            }

            ProfilePrefs.Save();
        }

        public static int GetHeroRuns(CharacterData character) => ProfileManager.IsTemporary ? 50 : GetInt("HeroRuns", character);
        public static int GetHeroWins(CharacterData character) => ProfileManager.IsTemporary ? 50 : GetInt("HeroWins", character);
        public static int GetHeroMaxCardDamage(CharacterData character) => ProfileManager.IsTemporary ? 999 : GetInt("HeroMaxCardDamage", character);
        public static int GetHeroMaxRunDamage(CharacterData character) => ProfileManager.IsTemporary ? 9999 : GetInt("HeroMaxRunDamage", character);
        public static int GetHeroMaxArmor(CharacterData character) => ProfileManager.IsTemporary ? 999 : GetInt("HeroMaxArmor", character);
        public static int GetHeroEnemiesDefeated(CharacterData character) => ProfileManager.IsTemporary ? 999 : GetInt("HeroEnemiesDefeated", character);
        public static int GetHeroCardsUsed(CharacterData character) => ProfileManager.IsTemporary ? 999 : GetInt("HeroCardsUsed", character);

        static void SetFlag(string category, Object asset)
        {
            if (asset != null)
                ProfilePrefs.SetInt(Key(category, asset), 1);
        }

        static bool GetFlag(string category, Object asset)
        {
            return asset != null &&
                   (ProfileManager.IsTemporary || ProfilePrefs.GetInt(Key(category, asset), 0) == 1);
        }

        static void Add(string category, Object asset, int amount)
        {
            if (asset == null || amount <= 0)
                return;

            string key = Key(category, asset);
            ProfilePrefs.SetInt(key, ProfilePrefs.GetInt(key, 0) + amount);
        }

        static void SetMaximum(string category, Object asset, int value)
        {
            if (asset != null && value > GetInt(category, asset))
                ProfilePrefs.SetInt(Key(category, asset), value);
        }

        static int GetInt(string category, Object asset)
        {
            return asset != null ? ProfilePrefs.GetInt(Key(category, asset), 0) : 0;
        }

        static string Key(string category, Object asset)
        {
            return Prefix + category + "_" + asset.name;
        }

        static string EpiphanyKey(CardData card, int optionIndex)
        {
            return Prefix + "EpiphanySeen_" +
                   GetEpiphanyCollectionId(card, optionIndex);
        }
    }
}
