using UnityEngine;
using JuegoDeCartas.UI;

namespace JuegoDeCartas.Managers
{
    public class GameManager : MonoBehaviour
    {
        [SerializeField] private EndGameMenu endGameMenu;

        public void ShowVictory()
        {
            if (endGameMenu != null)
                endGameMenu.ShowVictory();
        }

        public void ShowDefeat()
        {
            if (endGameMenu != null)
                endGameMenu.ShowDefeat();
        }
    }
}
