using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using JuegoDeCartas.Articulos;
using JuegoDeCartas.Characters;
using JuegoDeCartas.Cards;
using JuegoDeCartas.Challenges;
using JuegoDeCartas.Enemies;
using JuegoDeCartas.Missions;
using JuegoDeCartas.Progression;
using JuegoDeCartas.Relics;

namespace JuegoDeCartas.UI
{
    public class ProfileMenu : MonoBehaviour
    {
        public GameObject panel;
        public MainMenuManager mainMenuManager;
        public ProfileSlotUI[] slots = new ProfileSlotUI[ProfileManager.SlotCount];
        public Button temporaryProfileButton;
        public Button backButton;
        public TextMeshProUGUI activeProfileText;

        [Header("Completion Content")]
        public List<EnemyData> enemies = new List<EnemyData>();
        public List<CharacterData> heroes = new List<CharacterData>();
        public List<ArticuloData> items = new List<ArticuloData>();
        public List<ItemPackData> packs = new List<ItemPackData>();
        public List<CardData> cards = new List<CardData>();
        public List<RelicData> relics = new List<RelicData>();
        public List<MissionData> missions = new List<MissionData>();
        public List<ChallengeData> challenges = new List<ChallengeData>();

        void Awake()
        {
            if (panel == null)
                panel = gameObject;
            if (temporaryProfileButton != null)
                temporaryProfileButton.onClick.AddListener(LoadTemporary);
            if (backButton != null)
                backButton.onClick.AddListener(Close);
        }

        void OnEnable()
        {
            ProfileManager.ProfileChanged += Refresh;
        }

        void OnDisable()
        {
            ProfileManager.ProfileChanged -= Refresh;
        }

        public void Open()
        {
            if (panel == null)
                return;
            panel.SetActive(true);
            Refresh();
        }

        public void Close()
        {
            if (panel != null)
                panel.SetActive(false);
            if (mainMenuManager != null)
                mainMenuManager.ShowMainMenu();
        }

        public void Refresh()
        {
            if (activeProfileText != null)
                activeProfileText.text = "Perfil activo: " + ProfileManager.ActiveProfileName;

            for (int i = 0; i < slots.Length && i < ProfileManager.SlotCount; i++)
            {
                if (slots[i] == null)
                    continue;

                ProfileInfo info = ProfileManager.GetInfo(i);
                float completion = info.Exists ? CalculateCompletionForSlot(i) : 0f;
                slots[i].slotIndex = i;
                slots[i].Setup(info, completion, Load, Delete, Rename);
            }
        }

        void Load(int slot)
        {
            ProfileManager.Load(slot);
            CharacterRunState.Clear();
            MissionRunState.Clear();
            ChallengeRunState.Clear();
        }

        void Delete(int slot)
        {
            ProfileManager.Delete(slot);
            CharacterRunState.Clear();
            MissionRunState.Clear();
            ChallengeRunState.Clear();
        }

        void Rename(int slot, string value)
        {
            ProfileManager.Rename(slot, value);
        }

        void LoadTemporary()
        {
            ProfileManager.LoadTemporary();
            CharacterRunState.Clear();
            MissionRunState.Clear();
            ChallengeRunState.Clear();
        }

        float CalculateCompletionForSlot(int slot)
        {
            int completed = 0;
            int total = enemies.Count + items.Count + packs.Count + cards.Count +
                        relics.Count + heroes.Count +
                        GetSubclassCount() + GetEpiphanyCount();

            for (int i = 0; i < enemies.Count; i++)
                if (HasCollectionFlag(slot, "EnemySeen", enemies[i])) completed++;
            for (int i = 0; i < items.Count; i++)
                if (HasCollectionFlag(slot, "ItemSeen", items[i])) completed++;
            for (int i = 0; i < packs.Count; i++)
                if (HasCollectionFlag(slot, "PackSeen", packs[i])) completed++;
            for (int i = 0; i < cards.Count; i++)
                if (GetCollectionCount(slot, "CardUsed", cards[i]) > 0) completed++;
            for (int i = 0; i < relics.Count; i++)
                if (HasCollectionFlag(slot, "RelicSeen", relics[i])) completed++;
            for (int i = 0; i < heroes.Count; i++)
                if (IsHeroUnlockedInSlot(slot, heroes[i])) completed++;
            for (int i = 0; i < heroes.Count; i++)
            {
                CharacterData hero = heroes[i];
                if (hero == null || hero.subclasses == null)
                    continue;
                for (int s = 0; s < hero.subclasses.Count; s++)
                    if (HasCollectionFlag(slot, "SubclassSeen", hero.subclasses[s])) completed++;
            }
            for (int i = 0; i < cards.Count; i++)
            {
                CardData card = cards[i];
                if (card == null)
                    continue;
                for (int e = 0; e < card.GetEpiphanyOptions().Count; e++)
                {
                    string key = "Collection_EpiphanySeen_" +
                                 CollectionProgress.GetEpiphanyCollectionId(card, e);
                    if (ProfilePrefs.GetIntForSlot(slot, key, 0) == 1)
                        completed++;
                }
            }

            total += missions.Count * heroes.Count * 3;
            for (int m = 0; m < missions.Count; m++)
            {
                MissionData mission = missions[m];
                if (mission == null)
                    continue;
                for (int h = 0; h < heroes.Count; h++)
                {
                    CharacterData hero = heroes[h];
                    if (hero == null)
                        continue;
                    for (int d = 1; d <= 3; d++)
                    {
                        string key = $"MissionCompleted_{mission.name}_{(MissionDifficulty)d}_{hero.name}";
                        if (ProfilePrefs.GetIntForSlot(slot, key, 0) == 1)
                            completed++;
                    }
                }
            }

            total += challenges.Count * heroes.Count;
            for (int c = 0; c < challenges.Count; c++)
            {
                ChallengeData challenge = challenges[c];
                if (challenge == null || !challenge.CountsForCompletion)
                    continue;

                for (int h = 0; h < heroes.Count; h++)
                {
                    CharacterData hero = heroes[h];
                    if (hero == null)
                        continue;

                    string key = $"ChallengeCompleted_{challenge.name}_{hero.name}";
                    if (ProfilePrefs.GetIntForSlot(slot, key, 0) == 1)
                        completed++;
                }
            }

            return total > 0 ? Mathf.Clamp01((float)completed / total) : 0f;
        }

