using System.Collections.Generic;
using System.Linq;
using JuegoDeCartas.Articulos;
using JuegoDeCartas.Cards;
using JuegoDeCartas.Characters;
using JuegoDeCartas.Managers;
using JuegoDeCartas.Missions;
using JuegoDeCartas.UI;
using JuegoDeCartas.Progression;
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

            Assert.IsFalse(rogue.comingSoon);
            Assert.AreEqual(10, rogue.startingDeck.Count);
            Assert.NotNull(rogue.unlockCondition);
        }

        [Test]
        public void CompletingMediumDifficultyMeetsMageUnlockCondition()
        {
            ProfileManager.Load(0);
            const string globalKey = "AnyMissionCompleted_Media";
            string scopedGlobalKey = ProfileManager.ScopedKey(globalKey);
            bool hadGlobal = PlayerPrefs.HasKey(scopedGlobalKey);
            int previousGlobal = PlayerPrefs.GetInt(scopedGlobalKey, 0);

            MissionData mission = ScriptableObject.CreateInstance<MissionData>();
            mission.name = "CodexUnlockTestMission";
            var condition = ScriptableObject.CreateInstance<DifficultyCompletionUnlockCondition>();
            condition.requiredDifficulty = MissionDifficulty.Media;
            condition.missions = new List<MissionData> { mission };

            string missionKey = ProfileManager.ScopedKey("MissionCompleted_" + mission.name + "_Media");
            PlayerPrefs.DeleteKey(missionKey);
            PlayerPrefs.DeleteKey(scopedGlobalKey);

            Assert.IsFalse(condition.IsMet());
            mission.MarkCompleted(MissionDifficulty.Media);
            Assert.IsTrue(condition.IsMet());

            PlayerPrefs.DeleteKey(missionKey);
            if (hadGlobal)
                PlayerPrefs.SetInt(scopedGlobalKey, previousGlobal);
            else
                PlayerPrefs.DeleteKey(scopedGlobalKey);
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
        public void PlayerFramePrefersSubclassNameOverClassName()
        {
            CharacterData character = ScriptableObject.CreateInstance<CharacterData>();
            character.characterName = "Caballero";
            SubclassData subclass = ScriptableObject.CreateInstance<SubclassData>();
            subclass.subclassName = "Caballero pesado";

            Assert.AreEqual(
                "Caballero",
                UIManager.ResolveClassOrSubclassName(character, null)
            );
            Assert.AreEqual(
                "Caballero pesado",
                UIManager.ResolveClassOrSubclassName(character, subclass)
            );

            Object.DestroyImmediate(subclass);
            Object.DestroyImmediate(character);
        }

        [Test]
        public void PlayerFrameTooltipUsesSubclassPassiveOrCharacterMechanic()
        {
            CharacterData character = ScriptableObject.CreateInstance<CharacterData>();
            character.characterName = "Caballero";
            character.description = "Descripcion general.";
            character.mechanicDescription = "Equilibra ataque y defensa.";
            SubclassData subclass = ScriptableObject.CreateInstance<SubclassData>();
            subclass.subclassName = "Lider";
            subclass.description = "Descripcion de subclase.";
            subclass.passiveDescription = "Los aumentos de daño se acumulan.";

            PlayerFrameHoverTooltip.ResolveIdentityTooltip(
                character,
                null,
                out string classTitle,
                out string classDescription
            );
            PlayerFrameHoverTooltip.ResolveIdentityTooltip(
                character,
                subclass,
                out string subclassTitle,
                out string subclassDescription
            );

            Assert.AreEqual("Caballero", classTitle);
            Assert.AreEqual("Equilibra ataque y defensa.", classDescription);
            Assert.AreEqual("Lider", subclassTitle);
            Assert.AreEqual("Los aumentos de daño se acumulan.", subclassDescription);

            Object.DestroyImmediate(subclass);
            Object.DestroyImmediate(character);
        }

        [Test]
        public void EveryCharacterHasThreeOwnedSubclasses()
        {
            CharacterData[] characters =
            {
                AssetDatabase.LoadAssetAtPath<CharacterData>("Assets/GameData/Characters/Caballero.asset"),
                AssetDatabase.LoadAssetAtPath<CharacterData>("Assets/GameData/Characters/Mago.asset"),
                AssetDatabase.LoadAssetAtPath<CharacterData>("Assets/GameData/Characters/Picaro.asset")
            };

            foreach (CharacterData character in characters)
            {
                Assert.NotNull(character);
                Assert.AreEqual(3, character.subclasses.Count);
                Assert.IsTrue(character.subclasses.All(subclass =>
                    subclass != null &&
                    subclass.character == character &&
                    !string.IsNullOrWhiteSpace(subclass.subclassName) &&
                    !string.IsNullOrWhiteSpace(subclass.passiveDescription)));
                Assert.AreEqual(3, character.subclasses.Select(subclass => subclass.passiveType).Distinct().Count());
            }
        }

        [TestCase(4, 2)]
        [TestCase(5, 3)]
        [TestCase(6, 3)]
        public void SubclassSelectionCombatUsesRunMidpoint(int combats, int expected)
        {
            WaveManager waves = new WaveManager { totalCombats = combats };
            Assert.AreEqual(expected, waves.SubclassSelectionCombat);
        }

        [Test]
        public void RunCanSelectOnlyOneOwnedSubclass()
        {
            CharacterData character = ScriptableObject.CreateInstance<CharacterData>();
            SubclassData first = ScriptableObject.CreateInstance<SubclassData>();
            SubclassData second = ScriptableObject.CreateInstance<SubclassData>();
            first.character = character;
            second.character = character;
            character.subclasses = new List<SubclassData> { first, second };

            CharacterRunState.Select(character);

            Assert.IsTrue(CharacterRunState.SelectSubclass(first));
            Assert.AreSame(first, CharacterRunState.SelectedSubclass);
            Assert.IsFalse(CharacterRunState.SelectSubclass(second));
            Assert.AreSame(first, CharacterRunState.SelectedSubclass);

            Object.DestroyImmediate(first);
            Object.DestroyImmediate(second);
            Object.DestroyImmediate(character);
        }

        [Test]
        public void SubclassPassivesApplyManaBuffStackingAndArmorRetention()
        {
            CharacterData character = ScriptableObject.CreateInstance<CharacterData>();
            SubclassData channeler = ScriptableObject.CreateInstance<SubclassData>();
            channeler.character = character;
            channeler.passiveType = SubclassPassiveType.BonusMaxMana;
            channeler.amount = 1;
            character.subclasses = new List<SubclassData> { channeler };

            GameObject root = new GameObject("SubclassBattleTest");
            BattleManager battle = root.AddComponent<BattleManager>();
            battle.player = new Entity();
            battle.player.stats.maxMana = 3;
            battle.player.stats.mana = 2;
            battle.deckManager = root.AddComponent<DeckManager>();

            CharacterRunState.Select(character);
            Assert.IsTrue(battle.ActivateSubclass(channeler));
            Assert.AreEqual(4, battle.player.stats.maxMana);
            Assert.AreEqual(3, battle.player.stats.mana);

            CharacterRunState.Clear();
            SubclassData leader = ScriptableObject.CreateInstance<SubclassData>();
            leader.character = character;
            leader.passiveType = SubclassPassiveType.StackDamageBuffs;
            character.subclasses = new List<SubclassData> { leader };
            CharacterRunState.Select(character);
            Assert.IsTrue(battle.ActivateSubclass(leader));
            battle.ApplyPlayerDamageBonus(3, 2);
            battle.ApplyPlayerDamageBonus(5, 4);
            Assert.AreEqual(8, battle.playerDamageBonus);
            Assert.AreEqual(4, battle.playerDamageBonusTurnsRemaining);

            CharacterRunState.Clear();
            SubclassData bastion = ScriptableObject.CreateInstance<SubclassData>();
            bastion.character = character;
            bastion.passiveType = SubclassPassiveType.RetainArmor;
            bastion.percentage = 50;
            character.subclasses = new List<SubclassData> { bastion };
            CharacterRunState.Select(character);
            Assert.IsTrue(battle.ActivateSubclass(bastion));
            Assert.AreEqual(9, battle.GetArmorAtPlayerTurnStart(19));

            Object.DestroyImmediate(root);
            Object.DestroyImmediate(channeler);
            Object.DestroyImmediate(leader);
            Object.DestroyImmediate(bastion);
            Object.DestroyImmediate(character);
        }

        [Test]
        public void StackingSubclassRemovesNonStackingWarningFromRuntimeCardText()
        {
            CharacterData character = ScriptableObject.CreateInstance<CharacterData>();
            SubclassData leader = ScriptableObject.CreateInstance<SubclassData>();
            leader.character = character;
            leader.passiveType = SubclassPassiveType.StackDamageBuffs;
            character.subclasses = new List<SubclassData> { leader };

            CardData card = ScriptableObject.CreateInstance<CardData>();
            card.description = "Ganas 5 de daño durante 3 turnos. No se acumula.";

            GameObject root = new GameObject("RuntimeDescriptionTest");
            BattleManager battle = root.AddComponent<BattleManager>();
            battle.player = new Entity();
            battle.deckManager = root.AddComponent<DeckManager>();

            CharacterRunState.Select(character);
            Assert.IsTrue(battle.ActivateSubclass(leader));

            string description = battle.GetRuntimeCardDescription(card);
            Assert.AreEqual("Ganas 5 de daño durante 3 turnos.", description);
            StringAssert.DoesNotContain("No se acumula", description);

            Object.DestroyImmediate(root);
            Object.DestroyImmediate(card);
            Object.DestroyImmediate(leader);
            Object.DestroyImmediate(character);
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
            ItemPackData epiphany = LoadPack("SobreEpifania");
            ItemPackData archetypes = LoadPack("SobreArquetipos");

            AssertPack(common, 200, 3, 24f, 85f, 15f, 0f);
            AssertPack(rare, 400, 4, 12f, 15f, 75f, 10f);
            AssertPack(epic, 800, 5, 4f, 0f, 25f, 75f);
            AssertPack(vitality, 250, 4, 20f, 60f, 35f, 5f);
            AssertPack(arcane, 350, 3, 16f, 45f, 45f, 10f);
            AssertPack(forge, 450, 3, 14f, 10f, 65f, 25f);
            AssertPack(chaos, 550, 5, 10f, 20f, 45f, 35f);
            AssertPack(epiphany, 600, 1, 18f, 0f, 0f, 100f);
            AssertPack(archetypes, 500, 1, 18f, 0f, 0f, 100f);
        }

        [Test]
        public void ThematicPacksAllowEpiphanyAndCrossClassItems()
        {
            ItemPackData arcane = LoadPack("SobreArcano");
            ItemPackData forge = LoadPack("SobreForja");
            ArticuloData epiphany = AssetDatabase.LoadAssetAtPath<ArticuloData>(
                "Assets/Scripts/Articulos/Articulos S.O/DespertarEpifania.asset"
            );
            ArticuloData crossClass = AssetDatabase.LoadAssetAtPath<ArticuloData>(
                "Assets/Scripts/Articulos/Articulos S.O/PortalDeArquetipos.asset"
            );

            Assert.NotNull(epiphany);
            Assert.NotNull(crossClass);
            Assert.IsTrue(forge.Allows(epiphany));
            Assert.IsTrue(arcane.Allows(crossClass));
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
            Assert.IsTrue(fixture.cardPanel.activeInHierarchy);
            Assert.IsFalse(fixture.shopContent.activeSelf);

            fixture.cardSelection.Close();

            Assert.IsTrue(fixture.packPanel.activeSelf);
            Assert.IsFalse(fixture.cardPanel.activeSelf);
            Assert.IsFalse(fixture.shopContent.activeSelf);
            Assert.IsFalse(offer.Claimed);

            fixture.Destroy();
            Object.DestroyImmediate(duplicate);
            Object.DestroyImmediate(offer.Definition);
            Object.DestroyImmediate(cardData);
        }

        [Test]
        public void CardSelectionClosesBeforeInvokingSelectionCallback()
        {
            ShopFixture fixture = CreateShopFixture(true);
            CardData cardData = ScriptableObject.CreateInstance<CardData>();
            Card card = new Card(cardData);
            bool callbackSawClosedPanel = false;

            fixture.cardSelection.OpenForSelection(
                new List<Card> { card },
                "Seleccion",
                _ => callbackSawClosedPanel = !fixture.cardPanel.activeInHierarchy,
                null);

            Button cardButton = fixture.cardSelection.contentParent.GetComponentInChildren<Button>();
            Assert.NotNull(cardButton);
            cardButton.onClick.Invoke();

            Assert.IsTrue(callbackSawClosedPanel);
            Assert.IsFalse(fixture.cardPanel.activeSelf);

            fixture.Destroy();
            Object.DestroyImmediate(cardData);
        }

        [Test]
        public void UpgradePackFlowCompletesWithoutLeavingHiddenSelection()
        {
            ShopFixture fixture = CreateShopFixture(true);
            ConfigureUpgradeSelection(fixture);
            fixture.gameManager.dinero = 300;

            CardData cardData = ScriptableObject.CreateInstance<CardData>();
            cardData.cardName = "Carta";
            cardData.cost = 2;
            cardData.upgradeOptions = new List<CardUpgradeOption>
            {
                new CardUpgradeOption { upgradeName = "Mejora", costReduction = 1 }
            };
            Card card = new Card(cardData);
            fixture.battle.deckManager.hand.Add(card);

            ArticuloData upgrade = CreateItem("Mejorar", Rareza.Raro);
            upgrade.tipoEfecto = TipoEfectoArticulo.MejorarCarta;
            ItemPackOffer offer = CreateOffer(upgrade, 200);

            Assert.IsTrue(fixture.shop.TryPurchasePack(offer));
            Assert.IsTrue(fixture.shop.TryClaimItem(offer, upgrade));
            Assert.IsTrue(fixture.cardPanel.activeInHierarchy);

            Button cardButton = fixture.cardSelection.contentParent.GetComponentInChildren<Button>();
            Assert.NotNull(cardButton);
            cardButton.onClick.Invoke();

            Assert.IsFalse(fixture.cardPanel.activeSelf);
            Assert.IsTrue(fixture.upgradePanel.activeInHierarchy);

            fixture.upgradeSelection.upgradeButtons[0].onClick.Invoke();

            Assert.IsTrue(card.upgraded);
            Assert.AreEqual(0, card.selectedUpgradeIndex);
            Assert.IsTrue(offer.Claimed);
            Assert.IsFalse(fixture.upgradePanel.activeSelf);
            Assert.IsTrue(fixture.shopContent.activeSelf);

            fixture.Destroy();
            Object.DestroyImmediate(upgrade);
            Object.DestroyImmediate(offer.Definition);
            Object.DestroyImmediate(cardData);
        }

        [Test]
        public void CancellingUpgradeReturnsToPackWithoutClaimingOrChangingCard()
        {
            ShopFixture fixture = CreateShopFixture(true);
            ConfigureUpgradeSelection(fixture);
            fixture.gameManager.dinero = 300;

            CardData cardData = ScriptableObject.CreateInstance<CardData>();
            cardData.cardName = "Carta";
            cardData.cost = 2;
            cardData.upgradeOptions = new List<CardUpgradeOption>
            {
                new CardUpgradeOption { upgradeName = "Mejora", costReduction = 1 }
            };
            Card card = new Card(cardData);
            fixture.battle.deckManager.hand.Add(card);

            ArticuloData upgrade = CreateItem("MejorarCancelado", Rareza.Raro);
            upgrade.tipoEfecto = TipoEfectoArticulo.MejorarCarta;
            ItemPackOffer offer = CreateOffer(upgrade, 200);

            Assert.IsTrue(fixture.shop.TryPurchasePack(offer));
            Assert.IsTrue(fixture.shop.TryClaimItem(offer, upgrade));
            fixture.cardSelection.contentParent.GetComponentInChildren<Button>().onClick.Invoke();
            Assert.IsTrue(fixture.upgradePanel.activeSelf);

            Assert.IsTrue(
                fixture.upgradeSelection.cancelButton.gameObject.activeSelf
            );
            fixture.upgradeSelection.cancelButton.onClick.Invoke();

            Assert.IsFalse(fixture.upgradePanel.activeSelf);
            Assert.IsTrue(fixture.packPanel.activeSelf);
            Assert.IsFalse(offer.Claimed);
            Assert.IsFalse(card.upgraded);
            Assert.AreEqual(-1, card.selectedUpgradeIndex);

            fixture.Destroy();
            Object.DestroyImmediate(upgrade);
            Object.DestroyImmediate(offer.Definition);
            Object.DestroyImmediate(cardData);
        }

        [Test]
        public void EpiphanyChoiceCanGoBackAndSelectAConfiguredOptionWithoutRefund()
        {
            ShopFixture fixture = CreateShopFixture(true);
            ConfigureUpgradeSelection(fixture, 2);
            fixture.gameManager.dinero = 300;

            CardData cardData = ScriptableObject.CreateInstance<CardData>();
            cardData.cardName = "Carta";
            cardData.epiphanyOptions = new List<CardEpiphany>
            {
                new CardEpiphany
                {
                    epiphanyName = "Primera",
                    description = "Efecto uno."
                },
                new CardEpiphany
                {
                    epiphanyName = "Segunda",
                    description = "Efecto dos."
                }
            };
            Card card = new Card(cardData);
            fixture.battle.deckManager.hand.Add(card);

            ArticuloData epiphany = CreateItem("Epifania", Rareza.Epico);
            epiphany.tipoEfecto = TipoEfectoArticulo.DespertarEpifania;
            ItemPackOffer offer = CreateOffer(epiphany, 200);

            Assert.IsTrue(fixture.shop.TryPurchasePack(offer));
            Assert.AreEqual(100, fixture.gameManager.dinero);
            Assert.IsTrue(fixture.shop.TryClaimItem(offer, epiphany));
            fixture.cardSelection.contentParent
                .GetComponentInChildren<Button>()
                .onClick.Invoke();

            Assert.IsTrue(fixture.upgradePanel.activeSelf);
            Assert.IsTrue(
                fixture.upgradeSelection.upgradeButtons[1].gameObject.activeSelf
            );
            Assert.IsTrue(
                fixture.upgradeSelection.cancelButton.gameObject.activeSelf
            );

            fixture.upgradeSelection.cancelButton.onClick.Invoke();

            Assert.IsTrue(fixture.packPanel.activeSelf);
            Assert.IsFalse(fixture.cardPanel.activeSelf);
            Assert.AreEqual(100, fixture.gameManager.dinero);
            Assert.IsFalse(offer.Claimed);
            Assert.IsFalse(card.epiphanyUnlocked);

            Assert.IsTrue(fixture.shop.TryClaimItem(offer, epiphany));
            fixture.cardSelection.contentParent
                .GetComponentInChildren<Button>()
                .onClick.Invoke();
            fixture.upgradeSelection.upgradeButtons[1].onClick.Invoke();

            Assert.IsTrue(card.epiphanyUnlocked);
            Assert.AreEqual(1, card.selectedEpiphanyIndex);
            Assert.AreEqual("Segunda", card.Epiphany.epiphanyName);
            Assert.IsTrue(offer.Claimed);
            Assert.AreEqual(100, fixture.gameManager.dinero);

            fixture.Destroy();
            Object.DestroyImmediate(epiphany);
            Object.DestroyImmediate(offer.Definition);
            Object.DestroyImmediate(cardData);
        }

        [Test]
        public void SingleEpiphanyIsCenteredAndRestoresTheRegularGrid()
        {
            ShopFixture fixture = CreateShopFixture(true);
            ConfigureUpgradeSelection(fixture, 2);
            RectTransform first = (RectTransform)fixture
                .upgradeSelection.upgradeButtons[0].transform;
            RectTransform second = (RectTransform)fixture
                .upgradeSelection.upgradeButtons[1].transform;
            first.anchorMin = new Vector2(0.08f, 0.48f);
            first.anchorMax = new Vector2(0.47f, 0.82f);
            second.anchorMin = new Vector2(0.53f, 0.48f);
            second.anchorMax = new Vector2(0.92f, 0.82f);

            CardData data = ScriptableObject.CreateInstance<CardData>();
            data.epiphanyOptions = new List<CardEpiphany>
            {
                new CardEpiphany { epiphanyName = "Unica" }
            };
            Card card = new Card(data);

            Assert.IsTrue(
                fixture.upgradeSelection.ShowEpiphanies(card, () => { })
            );
            Assert.AreEqual(
                fixture.upgradeSelection.singleOptionAnchorMin,
                first.anchorMin
            );
            Assert.AreEqual(
                fixture.upgradeSelection.singleOptionAnchorMax,
                first.anchorMax
            );
            Assert.IsFalse(
                fixture.upgradeSelection.upgradeButtons[1].gameObject.activeSelf
            );
            Assert.IsFalse(
                fixture.upgradeSelection.cancelButton.gameObject.activeSelf
            );

            fixture.upgradeSelection.Cancel();

            Assert.AreEqual(new Vector2(0.08f, 0.48f), first.anchorMin);
            Assert.AreEqual(new Vector2(0.47f, 0.82f), first.anchorMax);

            fixture.Destroy();
            Object.DestroyImmediate(data);
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

        [Test]
        public void RestockKeepsReservedPackAndReplacesOtherOffers()
        {
            ShopFixture fixture = CreateShopFixture();
            fixture.shop.packPrefab = fixture.packPrefab;
            fixture.shop.packDefinitions = new List<ItemPackData>
            {
                CreatePackDefinition("Comun", 1)
            };
            fixture.shop.itemPool = new List<ArticuloData>
            {
                CreateItem("A", Rareza.Comun),
                CreateItem("B", Rareza.Comun),
                CreateItem("C", Rareza.Comun)
            };
            fixture.shop.slotContainers = new[]
            {
                new GameObject("Slot1").transform,
                new GameObject("Slot2").transform,
                new GameObject("Slot3").transform
            };
            for (int i = 0; i < fixture.shop.slotContainers.Length; i++)
                fixture.shop.slotContainers[i].SetParent(fixture.root.transform);

            fixture.shop.restockCost = 50;
            fixture.gameManager.dinero = 500;
            Assert.IsTrue(fixture.shop.TryRestock());

            ItemPackOffer reserved = fixture.shop.CurrentOffers[0];
            ItemPackOffer replaced = fixture.shop.CurrentOffers[1];
            Assert.IsTrue(fixture.shop.ToggleReservation(reserved));
            Assert.IsTrue(reserved.Reserved);

            Assert.IsTrue(fixture.shop.TryRestock());

            Assert.AreSame(reserved, fixture.shop.CurrentOffers[0]);
            Assert.IsTrue(fixture.shop.CurrentOffers[0].Reserved);
            Assert.AreNotSame(replaced, fixture.shop.CurrentOffers[1]);
            Assert.AreEqual(400, fixture.gameManager.dinero);

            Object.DestroyImmediate(fixture.shop.packDefinitions[0]);
            DestroyItems(fixture.shop.itemPool);
            fixture.Destroy();
        }

        [Test]
        public void RestockKeepsReservedPackGameObjectWithoutReplayingEntrance()
        {
            ShopFixture fixture = CreateShopFixture();
            fixture.shop.packPrefab = fixture.packPrefab;
            fixture.shop.packDefinitions = new List<ItemPackData>
            {
                CreatePackDefinition("Comun", 1)
            };
            fixture.shop.itemPool = new List<ArticuloData>
            {
                CreateItem("A", Rareza.Comun),
                CreateItem("B", Rareza.Comun),
                CreateItem("C", Rareza.Comun)
            };
            fixture.shop.slotContainers = new[]
            {
                new GameObject("Slot1").transform,
                new GameObject("Slot2").transform
            };
            for (int i = 0; i < fixture.shop.slotContainers.Length; i++)
                fixture.shop.slotContainers[i].SetParent(fixture.root.transform);

            fixture.shop.restockCost = 50;
            fixture.gameManager.dinero = 500;
            Assert.IsTrue(fixture.shop.TryRestock());

            ItemPackOffer reserved = fixture.shop.CurrentOffers[0];
            GameObject reservedObject = fixture.shop.slotContainers[0]
                .GetChild(0)
                .gameObject;
            Assert.IsTrue(fixture.shop.ToggleReservation(reserved));

            Assert.IsTrue(fixture.shop.TryRestock());

            Assert.AreSame(
                reservedObject,
                fixture.shop.slotContainers[0].GetChild(0).gameObject
            );
            Assert.AreEqual(1, fixture.shop.slotContainers[0].childCount);
            Assert.AreEqual(1, fixture.shop.slotContainers[1].childCount);

            Object.DestroyImmediate(fixture.shop.packDefinitions[0]);
            DestroyItems(fixture.shop.itemPool);
            fixture.Destroy();
        }

        [Test]
        public void RestockPreparesReplacedPackEntranceBeforeShowingNewContent()
        {
            ShopFixture fixture = CreateShopFixture();
            CanvasGroup prefabCanvasGroup =
                fixture.packPrefab.AddComponent<CanvasGroup>();
            UITransitionAnimator transition =
                fixture.packPrefab.AddComponent<UITransitionAnimator>();
            transition.canvasGroup = prefabCanvasGroup;
            transition.target = fixture.packPrefab.GetComponent<RectTransform>();
            fixture.packPrefab.GetComponent<ItemPackDisplay>().transition =
                transition;

            fixture.shop.packPrefab = fixture.packPrefab;
            fixture.shop.packDefinitions = new List<ItemPackData>
            {
                CreatePackDefinition("Comun", 1)
            };
            fixture.shop.itemPool = new List<ArticuloData>
            {
                CreateItem("A", Rareza.Comun),
                CreateItem("B", Rareza.Comun),
                CreateItem("C", Rareza.Comun)
            };
            fixture.shop.slotContainers = new[]
            {
                new GameObject("Slot1").transform,
                new GameObject("Slot2").transform
            };
            for (int i = 0; i < fixture.shop.slotContainers.Length; i++)
                fixture.shop.slotContainers[i].SetParent(fixture.root.transform);

            fixture.shop.restockCost = 50;
            fixture.gameManager.dinero = 500;
            Assert.IsTrue(fixture.shop.TryRestock());
            for (int i = 0; i < fixture.shop.slotContainers.Length; i++)
            {
                CanvasGroup group = fixture.shop.slotContainers[i]
                    .GetChild(0)
                    .GetComponent<CanvasGroup>();
                group.alpha = 1f;
            }

            Assert.IsTrue(fixture.shop.TryRestock());

            for (int i = 0; i < fixture.shop.slotContainers.Length; i++)
            {
                CanvasGroup group = fixture.shop.slotContainers[i]
                    .GetChild(0)
                    .GetComponent<CanvasGroup>();
                Assert.AreEqual(0f, group.alpha);
            }

            Object.DestroyImmediate(fixture.shop.packDefinitions[0]);
            DestroyItems(fixture.shop.itemPool);
            fixture.Destroy();
        }

        [Test]
        public void ReservedPackPersistsWhenShopClosesAndReopens()
        {
            ShopFixture fixture = CreateShopFixture();
            fixture.shop.pauseTime = false;
            fixture.shop.packPrefab = fixture.packPrefab;
            fixture.shop.packDefinitions = new List<ItemPackData>
            {
                CreatePackDefinition("Comun", 1)
            };
            fixture.shop.itemPool = new List<ArticuloData>
            {
                CreateItem("A", Rareza.Comun),
                CreateItem("B", Rareza.Comun),
                CreateItem("C", Rareza.Comun)
            };
            fixture.shop.slotContainers = new[]
            {
                new GameObject("Slot1").transform,
                new GameObject("Slot2").transform
            };
            for (int i = 0; i < fixture.shop.slotContainers.Length; i++)
                fixture.shop.slotContainers[i].SetParent(fixture.root.transform);

            fixture.shop.Open();
            ItemPackOffer reserved = fixture.shop.CurrentOffers[0];
            ItemPackOffer oldUnreserved = fixture.shop.CurrentOffers[1];
            GameObject reservedObject = fixture.shop.slotContainers[0]
                .GetChild(0)
                .gameObject;
            Assert.IsTrue(fixture.shop.ToggleReservation(reserved));

            fixture.shop.Close();

            Assert.AreSame(reserved, fixture.shop.CurrentOffers[0]);
            Assert.IsTrue(reserved.Reserved);
            Assert.AreSame(
                reservedObject,
                fixture.shop.slotContainers[0].GetChild(0).gameObject
            );

            fixture.shop.Open();

            Assert.AreSame(reserved, fixture.shop.CurrentOffers[0]);
            Assert.IsTrue(fixture.shop.CurrentOffers[0].Reserved);
            Assert.AreSame(
                reservedObject,
                fixture.shop.slotContainers[0].GetChild(0).gameObject
            );
            Assert.AreNotSame(oldUnreserved, fixture.shop.CurrentOffers[1]);

            Object.DestroyImmediate(fixture.shop.packDefinitions[0]);
            DestroyItems(fixture.shop.itemPool);
            fixture.Destroy();
        }

        [Test]
        public void TransitionPreparesHiddenStateBeforeFirstAnimationFrame()
        {
            GameObject root = new GameObject(
                "Transition",
                typeof(RectTransform),
                typeof(CanvasGroup),
                typeof(UITransitionAnimator)
            );
            RectTransform target = root.GetComponent<RectTransform>();
            target.anchoredPosition = new Vector2(10f, 20f);
            target.localScale = Vector3.one;
            UITransitionAnimator transition = root.GetComponent<UITransitionAnimator>();
            transition.target = target;
            transition.canvasGroup = root.GetComponent<CanvasGroup>();
            transition.hiddenOffset = new Vector2(0f, -45f);
            transition.hiddenScale = 0.92f;

            transition.PrepareForEntrance();

            Assert.AreEqual(0f, transition.canvasGroup.alpha);
            Assert.IsFalse(transition.canvasGroup.interactable);
            Assert.IsFalse(transition.canvasGroup.blocksRaycasts);
            Assert.AreEqual(new Vector2(10f, -25f), target.anchoredPosition);
            Assert.AreEqual(Vector3.one * 0.92f, target.localScale);

            Object.DestroyImmediate(root);
        }

        [Test]
        public void InterestUsesGoldThresholdsAndMaximum()
        {
            GameObject root = new GameObject("InterestTest");
            ShopManager shop = root.AddComponent<ShopManager>();
            shop.goldPerInterestStep = 100;
            shop.interestPerStep = 25;
            shop.maxInterest = 125;

            Assert.AreEqual(0, shop.CalculateInterest(99));
            Assert.AreEqual(25, shop.CalculateInterest(100));
            Assert.AreEqual(100, shop.CalculateInterest(499));
            Assert.AreEqual(125, shop.CalculateInterest(1000));

            Object.DestroyImmediate(root);
        }

        [Test]
        public void CancellingOpenedPackConsumesItWithoutApplyingAnItem()
        {
            ShopFixture fixture = CreateShopFixture();
            fixture.shop.interestPerStep = 0;
            fixture.shop.packPrefab = fixture.packPrefab;
            fixture.shop.packDefinitions = new List<ItemPackData>
            {
                CreatePackDefinition("Comun", 1)
            };
            ArticuloData heal = CreateItem("CuracionCancelada", Rareza.Comun);
            heal.tipoEfecto = TipoEfectoArticulo.CurarVida;
            heal.cantidad = 5;
            fixture.shop.itemPool = new List<ArticuloData> { heal };
            fixture.shop.slotContainers = new[]
            {
                new GameObject("Slot").transform
            };
            fixture.shop.slotContainers[0].SetParent(fixture.root.transform);
            fixture.gameManager.dinero = 300;
            fixture.battle.player.stats.maxHealth = 20;
            fixture.battle.player.stats.health = 10;

            fixture.shop.Open();
            ItemPackOffer offer = fixture.shop.CurrentOffers[0];

            Assert.IsTrue(fixture.shop.TryPurchasePack(offer));
            Assert.IsTrue(fixture.shop.CancelPack(offer));
            Assert.AreEqual(100, fixture.gameManager.dinero);
            Assert.AreEqual(10, fixture.battle.player.stats.health);
            Assert.IsTrue(offer.Claimed);
            Assert.IsTrue(fixture.shopPanel.activeSelf);

            Object.DestroyImmediate(fixture.shop.packDefinitions[0]);
            Object.DestroyImmediate(heal);
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
            fixture.shopContent = new GameObject("ShopContent");
            fixture.shopContent.transform.SetParent(fixture.shopPanel.transform);
            fixture.shop.shopContentObjects = new List<GameObject> { fixture.shopContent };

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
                fixture.cardPanel.transform.SetParent(fixture.shopPanel.transform);
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

        static void ConfigureUpgradeSelection(
            ShopFixture fixture,
            int buttonCount = 1)
        {
            fixture.upgradePanel = new GameObject("UpgradePanel");
            fixture.upgradePanel.transform.SetParent(fixture.root.transform);
            fixture.upgradePanel.SetActive(false);
            fixture.upgradeOverlay = new GameObject("UpgradeOverlay");
            fixture.upgradeOverlay.transform.SetParent(fixture.root.transform);
            fixture.upgradeOverlay.SetActive(false);

            GameObject cancelObject = new GameObject(
                "CancelUpgradeButton",
                typeof(RectTransform),
                typeof(Button)
            );
            cancelObject.transform.SetParent(fixture.upgradePanel.transform);

            fixture.upgradeSelection = fixture.root.AddComponent<UpgradeSelectionUI>();
            fixture.upgradeSelection.panel = fixture.upgradePanel;
            fixture.upgradeSelection.overlay = fixture.upgradeOverlay;
            fixture.upgradeSelection.cancelButton =
                cancelObject.GetComponent<Button>();
            fixture.upgradeSelection.upgradeButtons = new List<Button>();
            for (int i = 0; i < buttonCount; i++)
            {
                GameObject buttonObject = new GameObject(
                    "UpgradeButton" + i,
                    typeof(RectTransform),
                    typeof(Button)
                );
                buttonObject.transform.SetParent(fixture.upgradePanel.transform);
                fixture.upgradeSelection.upgradeButtons.Add(
                    buttonObject.GetComponent<Button>()
                );
            }
            fixture.shop.upgradeSelectionUI = fixture.upgradeSelection;
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
            public GameObject shopContent;
            public GameObject packPanel;
            public GameObject packContent;
            public GameObject choicePrefab;
            public GameObject packPrefab;
            public GameObject cardPanel;
            public CardSelectionUI cardSelection;
            public ItemPackSelectionUI packSelection;
            public GameObject upgradePanel;
            public GameObject upgradeOverlay;
            public UpgradeSelectionUI upgradeSelection;

            public void Destroy()
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}
