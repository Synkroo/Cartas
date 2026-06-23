using System.Collections.Generic;
using System.Linq;
using JuegoDeCartas.Managers;
using JuegoDeCartas.Relics;
using JuegoDeCartas.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace JuegoDeCartas.Tests
{
    public class RelicTests
    {
        const string RelicFolder = "Assets/GameData/Relics";
        const string GameScenePath = "Assets/Scenes/Game.unity";

        [Test]
        public void ProjectContainsTwelveConfiguredUniqueRelics()
        {
            RelicData[] relics = LoadRelics();

            Assert.AreEqual(12, relics.Length);
            Assert.AreEqual(
                12,
                relics.Select(relic => relic.effectType).Distinct().Count()
            );
            Assert.IsTrue(relics.All(relic =>
                relic != null &&
                !string.IsNullOrWhiteSpace(relic.relicName) &&
                !string.IsNullOrWhiteSpace(relic.description) &&
                relic.icon != null &&
                relic.price > 0 &&
                relic.appearanceWeight > 0f
            ));
        }

        [Test]
        public void InventoryAllowsSixUniqueRelicsAtMost()
        {
            RelicData[] relics = LoadRelics();
            GameObject root = new GameObject("RelicInventoryTest");
            RelicInventory inventory = root.AddComponent<RelicInventory>();
            inventory.Initialize(null);

            for (int i = 0; i < RelicInventory.MaxRelics; i++)
                Assert.IsTrue(inventory.TryAdd(relics[i]));

            Assert.IsTrue(inventory.IsFull);
            Assert.AreEqual(RelicInventory.MaxRelics, inventory.OwnedRelics.Count);
            Assert.IsFalse(inventory.TryAdd(relics[0]));
            Assert.IsFalse(inventory.TryAdd(relics[6]));
            Assert.IsTrue(inventory.TryReplace(relics[0], relics[6]));
            Assert.IsFalse(inventory.Contains(relics[0]));
            Assert.IsTrue(inventory.Contains(relics[6]));
            Assert.AreEqual(RelicInventory.MaxRelics, inventory.OwnedRelics.Count);

            Object.DestroyImmediate(root);
        }

        [Test]
        public void OfferGeneratorExcludesOwnedRelicsAndDuplicates()
        {
            RelicData[] relics = LoadRelics();
            var owned = new List<RelicData> { relics[0], relics[1] };

            List<RelicData> offers = RelicOfferGenerator.Roll(
                relics,
                owned,
                6
            );

            Assert.AreEqual(6, offers.Count);
            Assert.AreEqual(6, offers.Distinct().Count());
            Assert.IsFalse(offers.Contains(relics[0]));
            Assert.IsFalse(offers.Contains(relics[1]));
        }

        [Test]
        public void PassiveRelicsModifyDurationInterestAndFirstHit()
        {
            RelicData duration = FindRelic(RelicEffectType.BuffDuration);
            RelicData interest = FindRelic(RelicEffectType.FlatInterest);
            RelicData reduction = FindRelic(RelicEffectType.ReduceFirstHit);
            GameObject root = new GameObject("RelicPassiveTest");
            RelicInventory inventory = root.AddComponent<RelicInventory>();
            inventory.Initialize(null);
            inventory.TryAdd(duration);
            inventory.TryAdd(interest);
            inventory.TryAdd(reduction);

            Assert.AreEqual(4, inventory.ModifyBuffDuration(3));
            Assert.AreEqual(20, inventory.GetInterestBonus());

            inventory.OnCombatStarted();
            Assert.AreEqual(6, inventory.ModifyIncomingDamage(10));
            Assert.AreEqual(10, inventory.ModifyIncomingDamage(10));
            inventory.OnCombatStarted();
            Assert.AreEqual(6, inventory.ModifyIncomingDamage(10));

            Object.DestroyImmediate(root);
        }

        [Test]
        public void LifeStealAndCombatRewardsApplyThroughInventory()
        {
            GameObject root = new GameObject("RelicBattleTest");
            BattleManager battle = root.AddComponent<BattleManager>();
            battle.player = new Entity();
            battle.player.stats.maxHealth = 50;
            battle.player.stats.health = 20;
            battle.deckManager = root.AddComponent<DeckManager>();
            battle.gameManager = root.AddComponent<GameManager>();
            RelicInventory inventory = root.AddComponent<RelicInventory>();
            battle.relicInventory = inventory;
            inventory.Initialize(battle);
            inventory.TryAdd(FindRelic(RelicEffectType.LifeSteal));
            inventory.TryAdd(FindRelic(RelicEffectType.HealAfterCombat));
            inventory.TryAdd(FindRelic(RelicEffectType.GoldPerEnemy));

            inventory.OnDamageDealt(50);
            Assert.AreEqual(25, battle.player.stats.health);

            int bonusGold = inventory.OnEnemyDefeated();
            Assert.AreEqual(30, battle.player.stats.health);
            Assert.AreEqual(40, bonusGold);

            Object.DestroyImmediate(root);
        }

        [Test]
        public void ShopPurchaseChargesGoldAndAddsRelic()
        {
            RelicData[] relics = LoadRelics();
            GameObject root = new GameObject("RelicShopTest");
            BattleManager battle = root.AddComponent<BattleManager>();
            battle.player = new Entity();
            battle.deckManager = root.AddComponent<DeckManager>();
            GameManager gameManager = root.AddComponent<GameManager>();
            gameManager.dinero = 1000;
            RelicInventory inventory = root.AddComponent<RelicInventory>();
            inventory.Initialize(battle);
            ShopManager shop = root.AddComponent<ShopManager>();
            RelicShopOfferDisplay display = new GameObject(
                "RelicOffer",
                typeof(RectTransform),
                typeof(RelicShopOfferDisplay)
            ).GetComponent<RelicShopOfferDisplay>();
            display.transform.SetParent(root.transform);

            shop.battle = battle;
            shop.gameManager = gameManager;
            shop.relicInventory = inventory;
            shop.relicPool = relics.ToList();
            shop.relicOfferDisplays = new List<RelicShopOfferDisplay>
            {
                display
            };
            shop.RefreshRelicOffers();

            RelicData offered = shop.CurrentRelicOffers[0];
            int expectedGold = 1000 - offered.price;
            Assert.IsTrue(shop.TryPurchaseRelic(offered));
            Assert.AreEqual(expectedGold, gameManager.dinero);
            Assert.IsTrue(inventory.Contains(offered));
            Assert.IsFalse(shop.TryPurchaseRelic(offered));

            Object.DestroyImmediate(root);
        }

        [Test]
        public void FullInventoryPurchaseWaitsForReplacementBeforeCharging()
        {
            RelicData[] relics = LoadRelics();
            GameObject root = new GameObject("RelicReplacementShopTest");
            BattleManager battle = root.AddComponent<BattleManager>();
            battle.player = new Entity();
            battle.deckManager = root.AddComponent<DeckManager>();
            GameManager gameManager = root.AddComponent<GameManager>();
            gameManager.dinero = 2000;
            RelicInventory inventory = root.AddComponent<RelicInventory>();
            inventory.Initialize(battle);
            for (int i = 0; i < RelicInventory.MaxRelics; i++)
                Assert.IsTrue(inventory.TryAdd(relics[i]));

            GameObject replacementPanel = new GameObject(
                "ReplacementPanel",
                typeof(RectTransform)
            );
            replacementPanel.transform.SetParent(root.transform);
            replacementPanel.SetActive(false);
            GameObject replacementRoot = new GameObject(
                "ReplacementUI",
                typeof(RectTransform),
                typeof(RelicReplacementUI)
            );
            replacementRoot.transform.SetParent(root.transform);
            RelicReplacementUI replacement =
                replacementRoot.GetComponent<RelicReplacementUI>();
            replacement.panel = replacementPanel;
            replacement.cancelButton = replacementRoot.AddComponent<Button>();
            replacement.choices = new List<RelicReplacementChoiceUI>();
            for (int i = 0; i < RelicInventory.MaxRelics; i++)
            {
                GameObject choiceObject = new GameObject(
                    "Choice" + i,
                    typeof(RectTransform),
                    typeof(RelicReplacementChoiceUI)
                );
                choiceObject.transform.SetParent(replacementRoot.transform);
                replacement.choices.Add(
                    choiceObject.GetComponent<RelicReplacementChoiceUI>()
                );
            }

            ShopManager shop = root.AddComponent<ShopManager>();
            RelicShopOfferDisplay display = new GameObject(
                "RelicOffer",
                typeof(RectTransform),
                typeof(RelicShopOfferDisplay)
            ).GetComponent<RelicShopOfferDisplay>();
            display.transform.SetParent(root.transform);
            shop.battle = battle;
            shop.gameManager = gameManager;
            shop.relicInventory = inventory;
            shop.relicPool = new List<RelicData> { relics[6] };
            shop.relicOfferDisplays = new List<RelicShopOfferDisplay>
            {
                display
            };
            shop.relicReplacementUI = replacement;
            shop.RefreshRelicOffers();

            RelicData offered = shop.CurrentRelicOffers[0];
            int startingGold = gameManager.dinero;
            Assert.IsTrue(shop.TryPurchaseRelic(offered));
            Assert.AreEqual(startingGold, gameManager.dinero);
            Assert.IsTrue(replacementPanel.activeSelf);
            Assert.IsFalse(inventory.Contains(offered));

            shop.CancelRelicReplacement();
            Assert.AreEqual(startingGold, gameManager.dinero);
            Assert.IsFalse(replacementPanel.activeSelf);
            Assert.IsTrue(inventory.Contains(relics[0]));
            Assert.IsFalse(inventory.Contains(offered));

            Assert.IsTrue(shop.TryPurchaseRelic(offered));
            Assert.IsTrue(shop.ConfirmRelicReplacement(relics[0]));
            Assert.AreEqual(startingGold - offered.price, gameManager.dinero);
            Assert.IsFalse(inventory.Contains(relics[0]));
            Assert.IsTrue(inventory.Contains(offered));
            Assert.AreEqual(
                RelicInventory.MaxRelics,
                inventory.OwnedRelics.Count
            );
            Assert.IsFalse(replacementPanel.activeSelf);

            Object.DestroyImmediate(root);
        }

        [Test]
        public void GameSceneContainsSixInventorySlotsAndTwoShopOffers()
        {
            Scene scene = SceneManager.GetSceneByPath(GameScenePath);
            bool openedForTest = !scene.isLoaded;
            if (openedForTest)
            {
                scene = EditorSceneManager.OpenScene(
                    GameScenePath,
                    OpenSceneMode.Additive
                );
            }

            RelicInventoryUI inventoryUI =
                FindSceneComponent<RelicInventoryUI>(scene);
            ShopManager shop = FindSceneComponent<ShopManager>(scene);
            BattleManager battle = FindSceneComponent<BattleManager>(scene);

            Assert.NotNull(inventoryUI);
            Assert.AreEqual(RelicInventory.MaxRelics, inventoryUI.slots.Count);
            Assert.NotNull(inventoryUI.transition);
            Assert.IsTrue(inventoryUI.slots.All(slot =>
                slot != null &&
                slot.iconImage != null &&
                slot.tooltip != null &&
                slot.animationTarget != null
            ));
            Assert.NotNull(shop);
            Assert.AreEqual(2, shop.relicOfferDisplays.Count);
            Assert.IsTrue(shop.relicOfferDisplays.All(display =>
                display != null &&
                display.transition != null &&
                display.GetComponent<MenuButtonMotion>() != null
            ));
            Assert.NotNull(shop.relicReplacementUI);
            Assert.IsTrue(shop.relicReplacementUI.IsConfigured);
            Assert.AreEqual(
                RelicInventory.MaxRelics,
                shop.relicReplacementUI.choices.Count
            );
            Assert.IsNull(shop.transform.Find("RelicShopTooltip"));
            Assert.AreEqual(12, shop.relicPool.Count);
            Assert.NotNull(shop.relicInventory);
            Assert.NotNull(battle);
            Assert.AreSame(battle.relicInventory, shop.relicInventory);

            if (openedForTest)
                EditorSceneManager.CloseScene(scene, true);
        }

        [Test]
        public void RelicPanelsDoNotOverlapPrimaryHudOrPackArea()
        {
            Scene scene = SceneManager.GetSceneByPath(GameScenePath);
            bool openedForTest = !scene.isLoaded;
            if (openedForTest)
            {
                scene = EditorSceneManager.OpenScene(
                    GameScenePath,
                    OpenSceneMode.Additive
                );
            }

            RelicInventoryUI inventoryUI =
                FindSceneComponent<RelicInventoryUI>(scene);
            ShopManager shop = FindSceneComponent<ShopManager>(scene);
            Transform leftZone = FindSceneTransform(scene, "Zona Izquierda");
            Transform packRoot = shop.transform.Find("Posicion de articulos");
            RectTransform relicSection =
                shop.transform.Find("RelicShopSection") as RectTransform;
            RectTransform packPrefab = AssetDatabase
                .LoadAssetAtPath<GameObject>("Assets/Prefabs/PackPrefab.prefab")
                .GetComponent<RectTransform>();

            Assert.NotNull(inventoryUI);
            Assert.NotNull(leftZone);
            Bounds inventoryBounds = GetRectBounds(
                leftZone.parent,
                inventoryUI.transform as RectTransform
            );
            Bounds leftBounds = GetRectBounds(
                leftZone.parent,
                leftZone as RectTransform
            );
            Assert.GreaterOrEqual(
                inventoryBounds.min.y,
                leftBounds.max.y + 0.1f
            );
            Assert.GreaterOrEqual(
                (inventoryUI.transform as RectTransform).anchoredPosition.y,
                3f
            );

            CombatTooltipUI combatTooltip =
                inventoryUI.slots[0].tooltip;
            Assert.NotNull(combatTooltip);
            Assert.LessOrEqual(
                (combatTooltip.transform as RectTransform)
                    .anchoredPosition.y,
                -1.3f
            );

            Assert.NotNull(packRoot);
            Assert.NotNull(relicSection);
            Assert.NotNull(packPrefab);
            Bounds relicBounds =
                RectTransformUtility.CalculateRelativeRectTransformBounds(
                    shop.transform,
                    relicSection
                );
            foreach (Transform slot in packRoot)
            {
                Vector3 center = shop.transform.InverseTransformPoint(
                    slot.position
                );
                float packBottom = center.y -
                    packPrefab.rect.height * slot.localScale.y * 0.5f;
                Assert.LessOrEqual(
                    relicBounds.max.y + 0.05f,
                    packBottom
                );
            }

            if (openedForTest)
                EditorSceneManager.CloseScene(scene, true);
        }

        static RelicData[] LoadRelics()
        {
            return AssetDatabase.FindAssets("t:RelicData", new[] { RelicFolder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<RelicData>)
                .Where(relic => relic != null)
                .OrderBy(relic => relic.name)
                .ToArray();
        }

        static RelicData FindRelic(RelicEffectType effectType)
        {
            RelicData relic = LoadRelics()
                .FirstOrDefault(candidate => candidate.effectType == effectType);
            Assert.NotNull(relic, "Missing relic for " + effectType);
            return relic;
        }

        static T FindSceneComponent<T>(Scene scene) where T : Component
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                T component = root.GetComponentInChildren<T>(true);
                if (component != null)
                    return component;
            }

            return null;
        }

        static Transform FindSceneTransform(Scene scene, string objectName)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Transform[] transforms =
                    root.GetComponentsInChildren<Transform>(true);
                Transform match = transforms.FirstOrDefault(candidate =>
                    candidate.name == objectName
                );
                if (match != null)
                    return match;
            }

            return null;
        }

        static Bounds GetRectBounds(
            Transform relativeTo,
            RectTransform rect)
        {
            Assert.NotNull(relativeTo);
            Assert.NotNull(rect);

            Vector3[] corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            Bounds bounds = new Bounds(
                relativeTo.InverseTransformPoint(corners[0]),
                Vector3.zero
            );
            for (int i = 1; i < corners.Length; i++)
            {
                bounds.Encapsulate(
                    relativeTo.InverseTransformPoint(corners[i])
                );
            }

            return bounds;
        }
    }
}
