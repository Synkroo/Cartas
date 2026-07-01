using System.Collections.Generic;
using JuegoDeCartas.Characters;
using JuegoDeCartas.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using JuegoDeCartas.Managers;

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
        public Transform challengeListContent;
        public ChallengeOptionUI challengeOptionPrefab;
        public List<ChallengeOptionUI> optionViews = new List<ChallengeOptionUI>();

        [Header("Detail")]
        public TextMeshProUGUI challengeNameText;
        public TextMeshProUGUI descriptionText;
        public TextMeshProUGUI ruleText;
        public TextMeshProUGUI completionText;

        [Header("Hero Badges")]
        public List<CharacterData> badgeCharacters = new List<CharacterData>();
        public Transform badgeContent;
        public ChallengeHeroBadgeUI badgePrefab;
        public List<ChallengeHeroBadgeUI> heroBadges = new List<ChallengeHeroBadgeUI>();

        [Header("Seeded Run")]
        public Button seededRunButton;
        public GameObject seededRunSelectedState;
        public TextMeshProUGUI seededRunNameText;
        public TextMeshProUGUI seededRunStatusText;
        public List<GameObject> seedControls = new List<GameObject>();
        public TMP_InputField seedInput;
        public Button randomSeedButton;
        public string seededRunTitle = "Partida con semilla";
        public string seededRunStatusLabel = "RUN NORMAL";
        [TextArea(2, 5)]
        public string seededRunDescription =
            "Repite una run normal concreta o comparte su codigo con otros jugadores. " +
            "Ejemplo de semilla: A7F3C91D.";
        [TextArea(2, 4)]
        public string seededRunRule =
            "La semilla fija enemigos, cartas, ataques y contenido de la tienda.";

        [Header("Navigation")]
        public Button playButton;
        public Button backButton;

        ChallengeData selectedChallenge;
        ChallengeOptionUI selectedView;
        bool seededRunSelected;

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
            if (seededRunButton != null)
                seededRunButton.onClick.AddListener(SelectSeededRun);
        }

        public void Open()
        {
            if (panel == null)
                return;

            panel.SetActive(true);
            RefreshOptions();
            BuildHeroBadges();
            if (seedInput != null && string.IsNullOrWhiteSpace(seedInput.text))
                seedInput.text = RunRandom.GenerateSeedCode();
            if (seededRunNameText != null)
                seededRunNameText.text = seededRunTitle;
            if (seededRunStatusText != null)
                seededRunStatusText.text = seededRunStatusLabel;

            SelectFirstChallenge();
        }

        public void Close()
        {
            RunStateCoordinator.Reset();
            if (panel != null)
                panel.SetActive(false);
            if (mainMenuManager != null)
                mainMenuManager.ShowMainMenu();
        }

        public void RefreshOptions()
        {
            if (challengeListContent != null && challengeOptionPrefab != null)
            {
                ClearGeneratedOptions();
                for (int i = 0; i < challenges.Count; i++)
                {
                    ChallengeData challenge = challenges[i];
                    if (challenge == null)
                        continue;

                    ChallengeOptionUI view = Instantiate(
                        challengeOptionPrefab,
                        challengeListContent
                    );
                    view.gameObject.SetActive(true);
                    view.Setup(challenge, SelectChallenge);
                    optionViews.Add(view);
                }
                return;
            }

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

        void ClearGeneratedOptions()
        {
            for (int i = optionViews.Count - 1; i >= 0; i--)
            {
                ChallengeOptionUI view = optionViews[i];
                if (view != null && view.transform.parent == challengeListContent)
                    Destroy(view.gameObject);
            }
            optionViews.Clear();
        }

        public void GenerateSeed()
        {
            if (seedInput != null)
                seedInput.text = RunRandom.GenerateSeedCode();
        }

        public void Play()
        {
            if ((!seededRunSelected && selectedChallenge == null) || characterMenu == null)
                return;

            if (seededRunSelected)
            {
                string seed = seedInput != null ? seedInput.text : "";
                ChallengeRunState.ConfigureSeededRun(seed);
                if (seedInput != null)
                    seedInput.text = ChallengeRunState.SeedCode;
            }
            else
            {
                ChallengeRunState.ConfigureChallenge(selectedChallenge);
            }

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
            seededRunSelected = false;
            selectedView = view;
            selectedChallenge = challenge;

            for (int i = 0; i < optionViews.Count; i++)
            {
                if (optionViews[i] != null)
                    optionViews[i].SetSelected(optionViews[i] == selectedView);
            }
            if (seededRunSelectedState != null)
                seededRunSelectedState.SetActive(false);

            if (challengeNameText != null)
                challengeNameText.text = challenge != null ? challenge.challengeName : "";
            if (descriptionText != null)
                descriptionText.text = challenge != null ? challenge.description : "";
            if (ruleText != null)
                ruleText.text = challenge != null ? challenge.GetRuleSummary() : "";
            if (completionText != null)
            {
                completionText.text = challenge != null
                    ? !challenge.implemented
                        ? "CONCEPTO FUTURO"
                        : challenge.IsCompleted ? "MARCAS CONSEGUIDAS" : "MARCAS"
                    : "";
            }
            RefreshHeroBadges(challenge != null && challenge.implemented ? challenge : null);
            SetSeedControlsVisible(false);
            if (playButton != null)
                playButton.interactable = challenge != null && challenge.IsPlayable;
        }

        void SelectSeededRun()
        {
            seededRunSelected = true;
            selectedView = null;
            selectedChallenge = null;

            for (int i = 0; i < optionViews.Count; i++)
            {
                if (optionViews[i] != null)
                    optionViews[i].SetSelected(false);
            }
            if (seededRunSelectedState != null)
                seededRunSelectedState.SetActive(true);
            if (challengeNameText != null)
                challengeNameText.text = seededRunTitle;
            if (descriptionText != null)
                descriptionText.text = seededRunDescription;
            if (ruleText != null)
                ruleText.text = seededRunRule;
            if (completionText != null)
                completionText.text = "";

            RefreshHeroBadges(null);
            SetSeedControlsVisible(true);
            if (seedInput != null && string.IsNullOrWhiteSpace(seedInput.text))
                seedInput.text = RunRandom.GenerateSeedCode();
            if (playButton != null)
                playButton.interactable = true;
        }

        void RefreshHeroBadges(ChallengeData challenge)
        {
            for (int i = 0; i < heroBadges.Count; i++)
            {
                if (heroBadges[i] != null)
                    heroBadges[i].Refresh(challenge);
            }
        }

        void BuildHeroBadges()
        {
            if (badgeContent == null || badgePrefab == null)
                return;

            for (int i = badgeContent.childCount - 1; i >= 0; i--)
                Destroy(badgeContent.GetChild(i).gameObject);

            heroBadges.Clear();
            for (int i = 0; i < badgeCharacters.Count; i++)
            {
                CharacterData character = badgeCharacters[i];
                if (character == null)
                    continue;

                ChallengeHeroBadgeUI badge = Instantiate(badgePrefab, badgeContent);
                badge.character = character;
                badge.gameObject.SetActive(true);
                badge.Hide();
                heroBadges.Add(badge);
            }
        }

        void SetSeedControlsVisible(bool visible)
        {
            for (int i = 0; i < seedControls.Count; i++)
            {
                if (seedControls[i] != null)
                    seedControls[i].SetActive(visible);
            }
        }
    }
}
