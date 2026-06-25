using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using JuegoDeCartas.Characters;

namespace JuegoDeCartas.UI
{
    public class SubclassSelectionUI : MonoBehaviour
    {
        [Header("References")]
        public GameObject panel;
        public Transform content;
        public GameObject choicePrefab;
        public TextMeshProUGUI titleText;
        public UITransitionAnimator transition;

        [Header("Text")]
        public string titleFormat = "Elige una subclase para {0}";

        readonly List<GameObject> spawnedChoices = new List<GameObject>();
        Action<SubclassData> onSelected;

        public bool IsConfigured => panel != null && content != null && choicePrefab != null;

        void Awake()
        {
            if (panel != null)
                panel.SetActive(false);
        }

        public bool Open(CharacterData character, Action<SubclassData> selected)
        {
            if (!IsConfigured || character == null || character.subclasses == null)
                return false;

            ClearChoices();
            onSelected = selected;

            if (titleText != null)
                titleText.text = string.Format(titleFormat, character.characterName);

            for (int i = 0; i < character.subclasses.Count; i++)
            {
                SubclassData subclass = character.subclasses[i];
                if (subclass == null)
                    continue;

                GameObject instance = Instantiate(choicePrefab, content);
                SubclassChoiceDisplay display = instance.GetComponent<SubclassChoiceDisplay>();
                if (display != null)
                    display.Setup(subclass, Select);
                spawnedChoices.Add(instance);
            }

            if (spawnedChoices.Count == 0)
            {
                onSelected = null;
                return false;
            }

            panel.SetActive(true);
            if (transition != null)
                transition.PlayIn();
            return true;
        }

        public void Close()
        {
            onSelected = null;
            if (panel != null)
                panel.SetActive(false);
            ClearChoices();
        }

        void Select(SubclassData subclass)
        {
            Action<SubclassData> selected = onSelected;
            onSelected = null;
            if (panel != null)
                panel.SetActive(false);
            ClearChoices();
            selected?.Invoke(subclass);
        }

        void ClearChoices()
        {
            for (int i = 0; i < spawnedChoices.Count; i++)
            {
                if (spawnedChoices[i] != null)
                    Destroy(spawnedChoices[i]);
            }
            spawnedChoices.Clear();
        }
    }
}
