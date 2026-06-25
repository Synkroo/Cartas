using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using JuegoDeCartas.Characters;
using JuegoDeCartas.Progression;

namespace JuegoDeCartas.UI
{
    public class SubclassInfoPanelUI : MonoBehaviour
    {
        public GameObject panel;
        public TextMeshProUGUI titleText;
        public TextMeshProUGUI contentText;
        public Button closeButton;

        [Header("Text")]
        public string titleFormat = "Subclases de {0}";
        public string usedLabel = "Usada";
        public string unusedLabel = "Aun no usada";

        void Awake()
        {
            if (panel == null)
                panel = gameObject;
            if (closeButton != null)
                closeButton.onClick.AddListener(Close);
        }

        public void Open(CharacterData character)
        {
            if (panel == null || character == null)
                return;

            if (titleText != null)
                titleText.text = string.Format(titleFormat, character.characterName);
            if (contentText != null)
                contentText.text = BuildContent(character, usedLabel, unusedLabel);

            panel.SetActive(true);
        }

        public void Close()
        {
            if (panel != null)
                panel.SetActive(false);
        }

        public static string BuildContent(
            CharacterData character,
            string usedLabel = "Usada",
            string unusedLabel = "Aun no usada")
        {
            if (character.subclasses == null || character.subclasses.Count == 0)
                return "Este heroe no tiene subclases configuradas.";

            var builder = new StringBuilder();
            for (int i = 0; i < character.subclasses.Count; i++)
            {
                SubclassData subclass = character.subclasses[i];
                if (subclass == null)
                    continue;

                if (builder.Length > 0)
                    builder.AppendLine().AppendLine();

                builder.Append(subclass.subclassName);
                builder.Append("  -  ");
                builder.AppendLine(CollectionProgress.IsSubclassSeen(subclass) ? usedLabel : unusedLabel);

                if (!string.IsNullOrWhiteSpace(subclass.description))
                    builder.AppendLine(subclass.description);
                builder.Append(subclass.passiveDescription);
            }

            return builder.ToString();
        }
    }
}
