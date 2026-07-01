using JuegoDeCartas.Challenges;
using JuegoDeCartas.Characters;
using JuegoDeCartas.Managers;
using JuegoDeCartas.Missions;
using NUnit.Framework;
using UnityEngine;

namespace JuegoDeCartas.Tests
{
    public class RunStateLifecycleTests
    {
        [TearDown]
        public void TearDown()
        {
            RunStateCoordinator.Reset();
        }

        [Test]
        public void ResetClearsEveryStaticRunSelectionAndRandomSeed()
        {
            CharacterData character = ScriptableObject.CreateInstance<CharacterData>();
            SubclassData subclass = ScriptableObject.CreateInstance<SubclassData>();
            MissionData mission = ScriptableObject.CreateInstance<MissionData>();
            ChallengeData challenge = ScriptableObject.CreateInstance<ChallengeData>();

            subclass.character = character;
            character.subclasses.Add(subclass);

            CharacterRunState.Select(character);
            Assert.IsTrue(CharacterRunState.SelectSubclass(subclass));
            MissionRunState.SelectMission(mission, MissionDifficulty.Letal);
            ChallengeRunState.ConfigureChallenge(challenge);
            RunRandom.Initialize(12345);

            RunStateCoordinator.Reset();

            Assert.IsFalse(CharacterRunState.HasCharacter);
            Assert.IsFalse(CharacterRunState.HasSubclass);
            Assert.IsFalse(MissionRunState.HasMission);
            Assert.AreEqual(MissionDifficulty.Facil, MissionRunState.SelectedDifficulty);
            Assert.IsFalse(ChallengeRunState.HasConfiguredRun);
            Assert.IsFalse(RunRandom.IsInitialized);
            Assert.AreEqual(0, RunRandom.CurrentSeed);

            Object.DestroyImmediate(challenge);
            Object.DestroyImmediate(mission);
            Object.DestroyImmediate(subclass);
            Object.DestroyImmediate(character);
        }

        [Test]
        public void ResetClearsSeededRunConfiguration()
        {
            ChallengeRunState.ConfigureSeededRun("TEST-123");
            ChallengeRunState.PrepareRun();

            Assert.IsTrue(ChallengeRunState.IsSeededRun);
            Assert.IsTrue(RunRandom.IsInitialized);

            RunStateCoordinator.Reset();

            Assert.IsFalse(ChallengeRunState.IsSeededRun);
            Assert.IsEmpty(ChallengeRunState.SeedCode);
            Assert.IsFalse(RunRandom.IsInitialized);
        }
    }
}