        public float CalculateActiveCompletion()
        {
            if (ProfileManager.IsTemporary)
                return 1f;

            int completed = 0;
            int total = enemies.Count + items.Count + packs.Count + cards.Count +
                        relics.Count + heroes.Count +
                        GetSubclassCount() + GetEpiphanyCount();

            for (int i = 0; i < enemies.Count; i++)
                if (CollectionProgress.IsEnemySeen(enemies[i])) completed++;
            for (int i = 0; i < items.Count; i++)
                if (CollectionProgress.IsItemSeen(items[i])) completed++;
            for (int i = 0; i < packs.Count; i++)
                if (CollectionProgress.IsPackSeen(packs[i])) completed++;
            for (int i = 0; i < cards.Count; i++)
                if (CollectionProgress.GetCardUsedCount(cards[i]) > 0) completed++;
            for (int i = 0; i < relics.Count; i++)
                if (CollectionProgress.IsRelicSeen(relics[i])) completed++;
            for (int i = 0; i < heroes.Count; i++)
                if (heroes[i] != null && heroes[i].IsUnlocked) completed++;
            for (int i = 0; i < heroes.Count; i++)
            {
                CharacterData hero = heroes[i];
                if (hero == null || hero.subclasses == null)
                    continue;
                for (int s = 0; s < hero.subclasses.Count; s++)
                    if (CollectionProgress.IsSubclassSeen(hero.subclasses[s])) completed++;
            }
            for (int i = 0; i < cards.Count; i++)
            {
                CardData card = cards[i];
                if (card == null)
                    continue;
                for (int e = 0; e < card.GetEpiphanyOptions().Count; e++)
                    if (CollectionProgress.IsEpiphanySeen(card, e)) completed++;
            }

            int medalTotal = missions.Count * heroes.Count * 3;
            total += medalTotal;
            for (int m = 0; m < missions.Count; m++)
            {
                MissionData mission = missions[m];
                if (mission == null)
                    continue;
                for (int h = 0; h < heroes.Count; h++)
                {
                    CharacterData hero = heroes[h];
                    for (int d = 1; d <= 3; d++)
                    {
                        if (mission.IsDifficultyCompletedByCharacter((MissionDifficulty)d, hero))
                            completed++;
                    }
                }
            }

            total += challenges.Count * heroes.Count;
            for (int c = 0; c < challenges.Count; c++)
            {
                ChallengeData challenge = challenges[c];
                if (challenge == null || !challenge.CountsForCompletion)
                    continue;

                for (int h = 0; h < heroes.Count; h++)
                {
                    CharacterData hero = heroes[h];
                    if (hero != null && challenge.IsCompletedByCharacter(hero))
                        completed++;
                }
            }

            return total > 0 ? Mathf.Clamp01((float)completed / total) : 0f;
        }

        int GetSubclassCount()
        {
            int count = 0;
            for (int i = 0; i < heroes.Count; i++)
            {
                CharacterData hero = heroes[i];
                if (hero != null && hero.subclasses != null)
                    count += hero.subclasses.Count;
            }
            return count;
        }

        int GetEpiphanyCount()
        {
            int count = 0;
            for (int i = 0; i < cards.Count; i++)
            {
                if (cards[i] != null)
                    count += cards[i].GetEpiphanyOptions().Count;
            }
            return count;
        }

        static bool HasCollectionFlag(int slot, string category, Object asset)
        {
            return asset != null &&
                   ProfilePrefs.GetIntForSlot(slot, $"Collection_{category}_{asset.name}", 0) == 1;
        }

        static int GetCollectionCount(int slot, string category, Object asset)
        {
            return asset != null
                ? ProfilePrefs.GetIntForSlot(slot, $"Collection_{category}_{asset.name}", 0)
                : 0;
        }

        static bool IsHeroUnlockedInSlot(int slot, CharacterData hero)
        {
            if (hero == null)
                return false;
            if (hero.unlockedByDefault)
                return true;
            if (hero.unlockCondition is DifficultyCompletionUnlockCondition difficulty)
            {
                string key = "AnyMissionCompleted_" + difficulty.requiredDifficulty;
                return ProfilePrefs.GetIntForSlot(slot, key, 0) == 1;
            }
            return false;
        }
    }
}
