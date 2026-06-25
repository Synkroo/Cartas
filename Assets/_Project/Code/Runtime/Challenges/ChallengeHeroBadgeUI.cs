using JuegoDeCartas.Characters;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JuegoDeCartas.Challenges
{
    public class ChallengeHeroBadgeUI : MonoBehaviour
    {
        [Header("Hero")]
        public CharacterData character;

        [Header("References")]
        public GameObject root;
        public Image portraitImage;
        public Image bloodMarkImage;
        public TextMeshProUGUI fallbackIconText;
        public TextMeshProUGUI heroNameText;
        public TextMeshProUGUI statusText;
        public GameObject completedState;
        public GameObject incompleteState;

        [Header("Labels")]
        public string completedLabel = "CONSEGUIDA";
        public string incompleteLabel = "PENDIENTE";

        public void Refresh(ChallengeData challenge)
        {
            if (root == null)
                root = gameObject;

            bool visible = challenge != null && character != null;
            root.SetActive(visible);
            if (!visible)
                return;

            bool completed = challenge.IsCompletedByCharacter(character);
            if (portraitImage != null)
            {
                portraitImage.sprite = character.portrait;
                portraitImage.enabled = character.portrait != null;
            }
            if (fallbackIconText != null)
            {
                bool useFallback = character.portrait == null;
                fallbackIconText.gameObject.SetActive(useFallback);
                fallbackIconText.text =
                    useFallback && !string.IsNullOrWhiteSpace(character.characterName)
                        ? character.characterName.Substring(0, 1).ToUpperInvariant()
                        : "";
            }
            if (heroNameText != null)
                heroNameText.text = character.characterName;
            if (statusText != null)
                statusText.text = completed ? completedLabel : incompleteLabel;
            if (bloodMarkImage != null)
                bloodMarkImage.gameObject.SetActive(completed);
            if (completedState != null)
                completedState.SetActive(completed);
            if (incompleteState != null)
                incompleteState.SetActive(!completed);
        }

        public void Hide()
        {
            if (root == null)
                root = gameObject;
            root.SetActive(false);
        }
    }
}
