using JuegoDeCartas.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JuegoDeCartas.Tests
{
    public class SettingsMenuTests
    {
        GameObject root;

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
            GameSettings.ResetDefaults();
            if (root != null)
                Object.DestroyImmediate(root);
        }

        [Test]
        public void DisplayChangesStayPendingUntilApplyOrRevert()
        {
            GameSettings.SaveDisplay(1280, 720, false);
            SettingsMenuUI menu = CreateMenu();

            menu.Refresh();
            Assert.IsFalse(menu.HasPendingDisplayChanges);
            Assert.AreEqual("Pantalla guardada", menu.displayStatusText.text);

            menu.SelectNextResolution();
            menu.SetPendingFullscreen(true);
            string selectedResolution = menu.resolutionValueText.text;

            Assert.IsTrue(menu.HasPendingDisplayChanges);
            Assert.AreEqual(1280, GameSettings.Width);
            Assert.AreEqual(720, GameSettings.Height);
            Assert.IsFalse(GameSettings.Fullscreen);
            Assert.IsTrue(menu.applyDisplayButton.interactable);
            Assert.IsTrue(menu.revertDisplayButton.interactable);
            Assert.AreEqual(
                "Cambios de pantalla sin aplicar",
                menu.displayStatusText.text
            );

            menu.RevertDisplayChanges();
            Assert.IsFalse(menu.HasPendingDisplayChanges);
            Assert.AreEqual("1280 x 720", menu.resolutionValueText.text);
            Assert.IsFalse(menu.fullscreenToggle.isOn);

            menu.SelectNextResolution();
            menu.SetPendingFullscreen(true);
            selectedResolution = menu.resolutionValueText.text;
            menu.ApplyDisplayChanges();

            Assert.IsFalse(menu.HasPendingDisplayChanges);
            Assert.IsTrue(GameSettings.Fullscreen);
            Assert.AreEqual(selectedResolution, menu.resolutionValueText.text);
            Assert.AreEqual("Pantalla guardada", menu.displayStatusText.text);
            Assert.IsFalse(menu.applyDisplayButton.interactable);
            Assert.IsFalse(menu.revertDisplayButton.interactable);
        }

        [Test]
        public void PauseMenuResolvesConfiguredSettingsMenuWhenReferenceIsLegacy()
        {
            root = new GameObject("PauseRoot");
            GameObject legacy = new GameObject("Menu Opciones");
            legacy.transform.SetParent(root.transform);
            legacy.SetActive(false);

            GameObject options = new GameObject("OptionsMenu");
            options.transform.SetParent(root.transform);
            SettingsMenuUI settings = options.AddComponent<SettingsMenuUI>();
            options.SetActive(false);

            PauseMenu pause = root.AddComponent<PauseMenu>();
            pause.optionsMenu = legacy;

            pause.OpenMenu();

            Assert.AreSame(options, pause.optionsMenu);
            Assert.AreSame(settings, pause.settingsMenu);
            Assert.IsTrue(options.activeSelf);
            Assert.IsFalse(legacy.activeSelf);
        }

        SettingsMenuUI CreateMenu()
        {
            root = new GameObject("SettingsMenu", typeof(RectTransform));
            SettingsMenuUI menu = root.AddComponent<SettingsMenuUI>();
            menu.resolutionValueText = CreateText("Resolution");
            menu.displayStatusText = CreateText("Status");
            menu.fullscreenToggle = CreateToggle("Fullscreen");
            menu.applyDisplayButton = CreateButton("Apply");
            menu.revertDisplayButton = CreateButton("Revert");
            return menu;
        }

        TextMeshProUGUI CreateText(string name)
        {
            GameObject textObject = new GameObject(
                name,
                typeof(RectTransform),
                typeof(TextMeshProUGUI)
            );
            textObject.transform.SetParent(root.transform);
            return textObject.GetComponent<TextMeshProUGUI>();
        }

        Toggle CreateToggle(string name)
        {
            GameObject toggleObject = new GameObject(
                name,
                typeof(RectTransform),
                typeof(Toggle)
            );
            toggleObject.transform.SetParent(root.transform);
            return toggleObject.GetComponent<Toggle>();
        }

        Button CreateButton(string name)
        {
            GameObject buttonObject = new GameObject(
                name,
                typeof(RectTransform),
                typeof(Button)
            );
            buttonObject.transform.SetParent(root.transform);
            return buttonObject.GetComponent<Button>();
        }
    }
}
