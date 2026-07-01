using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using JuegoDeCartas.Articulos;
using JuegoDeCartas.Cards;
using JuegoDeCartas.Challenges;
using JuegoDeCartas.Characters;
using JuegoDeCartas.Enemies;
using JuegoDeCartas.Missions;
using JuegoDeCartas.Progression;
using JuegoDeCartas.Relics;

namespace JuegoDeCartas.UI
{
    public class CollectionMenu : MonoBehaviour
    {
        public enum CollectionTab
        {
            Enemies,
            Heroes,
            Items,
            Packs,
            Cards,
            Relics
        }

        [Header("Panel")]
        public GameObject panel;
        public MainMenuManager mainMenuManager;

        [Header("Tabs")]
        public Button enemiesButton;
        public Button heroesButton;
        public Button itemsButton;
        public Button packsButton;
        public Button cardsButton;
        public Button relicsButton;
        public Button backButton;

        [Header("Entries")]
        public Transform entriesContent;
        public GameObject entryPrefab;
        public List<EnemyData> enemies = new List<EnemyData>();
        public List<CharacterData> heroes = new List<CharacterData>();
        public List<ArticuloData> items = new List<ArticuloData>();
        public List<ItemPackData> packs = new List<ItemPackData>();
        public List<CardData> cards = new List<CardData>();
        public List<RelicData> relics = new List<RelicData>();
        public List<ChallengeData> challenges = new List<ChallengeData>();

        [Header("Detail")]
        public Image detailImage;
        public TextMeshProUGUI detailNameText;
        public TextMeshProUGUI detailDescriptionText;
        public TextMeshProUGUI detailStatsText;
        public TextMeshProUGUI detailProgressText;
        public GameObject undiscoveredOverlay;
        public Button subclassesButton;
        public SubclassInfoPanelUI subclassInfoPanel;
        public Button deckToggleButton;
        public TextMeshProUGUI deckToggleLabel;
        public string showDeckLabel = "Mazo inicial";
        public string hideDeckLabel = "Cerrar mazo";
        public Vector2 deckButtonInfoAnchorMin = new Vector2(0.17f, 0.49f);
        public Vector2 deckButtonInfoAnchorMax = new Vector2(0.30f, 0.55f);
        public Vector2 deckButtonOpenAnchorMin = new Vector2(0.82f, 0.75f);
        public Vector2 deckButtonOpenAnchorMax = new Vector2(0.95f, 0.82f);

        [Header("Hero Deck")]
        public GameObject deckSection;
        public Transform deckContent;
        public GameObject cardPrefab;
        public Vector2 deckCardSlotSize = new Vector2(150f, 220f);
        public Vector2 deckCardSpacing = new Vector2(36f, 36f);
        public bool applyDeckGridLayout = true;

        CharacterData currentHero;
        bool heroDeckVisible;

        void Awake()
        {
            if (panel == null)
                panel = gameObject;

            if (enemiesButton != null)
                enemiesButton.onClick.AddListener(ShowEnemies);
            if (heroesButton != null)
                heroesButton.onClick.AddListener(ShowHeroes);
            if (itemsButton != null)
                itemsButton.onClick.AddListener(ShowItems);
            if (packsButton != null)
                packsButton.onClick.AddListener(ShowPacks);
            if (cardsButton != null)
                cardsButton.onClick.AddListener(ShowCards);
            if (relicsButton != null)
                relicsButton.onClick.AddListener(ShowRelics);
            if (backButton != null)
                backButton.onClick.AddListener(Close);
            if (subclassesButton != null)
                subclassesButton.onClick.AddListener(ShowCurrentHeroSubclasses);
            if (deckToggleButton != null)
                deckToggleButton.onClick.AddListener(ToggleHeroDeck);
        }

        public void Open()
        {
            if (panel == null)
                return;

            panel.SetActive(true);
            ShowHeroes();
        }

        public void Close()
        {
            if (subclassInfoPanel != null)
                subclassInfoPanel.Close();
            if (panel != null)
                panel.SetActive(false);
            if (mainMenuManager != null)
                mainMenuManager.ShowMainMenu();
        }

        public void ShowEnemies()
        {
            SetHeroControlsVisible(false);
            ClearEntries();

            for (int i = 0; i < enemies.Count; i++)
            {
                EnemyData enemy = enemies[i];
                if (enemy == null)
                    continue;

                bool discovered = CollectionProgress.IsEnemySeen(enemy);
                CreateEntry(enemy.enemyName, enemy.sprite, discovered, () => ShowEnemy(enemy));
            }

            if (enemies.Count > 0)
                ShowEnemy(enemies[0]);
            else
                ClearDetail();
        }

