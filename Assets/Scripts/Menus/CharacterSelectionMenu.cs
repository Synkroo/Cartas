using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using JuegoDeCartas.Cards;
using JuegoDeCartas.Characters;
using JuegoDeCartas.Missions;

namespace JuegoDeCartas.UI
{
    public class CharacterSelectionMenu : MonoBehaviour
    {
        [Header("Panel")]
        public GameObject panel;
        public CanvasGroup canvasGroup;

        [Header("Characters")]
        public List<CharacterData> characters = new List<CharacterData>();
        public Image portraitImage;
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI descriptionText;
        public TextMeshProUGUI mechanicText;
        public TextMeshProUGUI statsText;
        public TextMeshProUGUI lockText;
        public Button subclassesButton;
        public SubclassInfoPanelUI subclassInfoPanel;

        [Header("Deck Preview")]
        public Transform deckContent;
        public GameObject cardPrefab;
        public Vector2 deckCardSlotSize = new Vector2(180f, 260f);
        public Vector3 deckCardScale = Vector3.one;

        [Header("Navigation")]
        public Button previousButton;
        public Button nextButton;
        public Button continueButton;
        public Button backButton;
        public MissionSelectionMenu missionMenu;
        public MainMenuManager mainMenuManager;

        [Header("Animation")]
        [Min(0.01f)] public float fadeDuration = 0.2f;

        int selectedIndex;
        Coroutine fadeRoutine;

        public CharacterData SelectedCharacter =>
            characters.Count > 0 && selectedIndex >= 0 && selectedIndex < characters.Count
                ? characters[selectedIndex]
                : null;

        void Awake()
        {
            if (panel == null)
                panel = gameObject;
            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();

            if (previousButton != null)
                previousButton.onClick.AddListener(ShowPrevious);
            if (nextButton != null)
                nextButton.onClick.AddListener(ShowNext);
            if (continueButton != null)
                continueButton.onClick.AddListener(Continue);
            if (backButton != null)
                backButton.onClick.AddListener(BackToMainMenu);
            if (subclassesButton != null)
                subclassesButton.onClick.AddListener(ShowSubclasses);
        }

        public void Open()
        {
            if (panel == null)
                return;

            panel.SetActive(true);
            selectedIndex = FindInitialIndex();
            Refresh();

            if (canvasGroup != null)
                canvasGroup.alpha = 0f;
            FadeTo(1f);
        }

        public void Close()
        {
            Close(null);
        }

        void Close(System.Action completed)
        {
            if (subclassInfoPanel != null)
                subclassInfoPanel.Close();

            FadeTo(0f, () =>
            {
                if (panel != null)
                    panel.SetActive(false);
                completed?.Invoke();
            });
        }

        public void ShowPrevious()
        {
            if (characters.Count == 0)
                return;

            selectedIndex = (selectedIndex - 1 + characters.Count) % characters.Count;
            Refresh();
        }

        public void ShowNext()
        {
            if (characters.Count == 0)
                return;

            selectedIndex = (selectedIndex + 1) % characters.Count;
            Refresh();
        }

        public void Continue()
        {
            CharacterData selected = SelectedCharacter;
            if (selected == null || !selected.IsSelectable || missionMenu == null)
                return;

            CharacterRunState.Select(selected);
            missionMenu.onClosed = ReturnFromMissionMenu;
            Close(() => missionMenu.Open());
        }

        public void BackToMainMenu()
        {
            Close(() =>
            {
                if (mainMenuManager != null)
                    mainMenuManager.ShowMainMenu();
            });
        }

        void ReturnFromMissionMenu()
        {
            Open();
        }

        void Refresh()
        {
            CharacterData selected = SelectedCharacter;
            bool hasCharacter = selected != null;

            if (nameText != null)
                nameText.text = hasCharacter ? selected.characterName : "Sin personajes";
            if (descriptionText != null)
                descriptionText.text = hasCharacter ? selected.description : "";
            if (mechanicText != null)
                mechanicText.text = hasCharacter ? selected.mechanicDescription : "";
            if (statsText != null)
            {
                statsText.text = hasCharacter
                    ? $"Vida {selected.maxHealth}   Mana {selected.maxMana}   Robo {selected.cardsPerTurn}   Oro {selected.startingGold}"
                    : "";
            }

            if (portraitImage != null)
            {
                portraitImage.sprite = hasCharacter ? selected.portrait : null;
                portraitImage.enabled = hasCharacter && selected.portrait != null;
            }

            bool selectable = hasCharacter && selected.IsSelectable;
            if (continueButton != null)
                continueButton.interactable = selectable;
            if (lockText != null)
            {
                lockText.gameObject.SetActive(hasCharacter && !selectable);
                lockText.text = hasCharacter && !selectable ? selected.GetLockedMessage() : "";
            }
            if (subclassesButton != null)
            {
                subclassesButton.gameObject.SetActive(
                    hasCharacter &&
                    selected.subclasses != null &&
                    selected.subclasses.Count > 0
                );
            }

            RenderDeck(selected);
        }

        void RenderDeck(CharacterData selected)
        {
            if (deckContent == null)
                return;

            for (int i = deckContent.childCount - 1; i >= 0; i--)
                Destroy(deckContent.GetChild(i).gameObject);

            if (selected == null || cardPrefab == null)
                return;

            for (int i = 0; i < selected.startingDeck.Count; i++)
            {
                CardData cardData = selected.startingDeck[i];
                if (cardData == null)
                    continue;

                GameObject instance = Instantiate(cardPrefab, deckContent);
                RectTransform cardRect = instance.transform as RectTransform;
                if (cardRect != null)
                    cardRect.sizeDelta = deckCardSlotSize;

                CardView view = instance.GetComponentInChildren<CardView>();
                if (view != null)
                {
                    view.transform.localScale = Vector3.Scale(view.transform.localScale, deckCardScale);
                    view.Setup(new Card(cardData), null);
                    view.interactable = false;
                }

                Button button = view != null ? view.GetComponent<Button>() : instance.GetComponent<Button>();
                if (button != null)
                    button.interactable = false;
            }
        }

        int FindInitialIndex()
        {
            if (characters.Count == 0)
                return 0;

            CharacterData previous = CharacterRunState.SelectedCharacter;
            if (previous != null)
            {
                int index = characters.IndexOf(previous);
                if (index >= 0)
                    return index;
            }

            return 0;
        }

        public void ShowSubclasses()
        {
            if (subclassInfoPanel != null && SelectedCharacter != null)
                subclassInfoPanel.Open(SelectedCharacter);
        }

        void FadeTo(float target, System.Action completed = null)
        {
            if (canvasGroup == null)
            {
                completed?.Invoke();
                return;
            }

            if (fadeRoutine != null)
                StopCoroutine(fadeRoutine);
            fadeRoutine = StartCoroutine(FadeRoutine(target, completed));
        }

        IEnumerator FadeRoutine(float target, System.Action completed)
        {
            float start = canvasGroup.alpha;
            float elapsed = 0f;
            bool visible = target > 0.01f;
            canvasGroup.interactable = visible;
            canvasGroup.blocksRaycasts = visible;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Lerp(start, target, Mathf.Clamp01(elapsed / fadeDuration));
                yield return null;
            }

            canvasGroup.alpha = target;
            completed?.Invoke();
        }
    }
}
