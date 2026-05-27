using UnityEngine;
using UnityEngine.UI;
using JuegoDeCartas.Missions;

namespace JuegoDeCartas.UI
{
    public class MainMenuManager : MonoBehaviour
    {
        [SerializeField] MissionSelectionMenu missionMenu;
        [SerializeField] Button[] mainMenuButtons;

        public void PlayGame()
        {
            SetButtonsInteractable(false);

            if (missionMenu == null)
                missionMenu = FindAnyObjectByType<MissionSelectionMenu>(FindObjectsInactive.Include);

            if (missionMenu != null)
            {
                missionMenu.Open();
                return;
            }

            Debug.LogWarning("No hay MissionSelectionMenu asignado en MainMenuManager.");
        }

        public void CloseMissionMenu()
        {
            if (missionMenu != null)
                missionMenu.Close();
            SetButtonsInteractable(true);
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