        public void ShowHeroes()
        {
            SetHeroControlsVisible(true);
            ClearEntries();

            for (int i = 0; i < heroes.Count; i++)
            {
                CharacterData hero = heroes[i];
                if (hero == null)
                    continue;

                CreateEntry(hero.characterName, hero.portrait, true, () => ShowHero(hero));
            }

            if (heroes.Count > 0)
                ShowHero(heroes[0]);
            else
                ClearDetail();
        }

        public void ShowItems()
        {
            SetHeroControlsVisible(false);
            ClearEntries();

            for (int i = 0; i < items.Count; i++)
            {
                ArticuloData item = items[i];
                if (item == null)
                    continue;

                bool discovered = CollectionProgress.IsItemSeen(item);
                CreateEntry(item.nombre, item.imagen, discovered, () => ShowItem(item));
            }

            if (items.Count > 0)
                ShowItem(items[0]);
            else
                ClearDetail();
        }

        public void ShowPacks()
        {
            SetHeroControlsVisible(false);
            ClearEntries();

            for (int i = 0; i < packs.Count; i++)
            {
                ItemPackData pack = packs[i];
                if (pack == null)
                    continue;

                bool discovered = CollectionProgress.IsPackSeen(pack);
                CreateEntry(pack.packName, pack.image, discovered, () => ShowPack(pack));
            }

            if (packs.Count > 0)
                ShowPack(packs[0]);
            else
                ClearDetail();
        }

        public void ShowCards()
        {
            SetHeroControlsVisible(false);
            ClearEntries();

            for (int i = 0; i < cards.Count; i++)
            {
                CardData card = cards[i];
                if (card == null)
                    continue;

                CreateEntry(card.cardName, card.sprite, true, () => ShowCard(card));
            }

            if (cards.Count > 0)
                ShowCard(cards[0]);
            else
                ClearDetail();
        }

        public void ShowRelics()
        {
            SetHeroControlsVisible(false);
            ClearEntries();

            for (int i = 0; i < relics.Count; i++)
            {
                RelicData relic = relics[i];
                if (relic == null)
                    continue;

                bool discovered = CollectionProgress.IsRelicSeen(relic);
                CreateEntry(
                    relic.relicName,
                    relic.icon,
                    discovered,
                    () => ShowRelic(relic)
                );
            }

            if (relics.Count > 0)
                ShowRelic(relics[0]);
            else
                ClearDetail();
        }

        void CreateEntry(string displayName, Sprite sprite, bool discovered, System.Action selected)
        {
            if (entryPrefab == null || entriesContent == null)
                return;

            GameObject instance = Instantiate(entryPrefab, entriesContent);
            CollectionEntryUI entry = instance.GetComponent<CollectionEntryUI>();
            if (entry != null)
                entry.Setup(displayName, sprite, discovered, selected);
        }

        void ShowEnemy(EnemyData enemy)
        {
            ClearDeck();
            SetDeckVisible(false);
            bool discovered = enemy != null && CollectionProgress.IsEnemySeen(enemy);
            SetDiscovered(discovered);

            if (!discovered || enemy == null)
            {
                SetDetail("Enemigo desconocido", null, "Encuentralo durante una run para revelar su informacion.", "", "");
                return;
            }

            string tier = enemy.enemyTier switch
            {
                EnemyTier.Boss => "Jefe",
                EnemyTier.MiniBoss => "Minijefe",
                _ => "Normal"
            };

            string mechanics = BuildMechanicDescription(enemy);
            SetDetail(
                enemy.enemyName,
                enemy.sprite,
                mechanics,
                $"Tipo: {tier}\nVida: {enemy.maxHealth}\nDaño: {enemy.minDamage}-{enemy.maxDamage}\nArmadura: {enemy.startArmor}",
                $"Derrotado: {CollectionProgress.GetEnemyDefeatedCount(enemy)} veces"
            );
        }

        void ShowHero(CharacterData hero)
        {
            currentHero = hero;
            SetDiscovered(true);
            SetHeroControlsVisible(true);
            SetHeroDeckMode(false);
            RenderDeck(hero);

            if (hero == null)
            {
                ClearDetail();
                return;
            }

            string availability = hero.IsSelectable
                ? "Disponible"
                : hero.comingSoon ? "Proximamente" : hero.GetLockedMessage();

            SetDetail(
                hero.characterName,
                hero.portrait,
                hero.description + "\n\n" + hero.mechanicDescription,
                $"Vida: {hero.maxHealth}\nMana: {hero.maxMana}\nRobo: {hero.cardsPerTurn}\nOro inicial: {hero.startingGold}",
                BuildHeroProgress(hero, availability)
            );
        }

