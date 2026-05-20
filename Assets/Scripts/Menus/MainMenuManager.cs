using UnityEngine;
using UnityEngine.SceneManagement;

namespace JuegoDeCartas.UI
{
    public class MainMenuManager : MonoBehaviour
    {
        public void PlayGame()
        {
            SceneManager.LoadScene("Game");
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
