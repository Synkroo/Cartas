using UnityEngine;
using JuegoDeCartas.UI;

namespace JuegoDeCartas.Managers
{
    public class GameManager : MonoBehaviour
    {
        [SerializeField] private EndGameMenu endGameMenu;

        [Header("Shop")]
        public ShopManager shopManager;

        public int dinero = 1000;

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

        public void OpenShop()
        {
            if (shopManager != null)
                shopManager.Open();
        }
    }
}