        void ShowItem(ArticuloData item)
        {
            ClearDeck();
            SetDeckVisible(false);
            bool discovered = item != null && CollectionProgress.IsItemSeen(item);
            SetDiscovered(discovered);

            if (!discovered || item == null)
            {
                SetDetail("Articulo desconocido", null, "Abre un sobre que lo contenga para revelarlo.", "", "");
                return;
            }

            SetDetail(
                item.nombre,
                item.imagen,
                item.descripcion,
                $"Rareza: {GetRarityLabel(item.rareza)}\nEfecto: {GetEffectLabel(item.tipoEfecto)}\nCantidad: {item.cantidad}",
                $"Usado: {CollectionProgress.GetItemUsedCount(item)} veces"
            );
        }

        void ShowPack(ItemPackData pack)
        {
            ClearDeck();
            SetDeckVisible(false);
            bool discovered = pack != null && CollectionProgress.IsPackSeen(pack);
            SetDiscovered(discovered);

            if (!discovered || pack == null)
            {
                SetDetail("Sobre desconocido", null, "Encuentralo en una tienda para revelar su contenido.", "", "");
                return;
            }

            var rarityBuilder = new StringBuilder();
            for (int i = 0; i < pack.rarityWeights.Count; i++)
            {
                PackRarityWeight weight = pack.rarityWeights[i];
                if (weight == null || weight.weight <= 0f)
                    continue;
                if (rarityBuilder.Length > 0)
                    rarityBuilder.Append(" | ");
                rarityBuilder.Append(GetRarityLabel(weight.rarity));
                rarityBuilder.Append(": ");
                rarityBuilder.Append(weight.weight);
                rarityBuilder.Append("%");
            }

            SetDetail(
                pack.packName,
                pack.image,
                pack.description,
                $"Precio base: {pack.price}\nOpciones: {pack.choiceCount}\nRareza visual: {GetRarityLabel(pack.displayRarity)}",
                rarityBuilder.ToString()
            );
        }

        void ShowCard(CardData card)
        {
            ClearDeck();
            SetDeckVisible(false);
            SetDiscovered(true);

            if (card == null)
            {
                ClearDetail();
                return;
            }

            var upgrades = new StringBuilder();
            upgrades.AppendLine($"Usada: {CollectionProgress.GetCardUsedCount(card)} veces");
            if (card.upgradeOptions != null && card.upgradeOptions.Count > 0)
            {
                upgrades.AppendLine();
                upgrades.AppendLine("Mejoras:");
                for (int i = 0; i < card.upgradeOptions.Count; i++)
                {
                    CardUpgradeOption option = card.upgradeOptions[i];
                    if (option == null)
                        continue;

                    upgrades.Append("- ");
                    upgrades.Append(option.upgradeName);
                    if (!string.IsNullOrWhiteSpace(option.description))
                    {
                        upgrades.Append(": ");
                        upgrades.Append(option.description);
                    }
                    upgrades.AppendLine();
                }
            }

            List<CardEpiphany> epiphanies = card.GetEpiphanyOptions();
            if (epiphanies.Count > 0)
            {
                upgrades.AppendLine();
                upgrades.AppendLine("Epifanias:");
                for (int i = 0; i < epiphanies.Count; i++)
                {
                    CardEpiphany epiphany = epiphanies[i];
                    if (epiphany == null)
                        continue;

                    upgrades.Append("- ");
                    if (!CollectionProgress.IsEpiphanySeen(card, i))
                    {
                        upgrades.AppendLine("Epifania desconocida");
                        continue;
                    }

                    upgrades.Append(epiphany.epiphanyName);
                    if (!string.IsNullOrWhiteSpace(epiphany.description))
                    {
                        upgrades.Append(": ");
                        upgrades.Append(epiphany.description);
                    }
                    upgrades.AppendLine();
                }
            }

            SetDetail(
                card.cardName,
                card.sprite,
                card.description,
                $"Coste: {card.cost}\nSe destruye al usar: {(card.destroyOnUse ? "Si" : "No")}\nEfectos: {(card.effects != null ? card.effects.Count : 0)}",
                upgrades.ToString().TrimEnd()
            );
        }

