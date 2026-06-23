using System.Collections;
using System.Linq;
using JuegoDeCartas.Challenges;
using JuegoDeCartas.Managers;
using JuegoDeCartas.Missions;
using JuegoDeCartas.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace JuegoDeCartas.Tests
{
    public class SceneSmokeTests
    {
        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
            RunStateCoordinator.Reset();
        }

        [UnityTest]
        public IEnumerator MainMenuLoadsWithEveryPrimaryFlowConfigured()
        {
            yield return SceneManager.LoadSceneAsync("MainMenu");
            yield return null;
            yield return null;

            Assert.NotNull(FindIncludingInactive<MainMenuManager>());
            Assert.NotNull(FindIncludingInactive<CharacterSelectionMenu>());
            Assert.NotNull(FindIncludingInactive<MissionSelectionMenu>());
            Assert.NotNull(FindIncludingInactive<ChallengeModeMenu>());
            Assert.NotNull(FindIncludingInactive<CollectionMenu>());
            Assert.NotNull(FindIncludingInactive<ProfileMenu>());

            CanvasScaler[] screenScalers = Object
                .FindObjectsByType<CanvasScaler>(FindObjectsInactive.Include)
                .Where(scaler =>
                    scaler.GetComponent<Canvas>()?.renderMode != RenderMode.WorldSpace &&
                    scaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize
                )
                .ToArray();

            Assert.IsNotEmpty(screenScalers);
            Assert.IsTrue(screenScalers.All(scaler =>
                scaler.referenceResolution == new Vector2(1920f, 1080f)
            ));
        }

        [UnityTest]
        public IEnumerator GameLoadsWithSerializedInteractiveUiDependencies()
        {
            yield return SceneManager.LoadSceneAsync("Game");
            yield return null;
            yield return null;
            yield return null;

            Assert.NotNull(FindIncludingInactive<GameManager>());
            Assert.NotNull(FindIncludingInactive<BattleManager>());

            ShopManager shop = FindIncludingInactive<ShopManager>();
            EndGameMenu endGame = FindIncludingInactive<EndGameMenu>();
            CardSelectionUI cardSelection = FindIncludingInactive<CardSelectionUI>();

            Assert.NotNull(shop);
            Assert.NotNull(endGame);
            Assert.NotNull(cardSelection);
            Assert.NotNull(shop.menusCanvas);
            Assert.NotNull(shop.menusCanvas.GetComponent<GraphicRaycaster>());
            Assert.NotNull(shop.closeButton);
            Assert.NotNull(shop.deckButton);
            Assert.NotNull(shop.restockButton);
            Assert.NotNull(shop.restockPriceText);
            Assert.NotNull(shop.shopTitleText);
            Assert.NotNull(endGame.menusCanvas);
            Assert.NotNull(endGame.menusCanvas.GetComponent<GraphicRaycaster>());
            Assert.IsTrue(cardSelection.IsConfigured);
            Assert.NotNull(cardSelection.cardPrefab.GetComponent<Button>());

            UITransitionAnimator[] transitions =
                Object.FindObjectsByType<UITransitionAnimator>(FindObjectsInactive.Include);
            Assert.IsNotEmpty(transitions);
            Assert.IsTrue(transitions.All(transition =>
                transition.canvasGroup != null && transition.target != null
            ));
            Assert.AreEqual(1f, Time.timeScale);
        }

        static T FindIncludingInactive<T>() where T : Object
        {
            return Object.FindAnyObjectByType<T>(FindObjectsInactive.Include);
        }
    }
}
