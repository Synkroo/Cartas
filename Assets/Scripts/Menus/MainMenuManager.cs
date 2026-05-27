using UnityEngine;
using JuegoDeCartas.Missions;

namespace JuegoDeCartas.UI
{
    public class MainMenuManager : MonoBehaviour
    {
        [SerializeField] MissionSelectionMenu missionMenu;

        public void PlayGame()
        {
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