        void ShowRelic(RelicData relic)
        {
            ClearDeck();
            SetDeckVisible(false);
            bool discovered = relic != null &&
                              CollectionProgress.IsRelicSeen(relic);
            SetDiscovered(discovered);

            if (!discovered || relic == null)
            {
                SetDetail(
                    "Reliquia desconocida",
                    null,
                    "Consiguela en una tienda para revelar su informacion.",
                    "",
                    ""
                );
                return;
            }

            SetDetail(
                relic.relicName,
                relic.icon,
                relic.description,
                $"Rareza: {GetRarityLabel(relic.rarity)}\nPrecio base: {relic.price}",
                $"Conseguida: {CollectionProgress.GetRelicAcquiredCount(relic)} veces"
            );
        }

        string BuildHeroProgress(CharacterData hero, string availability)
        {
            var builder = new StringBuilder();
            builder.AppendLine(availability);
            builder.AppendLine($"Runs: {CollectionProgress.GetHeroRuns(hero)}");
            builder.AppendLine($"Victorias: {CollectionProgress.GetHeroWins(hero)}");
            builder.AppendLine($"Mayor daño de carta: {CollectionProgress.GetHeroMaxCardDamage(hero)}");
            builder.AppendLine($"Mayor daño en run: {CollectionProgress.GetHeroMaxRunDamage(hero)}");
            builder.AppendLine($"Armadura maxima: {CollectionProgress.GetHeroMaxArmor(hero)}");
            builder.AppendLine($"Enemigos derrotados: {CollectionProgress.GetHeroEnemiesDefeated(hero)}");
            builder.AppendLine($"Cartas usadas: {CollectionProgress.GetHeroCardsUsed(hero)}");
            builder.AppendLine();
            builder.Append("Medallas: ");

            for (int i = 1; i <= 3; i++)
            {
                MissionDifficulty difficulty = (MissionDifficulty)i;
                bool completed = MissionData.IsAnyDifficultyCompletedByCharacter(difficulty, hero);
                if (i > 1)
                    builder.Append(" | ");
                builder.Append(GetDifficultyLabel(difficulty));
                builder.Append(completed ? " OK" : " -");
            }

            builder.AppendLine();
            builder.Append("Retos especiales: ");
            int challengeCount = 0;
            int completedChallenges = 0;
            for (int i = 0; i < challenges.Count; i++)
            {
                ChallengeData challenge = challenges[i];
                if (challenge == null || !challenge.CountsForCompletion)
                    continue;

                challengeCount++;
                if (challenge.IsCompletedByCharacter(hero))
                    completedChallenges++;
            }
            builder.Append(completedChallenges);
            builder.Append("/");
            builder.Append(challengeCount);

            return builder.ToString();
        }

        public void ShowCurrentHeroSubclasses()
        {
            if (subclassInfoPanel != null && currentHero != null)
                subclassInfoPanel.Open(currentHero);
        }

        public void ToggleHeroDeck()
        {
            if (currentHero != null)
                SetHeroDeckMode(!heroDeckVisible);
        }

        void SetHeroControlsVisible(bool visible)
        {
            if (!visible)
            {
                currentHero = null;
                SetHeroDeckMode(false);
            }

            if (subclassesButton != null)
                subclassesButton.gameObject.SetActive(visible);
            if (deckToggleButton != null)
                deckToggleButton.gameObject.SetActive(visible);
        }

        void SetHeroDeckMode(bool visible)
        {
            heroDeckVisible = visible;
            SetDeckVisible(visible);

            SetInfoObjectVisible(detailImage != null ? detailImage.gameObject : null, !visible);
            SetInfoObjectVisible(detailNameText != null ? detailNameText.gameObject : null, !visible);
            SetInfoObjectVisible(detailDescriptionText != null ? detailDescriptionText.gameObject : null, !visible);
            SetInfoObjectVisible(detailStatsText != null ? detailStatsText.gameObject : null, !visible);
            SetInfoObjectVisible(detailProgressText != null ? detailProgressText.gameObject : null, !visible);
            SetInfoObjectVisible(subclassesButton != null ? subclassesButton.gameObject : null, !visible);
            SetInfoObjectVisible(undiscoveredOverlay, !visible && undiscoveredOverlay != null && undiscoveredOverlay.activeSelf);

            if (deckToggleLabel != null)
                deckToggleLabel.text = visible ? hideDeckLabel : showDeckLabel;
            if (deckToggleButton != null)
            {
                RectTransform buttonRect = deckToggleButton.transform as RectTransform;
                if (buttonRect != null)
                {
                    buttonRect.anchorMin = visible
                        ? deckButtonOpenAnchorMin
                        : deckButtonInfoAnchorMin;
                    buttonRect.anchorMax = visible
                        ? deckButtonOpenAnchorMax
                        : deckButtonInfoAnchorMax;
                    buttonRect.offsetMin = Vector2.zero;
                    buttonRect.offsetMax = Vector2.zero;
                }
                deckToggleButton.transform.SetAsLastSibling();
            }
        }

