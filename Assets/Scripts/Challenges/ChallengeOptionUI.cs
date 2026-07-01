using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JuegoDeCartas.Challenges
{
    public class ChallengeOptionUI : MonoBehaviour
    {
        public Button button;
        public Image image;
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI difficultyText;
        public TextMeshProUGUI statusText;
        public GameObject selectedState;

        ChallengeData challenge;
        Action<ChallengeOptionUI, ChallengeData> onSelected;

        public ChallengeData Challenge => challenge;

        void Awake()
        {
            if (button != null)
                button.onClick.AddListener(Select);
        }

        public void Setup(
            ChallengeData data,
            Action<ChallengeOptionUI, ChallengeData> selected)
        {
            challenge = data;
            onSelected = selected;

            if (nameText != null)
                nameText.text = data != null ? data.challengeName : "";
            if (difficultyText != null)
                difficultyText.text = data != null ? data.GetDifficultyLabel() : "";
            if (statusText != null)
            {
                statusText.text = data == null
                    ? ""
                    : !data.implemented
                        ? "CONCEPTO"
                        : data.IsCompleted ? "COMPLETADO" : "";
            }
            if (image != null)
            {
                image.sprite = data != null ? data.image : null;
                image.enabled = data != null && data.image != null;
            }

            SetSelected(false);
        }

        public void SetSelected(bool selected)
        {
            if (selectedState != null)
                selectedState.SetActive(selected);
        }

        void Select()
        {
            if (challenge != null)
                onSelected?.Invoke(this, challenge);
        }
    }
}
