using System.Collections.Generic;
using JuegoDeCartas.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JuegoDeCartas.Challenges
{
    public class ChallengeModeMenu : MonoBehaviour
    {
        [Header("Panel")]
        public GameObject panel;
        public MainMenuManager mainMenuManager;
        public CharacterSelectionMenu characterMenu;

        [Header("Options")]
        public List<ChallengeData> challenges = new List<ChallengeData>();
        public List<ChallengeOptionUI> optionViews = new List<ChallengeOptionUI>();

        [Header("Detail")]
        public TextMeshProUGUI challengeNameText;
        public TextMeshProUGUI descriptionText;
        public TextMeshProUGUI ruleText;
        public TextMeshProUGUI completionText;

        [Header("Seed")]
        public TMP_InputField seedInput;
        public Button randomSeedButton;

        [Header("Navigation")]
        public Button playButton;
        public Button backButton;

        ChallengeData selectedChallenge;
        ChallengeOptionUI selectedView;

        void Awake()
        {
            if (panel == null)
                panel = gameObject;
            if (playButton != null)
                playButton.onClick.AddListener(Play);
            if (backButton != null)
                backButton.onClick.AddListener(Close);
            if (randomSeedButton != null)
                randomSeedButton.onClick.AddListener(GenerateSeed);
        }

        public void Open()
        {
            if (panel == null)
                return;

            panel.SetActive(true);
            RefreshOptions();
            if (seedInput != null && string.IsNullOrWhiteSpace(seedInput.text))
                seedInput.text = RunRandom.GenerateSeedCode();

            SelectFirstChallenge();
        }

        public void Close()
        {
            ChallengeRunState.Clear();
            if (panel != null)
                panel.SetActive(false);
            if (mainMenuManager != null)
                mainMenuManager.ShowMainMenu();
        }

        public void RefreshOptions()
        {
            int count = Mathf.Min(challenges.Count, optionViews.Count);
            for (int i = 0; i < optionViews.Count; i++)
            {
                ChallengeOptionUI view = optionViews[i];
                if (view == null)
                    continue;

                bool hasChallenge = i < count && challenges[i] != null;
                view.gameObject.SetActive(hasChallenge);
                if (hasChallenge)
                    view.Setup(challenges[i], SelectChallenge);
            }
        }

        public void GenerateSeed()
        {
            if (seedInput != null)
                seedInput.text = RunRandom.GenerateSeedCode();
        }

        public void Play()
        {
            if (selectedChallenge == null || characterMenu == null)
                return;

            string seed = seedInput != null ? seedInput.text : "";
            ChallengeRunState.Configure(selectedChallenge, seed);
            if (seedInput != null)
                seedInput.text = ChallengeRunState.SeedCode;

            panel.SetActive(false);
            characterMenu.Open();
        }

        void SelectFirstChallenge()
        {
            for (int i = 0; i < optionViews.Count; i++)
            {
                if (optionViews[i] != null &&
                    optionViews[i].gameObject.activeSelf &&
                    optionViews[i].Challenge != null)
                {
                    SelectChallenge(optionViews[i], optionViews[i].Challenge);
                    return;
                }
            }

            SelectChallenge(null, null);
        }

        void SelectChallenge(ChallengeOptionUI view, ChallengeData challenge)
        {
            selectedView = view;
            selectedChallenge = challenge;

            for (int i = 0; i < optionViews.Count; i++)
            {
                if (optionViews[i] != null)
                    optionViews[i].SetSelected(optionViews[i] == selectedView);
            }

            if (challengeNameText != null)
                challengeNameText.text = challenge != null ? challenge.challengeName : "";
            if (descriptionText != null)
                descriptionText.text = challenge != null ? challenge.description : "";
            if (ruleText != null)
                ruleText.text = challenge != null ? challenge.GetRuleSummary() : "";
            if (completionText != null)
            {
                completionText.text = challenge != null && challenge.IsCompleted
                    ? "COMPLETADO"
                    : "";
            }
            if (playButton != null)
                playButton.interactable = challenge != null;
        }
    }
}