        static void SetInfoObjectVisible(GameObject target, bool visible)
        {
            if (target != null)
                target.SetActive(visible);
        }

        static string BuildMechanicDescription(EnemyData enemy)
        {
            if (enemy.mechanics == null || enemy.mechanics.Count == 0)
                return "Sin mecanicas especiales.";

            var builder = new StringBuilder();
            for (int i = 0; i < enemy.mechanics.Count; i++)
            {
                EnemyMechanicData mechanic = enemy.mechanics[i];
                if (mechanic == null)
                    continue;

                if (builder.Length > 0)
                    builder.AppendLine();
                builder.Append(string.IsNullOrWhiteSpace(mechanic.mechanicName)
                    ? mechanic.mechanicType.ToString()
                    : mechanic.mechanicName);
            }

            return builder.Length > 0 ? builder.ToString() : "Sin mecanicas especiales.";
        }

        void RenderDeck(CharacterData hero)
        {
            ClearDeck();
            if (hero == null || deckContent == null || cardPrefab == null)
                return;

            ApplyDeckGridLayout();

            for (int i = 0; i < hero.startingDeck.Count; i++)
            {
                CardData cardData = hero.startingDeck[i];
                if (cardData == null)
                    continue;

                GameObject instance = Instantiate(cardPrefab, deckContent);
                RectTransform cardRect = instance.transform as RectTransform;
                if (cardRect != null)
                    cardRect.sizeDelta = deckCardSlotSize;

                CardView view = instance.GetComponentInChildren<CardView>();
                if (view != null)
                {
                    view.Setup(new Card(cardData), null);
                    view.interactable = false;
                    view.inspectionEnabled = true;
                }
            }
        }

        void ApplyDeckGridLayout()
        {
            if (!applyDeckGridLayout || deckContent == null)
                return;

            GridLayoutGroup grid = deckContent.GetComponent<GridLayoutGroup>();
            if (grid == null)
                return;

            grid.cellSize = deckCardSlotSize;
            grid.spacing = deckCardSpacing;
        }

        void ClearEntries()
        {
            if (entriesContent == null)
                return;

            for (int i = entriesContent.childCount - 1; i >= 0; i--)
                Destroy(entriesContent.GetChild(i).gameObject);
        }

        void ClearDeck()
        {
            if (deckContent == null)
                return;

            for (int i = deckContent.childCount - 1; i >= 0; i--)
                Destroy(deckContent.GetChild(i).gameObject);
        }

        void SetDetail(string title, Sprite sprite, string description, string stats, string progress)
        {
            if (detailNameText != null)
                detailNameText.text = title;
            if (detailDescriptionText != null)
                detailDescriptionText.text = description;
            if (detailStatsText != null)
                detailStatsText.text = stats;
            if (detailProgressText != null)
                detailProgressText.text = progress;
            if (detailImage != null)
            {
                detailImage.sprite = sprite;
                detailImage.enabled = sprite != null;
            }
        }

        void ClearDetail()
        {
            SetDetail("", null, "", "", "");
            ClearDeck();
        }

        void SetDiscovered(bool discovered)
        {
            if (undiscoveredOverlay != null)
                undiscoveredOverlay.SetActive(!discovered);
        }

        void SetDeckVisible(bool visible)
        {
            if (deckSection != null)
                deckSection.SetActive(visible);
        }

        static string GetRarityLabel(Rareza rarity)
        {
            return rarity switch
            {
                Rareza.Raro => "Raro",
                Rareza.Epico => "Epico",
                _ => "Comun"
            };
        }

        static string GetEffectLabel(TipoEfectoArticulo effect)
        {
            return effect.ToString();
        }

        static string GetRarityLabel(RelicRarity rarity)
        {
            return rarity switch
            {
                RelicRarity.Rara => "Rara",
                RelicRarity.Epica => "Epica",
                _ => "Comun"
            };
        }

        static string GetDifficultyLabel(MissionDifficulty difficulty)
        {
            return difficulty switch
            {
                MissionDifficulty.Media => "Media",
                MissionDifficulty.Letal => "Letal",
                _ => "Facil"
            };
        }
    }
}
