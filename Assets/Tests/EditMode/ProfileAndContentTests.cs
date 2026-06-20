using System.Linq;
using JuegoDeCartas.Cards;
using JuegoDeCartas.Characters;
using JuegoDeCartas.Progression;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using JuegoDeCartas.UI;

namespace JuegoDeCartas.Tests
{
    public class ProfileAndContentTests
    {
        const string TestKey = "Codex_ProfileIsolationTest";
        int previousSlot;
        bool wasTemporary;
        readonly bool[] previousExists = new bool[ProfileManager.SlotCount];
        readonly bool[] hadName = new bool[ProfileManager.SlotCount];
        readonly string[] previousNames = new string[ProfileManager.SlotCount];

        [SetUp]
        public void SetUp()
        {
            wasTemporary = ProfileManager.IsTemporary;
            previousSlot = ProfileManager.ActiveSlot;
            for (int i = 0; i < ProfileManager.SlotCount; i++)
            {
                string existsKey = $"Profiles_Slot_{i}_Exists";
                string nameKey = $"Profiles_Slot_{i}_Name";
                previousExists[i] = PlayerPrefs.GetInt(existsKey, i == 0 ? 1 : 0) == 1;
                hadName[i] = PlayerPrefs.HasKey(nameKey);
                previousNames[i] = PlayerPrefs.GetString(nameKey, "");
                PlayerPrefs.DeleteKey(ProfileManager.ScopedKeyForSlot(i, TestKey));
            }
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = 0; i < ProfileManager.SlotCount; i++)
            {
                PlayerPrefs.DeleteKey(ProfileManager.ScopedKeyForSlot(i, TestKey));
                PlayerPrefs.SetInt($"Profiles_Slot_{i}_Exists", previousExists[i] ? 1 : 0);
                if (hadName[i])
                    PlayerPrefs.SetString($"Profiles_Slot_{i}_Name", previousNames[i]);
                else
                    PlayerPrefs.DeleteKey($"Profiles_Slot_{i}_Name");
            }
            if (wasTemporary)
                ProfileManager.LoadTemporary();
            else
                ProfileManager.Load(previousSlot);
        }

        [Test]
        public void ProfilesKeepProgressIsolated()
        {
            ProfileManager.Load(0);
            ProfilePrefs.SetInt(TestKey, 17);
            ProfileManager.Load(1);

            Assert.AreEqual(0, ProfilePrefs.GetInt(TestKey, 0));

            ProfilePrefs.SetInt(TestKey, 29);
            ProfileManager.Load(0);
            Assert.AreEqual(17, ProfilePrefs.GetInt(TestKey, 0));
            ProfileManager.Load(1);
            Assert.AreEqual(29, ProfilePrefs.GetInt(TestKey, 0));
        }

        [Test]
        public void TemporaryProfileExposesCompleteProgressWithoutWriting()
        {
            ProfileManager.LoadTemporary();
            var pack = ScriptableObject.CreateInstance<JuegoDeCartas.Articulos.ItemPackData>();
            var subclass = ScriptableObject.CreateInstance<SubclassData>();

            ProfilePrefs.SetInt(TestKey, 42);

            Assert.IsTrue(ProfileManager.IsTemporary);
            Assert.IsTrue(CollectionProgress.IsPackSeen(pack));
            Assert.IsTrue(CollectionProgress.IsSubclassSeen(subclass));
            Assert.IsFalse(PlayerPrefs.HasKey(ProfileManager.ScopedKey(TestKey)));

            Object.DestroyImmediate(pack);
            Object.DestroyImmediate(subclass);
        }

        [Test]
        public void EveryCardHasFourConfiguredUpgrades()
        {
            string[] guids = AssetDatabase.FindAssets(
                "t:CardData",
                new[] { "Assets/Scripts/Cartas/Cartas S.O" });
            CardData[] cards = guids
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<CardData>)
                .Where(card => card != null)
                .ToArray();

            Assert.GreaterOrEqual(cards.Length, 16);
            Assert.IsTrue(cards.All(card =>
                card.upgradeOptions != null &&
                card.upgradeOptions.Count == 4 &&
                card.upgradeOptions.All(option =>
                    option != null && !string.IsNullOrWhiteSpace(option.upgradeName))));
        }

        [Test]
        public void RogueIsPlayableContentWithLethalUnlock()
        {
            CharacterData rogue = AssetDatabase.LoadAssetAtPath<CharacterData>(
                "Assets/GameData/Characters/Picaro.asset");
            DifficultyCompletionUnlockCondition condition =
                rogue.unlockCondition as DifficultyCompletionUnlockCondition;

            Assert.NotNull(rogue);
            Assert.IsFalse(rogue.comingSoon);
            Assert.AreEqual(10, rogue.startingDeck.Count);
            Assert.AreEqual(5, rogue.startingDeck.Distinct().Count());
            Assert.NotNull(condition);
            Assert.AreEqual(JuegoDeCartas.Missions.MissionDifficulty.Letal, condition.requiredDifficulty);
        }

        [Test]
        public void SubclassInfoListsUnusedSubclassesInsteadOfHidingThem()
        {
            CharacterData character = ScriptableObject.CreateInstance<CharacterData>();
            character.characterName = "Heroe";
            SubclassData first = ScriptableObject.CreateInstance<SubclassData>();
            SubclassData second = ScriptableObject.CreateInstance<SubclassData>();
            first.name = "UnusedSubclassA";
            first.subclassName = "Primera";
            first.passiveDescription = "Pasiva uno.";
            second.name = "UnusedSubclassB";
            second.subclassName = "Segunda";
            second.passiveDescription = "Pasiva dos.";
            character.subclasses = new System.Collections.Generic.List<SubclassData> { first, second };

            string content = SubclassInfoPanelUI.BuildContent(character);

            StringAssert.Contains("Primera", content);
            StringAssert.Contains("Segunda", content);
            StringAssert.Contains("Aun no usada", content);

            Object.DestroyImmediate(first);
            Object.DestroyImmediate(second);
            Object.DestroyImmediate(character);
        }

        [Test]
        public void CombatTooltipCanOpenWhenItsRootStartsInactive()
        {
            GameObject root = new GameObject("Tooltip");
            CombatTooltipUI tooltip = root.AddComponent<CombatTooltipUI>();
            tooltip.root = root;
            root.SetActive(false);

            tooltip.Show("Pasiva", "Descripcion");

            Assert.IsTrue(root.activeSelf);
            Object.DestroyImmediate(root);
        }
    }
}
