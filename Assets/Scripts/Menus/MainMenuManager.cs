using UnityEngine;
using UnityEngine.UI;
using JuegoDeCartas.Missions;
using JuegoDeCartas.Progression;
using TMPro;

namespace JuegoDeCartas.UI
{
    public class MainMenuManager : MonoBehaviour
    {
        [SerializeField] MissionSelectionMenu missionMenu;
        [SerializeField] CharacterSelectionMenu characterMenu;
        [SerializeField] CollectionMenu collectionMenu;
        [SerializeField] ProfileMenu profileMenu;
        [SerializeField] GameObject mainMenuRoot;
        [SerializeField] Button[] mainMenuButtons;
        [SerializeField] TextMeshProUGUI activeProfileText;

        void OnEnable()
        {
            ProfileManager.ProfileChanged += RefreshProfileText;
            RefreshProfileText();
        }

        void OnDisable()
        {
            ProfileManager.ProfileChanged -= RefreshProfileText;
        }

        public void PlayGame()
        {
            SetButtonsInteractable(false);

            if (characterMenu == null)
                characterMenu = FindAnyObjectByType<CharacterSelectionMenu>(FindObjectsInactive.Include);

            if (characterMenu != null)
            {
                if (mainMenuRoot != null)
                    mainMenuRoot.SetActive(false);
                characterMenu.Open();
                return;
            }

            if (missionMenu == null)
                missionMenu = FindAnyObjectByType<MissionSelectionMenu>(FindObjectsInactive.Include);

            if (missionMenu != null)
            {
                missionMenu.Open();
                return;
            }

            Debug.LogWarning("No hay MissionSelectionMenu asignado en MainMenuManager.");
        }

        public void ShowMainMenu()
        {
            if (mainMenuRoot != null)
                mainMenuRoot.SetActive(true);
            SetButtonsInteractable(true);
        }

        public void OpenCollection()
        {
            SetButtonsInteractable(false);
            if (mainMenuRoot != null)
                mainMenuRoot.SetActive(false);

            if (collectionMenu == null)
                collectionMenu = FindAnyObjectByType<CollectionMenu>(FindObjectsInactive.Include);

            if (collectionMenu != null)
                collectionMenu.Open();
            else
                ShowMainMenu();
        }

        public void OpenProfiles()
        {
            SetButtonsInteractable(false);
            if (mainMenuRoot != null)
                mainMenuRoot.SetActive(false);

            if (profileMenu == null)
                profileMenu = FindAnyObjectByType<ProfileMenu>(FindObjectsInactive.Include);

            if (profileMenu != null)
                profileMenu.Open();
            else
                ShowMainMenu();
        }

        void RefreshProfileText()
        {
            if (activeProfileText != null)
                activeProfileText.text = ProfileManager.ActiveProfileName;
        }

        public void CloseMissionMenu()
        {
            if (missionMenu != null)
                missionMenu.Close();
            ShowMainMenu();
        }

        void SetButtonsInteractable(bool value)
        {
            if (mainMenuButtons == null) return;
            for (int i = 0; i < mainMenuButtons.Length; i++)
            {
                if (mainMenuButtons[i] != null)
                    mainMenuButtons[i].interactable = value;
            }
        }

        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
