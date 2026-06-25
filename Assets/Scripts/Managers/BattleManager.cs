using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using JuegoDeCartas.Cards;
using JuegoDeCartas.Enemies;
using JuegoDeCartas.Missions;
using JuegoDeCartas.UI;
using JuegoDeCartas.Characters;
using JuegoDeCartas.Progression;
using JuegoDeCartas.Challenges;
using JuegoDeCartas.Relics;
using JuegoDeCartas.Effects;

namespace JuegoDeCartas.Managers
{
    public class BattleManager : MonoBehaviour
    {
        [Header("Player")]
        public Entity player;

        [Header("Systems")]
        public DeckManager deckManager;
        public TurnManager turnManager;
        public UIManager uiManager;
        public DeckViewerUI deckViewer;
        public CardSelectionUI mechanicCardSelectionUI;
        public GameManager gameManager;
        public JuegoDeCartas.Stats.GameStatsTracker statsTracker;
        public RelicInventory relicInventory;

        [Header("Wave")]
        public WaveManager waveManager = new WaveManager();

        [Header("Hand")]
        public HandRenderer handRenderer = new HandRenderer();

        private int lastCardDamageDealt;
        private bool battleEnded;
        private bool runRecorded;
        private bool subclassApplied;
        private int cardsPlayedThisTurn;
        private int pendingFirstCardDamageBonus;
        private bool resolvingCard;
        private bool pendingPostCombatTransition;
        private bool currentCardAttemptedDamage;
        private readonly List<Card> frozenCards = new List<Card>();
        private int combatDamageBonus;
        private int burnedCardsThisCombat;
        private int burnedCardsThisTurn;
        private readonly Dictionary<CombatMechanicEffect, Card> preparedMechanicTargets =
            new Dictionary<CombatMechanicEffect, Card>();
        private Card awaitingMechanicTargetSource;
        private CombatMechanicEffect awaitingMechanicTargetEffect;
        private const int FrozenPlayerTurns = 3;

        [HideInInspector] public int armorPerTurn;
        [HideInInspector] public int regenPerRound;
        [HideInInspector] public int playerDamageBonus;
        [HideInInspector] public int playerDamageBonusTurnsRemaining;

        public Enemy enemy => waveManager.enemy;
        public bool IsBattleEnded => battleEnded;
        public SubclassData ActiveSubclass => CharacterRunState.SelectedSubclass;
        public bool ArePlayerDamageBuffsStackable =>
            HasPassive(SubclassPassiveType.StackDamageBuffs);
        public int FrozenCardCount => GetFrozenCardCount();
        public int CombatDamageBonus => combatDamageBonus;
        public int BurnedCardsThisCombat => burnedCardsThisCombat;
        public int BurnedCardsThisTurn => burnedCardsThisTurn;
        public const int StunThreshold = 5;

        void OnDestroy()
        {
            if (deckManager != null)
                deckManager.OnDeckChanged -= RefreshDeckUI;

            waveManager.OnWaveCleared -= OnWaveCleared;
            waveManager.OnEnemyDefeated -= OnEnemyDefeated;
        }

        void Start()
        {
            Time.timeScale = 1f;
            if (relicInventory != null)
                relicInventory.Initialize(this);
            ApplySelectedCharacter();
            CollectionProgress.RegisterRunStarted(CharacterRunState.SelectedCharacter);

            if (uiManager != null)
                uiManager.Init(this);

            if (deckManager != null)
                deckManager.OnDeckChanged += RefreshDeckUI;

            ApplySelectedMission();

            waveManager.uiManager = uiManager;
            waveManager.gameManager = gameManager;
            waveManager.statsTracker = statsTracker;
            waveManager.enemyHealthBar = GetEnemyHealthBar();
            waveManager.battleManager = this;

            waveManager.OnWaveCleared += OnWaveCleared;
            waveManager.OnEnemyDefeated += OnEnemyDefeated;

            waveManager.Initialize();
            if (relicInventory != null)
                relicInventory.OnCombatStarted();

            if (deckManager != null)
                deckManager.InitializeDeck();

            if (turnManager != null)
                turnManager.StartGame();
        }

        void Update()
        {
            if (awaitingMechanicTargetSource == null)
                return;

            if (!Input.GetMouseButtonDown(0))
                return;

            if (PointerIsOverCardView())
                return;

            CancelMechanicTargetSelection();
        }

        public bool ActivateSubclass(SubclassData subclass)
        {
            if (!CharacterRunState.SelectSubclass(subclass))
                return false;

            ApplySelectedSubclassPassive();
            CollectionProgress.MarkSubclassSeen(subclass);
            ProfilePrefs.Save();
            RenderHand();
            if (deckManager != null)
                deckManager.OnDeckChanged?.Invoke();
            UpdateUI();
            return true;
        }

        void ApplySelectedSubclassPassive()
        {
            if (subclassApplied || ActiveSubclass == null || player == null)
                return;

            subclassApplied = true;

            if (ActiveSubclass.passiveType == SubclassPassiveType.BonusMaxMana)
            {
                int bonus = Mathf.Max(0, ActiveSubclass.amount);
                player.stats.maxMana += bonus;
                player.stats.mana += bonus;
                player.stats.Clamp();
            }
        }

        public void ApplySelectedCharacter()
        {
            CharacterData character = CharacterRunState.SelectedCharacter;
            if (character == null || player == null || deckManager == null)
                return;

            player.stats.maxHealth = Mathf.Max(1, character.maxHealth);
            player.stats.health = player.stats.maxHealth;
            player.stats.maxMana = Mathf.Max(0, character.maxMana);
            player.stats.mana = player.stats.maxMana;
            player.stats.armor = 0;
            deckManager.cardsPerTurn = Mathf.Max(0, character.cardsPerTurn);
            deckManager.startingDeck = new List<CardData>(character.startingDeck);

            if (gameManager != null)
                gameManager.dinero = Mathf.Max(0, character.startingGold);
        }

        void ApplySelectedMission()
        {
            MissionData mission = ChallengeRunState.IsChallengeRun
                ? ChallengeRunState.EncounterMission
                : MissionRunState.SelectedMission;
            if (mission == null)
                return;

            waveManager.normalEnemies.Clear();
            waveManager.miniBossEnemies.Clear();

            if (mission.possibleNormalEnemies != null)
            {
                for (int i = 0; i < mission.possibleNormalEnemies.Count; i++)
                {
                    if (mission.possibleNormalEnemies[i] != null)
                        waveManager.normalEnemies.Add(mission.possibleNormalEnemies[i]);
                }
            }

            if (mission.possibleMiniBosses != null)
            {
                for (int i = 0; i < mission.possibleMiniBosses.Count; i++)
                {
                    if (mission.possibleMiniBosses[i] != null)
                        waveManager.miniBossEnemies.Add(mission.possibleMiniBosses[i]);
                }
            }

            waveManager.finalBoss = mission.boss;
            waveManager.totalCombats = Mathf.Max(
                1,
                ChallengeRunState.CombatCountOverride > 0
                    ? ChallengeRunState.CombatCountOverride
                    : mission.combatCount
            );
            waveManager.miniBossFrequency = Mathf.Max(1, mission.miniBossFrequency);
        }

        EnemyHealthBar GetEnemyHealthBar()
        {
            if (waveManager.enemyHealthBar != null)
                return waveManager.enemyHealthBar;

            var bars = FindObjectsByType<EnemyHealthBar>(FindObjectsInactive.Include);
            for (int i = 0; i < bars.Length; i++)
            {
                if (bars[i].target == EnemyHealthBar.HealthTarget.Enemy)
                    return bars[i];
            }

            return null;
        }

        void RefreshDeckUI()
        {
            if (deckViewer != null && deckViewer.panel != null && deckViewer.panel.activeSelf)
                deckViewer.ShowRemaining();
        }

        void OnWaveCleared()
        {
            battleEnded = true;

            if (statsTracker != null)
                statsTracker.PopulateStatsText();

            MissionData mission = MissionRunState.SelectedMission;
            if (ChallengeRunState.IsChallengeRun)
            {
                ChallengeRunState.MarkCompleted(CharacterRunState.SelectedCharacter);
            }
            else if (mission != null)
            {
                mission.MarkCompleted(
                    MissionRunState.SelectedDifficulty,
                    CharacterRunState.SelectedCharacter
                );
            }

            RecordRun(true);

            if (gameManager != null)
                gameManager.ShowVictory();
        }

        void OnEnemyDefeated()
        {
        }

        public void DrawCards(int amount)
        {
            if (!battleEnded && deckManager != null)
                deckManager.DrawCards(amount);
        }

        public void HandleCardClicked(Card card)
        {
            if (TryResolveMechanicTargetClick(card))
                return;

            PlayCard(card);
        }

        bool TryPrepareCardMechanicSelection(Card card)
        {
            if (card == null ||
                card.data == null ||
                deckManager == null ||
                awaitingMechanicTargetSource != null)
            {
                return false;
            }

            CombatMechanicEffect effect = GetFirstMissingMechanicTarget(card);
            if (effect == null)
                return false;

            List<Card> candidates = GetMechanicTargetCandidates(card, effect);
            if (candidates.Count == 0)
                return false;

            awaitingMechanicTargetSource = card;
            awaitingMechanicTargetEffect = effect;
            RenderHand();
            return true;
        }

        bool TryResolveMechanicTargetClick(Card clickedCard)
        {
            if (awaitingMechanicTargetSource == null)
                return false;

            Card source = awaitingMechanicTargetSource;
            CombatMechanicEffect effect = awaitingMechanicTargetEffect;

            if (clickedCard == null ||
                ReferenceEquals(clickedCard, source) ||
                !IsValidMechanicTarget(source, effect, clickedCard))
            {
                CancelMechanicTargetSelection();
                return true;
            }

            preparedMechanicTargets[effect] = clickedCard;
            awaitingMechanicTargetSource = null;
            awaitingMechanicTargetEffect = null;
            RenderHand();
            PlayCard(source);
            return true;
        }

        void CancelMechanicTargetSelection()
        {
            preparedMechanicTargets.Clear();
            awaitingMechanicTargetSource = null;
            awaitingMechanicTargetEffect = null;
            RenderHand();
        }

        bool IsValidMechanicTarget(
            Card source,
            CombatMechanicEffect effect,
            Card candidate)
        {
            if (source == null || effect == null || candidate == null)
                return false;

            return GetMechanicTargetCandidates(source, effect).Contains(candidate);
        }

        CombatMechanicEffect GetFirstMissingMechanicTarget(Card card)
        {
            if (card?.data?.effects == null)
                return null;

            for (int i = 0; i < card.data.effects.Count; i++)
            {
                if (card.data.effects[i] is CombatMechanicEffect effect &&
                    effect.RequiresCardTarget &&
                    !preparedMechanicTargets.ContainsKey(effect))
                {
                    return effect;
                }
            }

            return null;
        }

        List<Card> GetMechanicTargetCandidates(
            Card source,
            CombatMechanicEffect effect)
        {
            List<Card> candidates = new List<Card>();
            if (effect == null || deckManager == null)
                return candidates;

            if (effect.action == CombatMechanicAction.BurnCards)
            {
                for (int i = 0; i < deckManager.hand.Count; i++)
                {
                    Card candidate = deckManager.hand[i];
                    if (candidate != null &&
                        candidate != source &&
                        candidate.data != null)
                    {
                        candidates.Add(candidate);
                    }
                }
            }
            else if (effect.action == CombatMechanicAction.FreezeCards)
            {
                for (int i = 0; i < deckManager.hand.Count; i++)
                {
                    Card candidate = deckManager.hand[i];
                    if (candidate != null &&
                        candidate != source &&
                        candidate.data != null &&
                        !candidate.frozen)
                    {
                        candidates.Add(candidate);
                    }
                }
            }
            else if (effect.action == CombatMechanicAction.ShatterFrozenCards)
            {
                AddFrozenTargetCandidates(candidates, frozenCards);
                AddFrozenTargetCandidates(candidates, deckManager.hand);
                candidates.Remove(source);
            }

            return candidates;
        }

        void AddFrozenTargetCandidates(List<Card> candidates, List<Card> source)
        {
            if (source == null)
                return;

            for (int i = 0; i < source.Count; i++)
            {
                Card candidate = source[i];
                if (candidate != null &&
                    candidate.data != null &&
                    candidate.frozen &&
                    !candidates.Contains(candidate))
                {
                    candidates.Add(candidate);
                }
            }
        }

        CardSelectionUI GetMechanicCardSelectionUI()
        {
            if (mechanicCardSelectionUI != null)
                return mechanicCardSelectionUI;

            mechanicCardSelectionUI =
                FindAnyObjectByType<CardSelectionUI>(FindObjectsInactive.Include);
            return mechanicCardSelectionUI;
        }

        string GetMechanicTargetSelectionTitle(CombatMechanicEffect effect)
        {
            if (effect == null)
                return "Elige una carta";

            return effect.action switch
            {
                CombatMechanicAction.BurnCards => "Elige una carta para quemar",
                CombatMechanicAction.ShatterFrozenCards => "Elige una carta congelada",
                _ => "Elige una carta"
            };
        }

        public bool TryConsumePreparedMechanicTarget(
            CombatMechanicEffect effect,
            out Card target)
        {
            target = null;
            if (effect == null)
                return false;

            if (!preparedMechanicTargets.TryGetValue(effect, out target))
                return false;

            preparedMechanicTargets.Remove(effect);
            return target != null;
        }

        public bool IsCardAwaitingMechanicTarget(Card card)
        {
            return card != null && ReferenceEquals(card, awaitingMechanicTargetSource);
        }

        bool PointerIsOverCardView()
        {
            if (EventSystem.current == null)
                return false;

            var eventData = new PointerEventData(EventSystem.current)
            {
                position = Input.mousePosition
            };
            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);

            for (int i = 0; i < results.Count; i++)
            {
                if (results[i].gameObject != null &&
                    results[i].gameObject.GetComponentInParent<CardView>() != null)
                {
                    return true;
                }
            }

            return false;
        }

        public void PlayCard(Card card)
        {
            if (battleEnded || card == null || card.data == null || player == null || deckManager == null || turnManager == null)
                return;

            if (turnManager.currentTurn != TurnManager.Turn.Player || turnManager.isExecuting)
                return;

            if (awaitingMechanicTargetSource != null)
            {
                TryResolveMechanicTargetClick(card);
                return;
            }

            int cost = card.effectiveCost;
            if (player.stats.mana < cost)
                return;

            if (TryPrepareCardMechanicSelection(card))
                return;

            player.stats.mana -= cost;
            CollectionProgress.MarkCardUsed(card.data);
            BeginCardResolution();
            resolvingCard = true;

            deckManager.hand.Remove(card);
            UnregisterFrozenCard(card);

            if (!card.effectiveDestroyOnUse)
                deckManager.discard.Add(card);

            int totalDamage = 0;
            currentCardAttemptedDamage = false;

            try
            {
                totalDamage = ResolveCardEffects(card);
            }
            finally
            {
                resolvingCard = false;
            }

            if (statsTracker != null)
                statsTracker.RegisterCardPlayed(totalDamage);

            if (currentCardAttemptedDamage &&
                HasPassive(SubclassPassiveType.DamageCardRamp))
            {
                combatDamageBonus += Mathf.Max(0, ActiveSubclass.amount);
            }

            currentCardAttemptedDamage = false;
            FinishCardResolution(card, cost);
            RenderHand();
            UpdateUI();
            ResolvePendingPostCombatTransition();
            if (deckViewer != null && deckViewer.panel != null && deckViewer.panel.activeSelf)
                deckViewer.ShowDiscard();
        }

        int ResolveCardEffects(Card card, int times = 1)
        {
            if (card == null || card.data == null)
                return 0;

            int repeats = 1 + card.reactivationCount;
            int totalDamage = 0;
            int passes = Mathf.Max(1, times);

            for (int pass = 0; pass < passes; pass++)
            {
                totalDamage += ResolveEffectList(
                    card.data.effects,
                    repeats,
                    true
                );

                CardUpgradeOption upgrade = card.SelectedUpgrade;
                if (upgrade != null)
                    totalDamage += ResolveEffectList(
                        upgrade.bonusEffects,
                        1,
                        false
                    );

                CardEpiphany epiphany = card.Epiphany;
                if (epiphany != null)
                    totalDamage += ResolveEffectList(
                        epiphany.bonusEffects,
                        1,
                        false
                    );
            }

            return totalDamage;
        }

        int ResolveEffectList(
            IEnumerable<CardEffect> effects,
            int repeats,
            bool honorReactivationMultiplier)
        {
            if (effects == null)
                return 0;

            int totalDamage = 0;
            int safeRepeats = Mathf.Max(1, repeats);
            foreach (var effect in effects)
            {
                if (effect == null)
                    continue;

                if (honorReactivationMultiplier &&
                    effect.UsesReactivationMultiplier)
                {
                    lastCardDamageDealt = 0;
                    effect.Apply(this, safeRepeats);
                    totalDamage += lastCardDamageDealt;
                    continue;
                }

                for (int i = 0; i < safeRepeats; i++)
                {
                    lastCardDamageDealt = 0;
                    effect.Apply(this);
                    totalDamage += lastCardDamageDealt;
                }
            }

            return totalDamage;
        }

        int ResolveCardEffectsWithoutCost(Card card, int times = 1)
        {
            if (card == null || card.data == null)
                return 0;

            bool wasResolving = resolvingCard;
            bool attemptedDamageBefore = currentCardAttemptedDamage;
            resolvingCard = true;

            try
            {
                return ResolveCardEffects(card, times);
            }
            finally
            {
                bool attemptedDamageNow = currentCardAttemptedDamage;
                resolvingCard = wasResolving;
                currentCardAttemptedDamage =
                    attemptedDamageBefore || attemptedDamageNow;
            }
        }

        public void DamageEnemy(int damage)
        {
            if (battleEnded || enemy == null) return;

            int firstCardBonus = pendingFirstCardDamageBonus;
            pendingFirstCardDamageBonus = 0;
            int cardDamageBonus = resolvingCard
                ? playerDamageBonus + combatDamageBonus + firstCardBonus
                : 0;
            int totalDamage = Mathf.Max(0, damage + cardDamageBonus);
            if (resolvingCard && totalDamage > 0)
                currentCardAttemptedDamage = true;

            DamageResult result = waveManager.DamageEnemy(totalDamage);
            lastCardDamageDealt += result.healthDamage;

            if (statsTracker != null && result.healthDamage > 0)
                statsTracker.RegisterDamageDealt(result.healthDamage);

            if (relicInventory != null && result.healthDamage > 0)
                relicInventory.OnDamageDealt(result.healthDamage);

            UpdateUI();
        }

        public void RequestPostCombatTransition()
        {
            pendingPostCombatTransition = true;
            if (!resolvingCard)
                ResolvePendingPostCombatTransition();
        }

        void ResolvePendingPostCombatTransition()
        {
            if (!pendingPostCombatTransition || resolvingCard)
                return;

            pendingPostCombatTransition = false;
            waveManager.FlushPostDeathTransition();
        }

        public int GainPlayerArmor(int amount)
        {
            if (battleEnded || player == null || amount <= 0)
                return 0;

            int previous = player.stats.armor;
            player.stats.armor += amount;
            player.stats.Clamp();
            int gained = player.stats.armor - previous;

            if (gained <= 0)
                return 0;

            if (statsTracker != null)
            {
                statsTracker.RegisterArmorGained(gained);
                statsTracker.RegisterMaxArmor(player.stats.armor);
            }

            if (HasArmorToDamagePassive() &&
                enemy != null &&
                enemy.stats.health > 0)
            {
                int damage = Mathf.Max(
                    ActiveSubclass.amount,
                    Mathf.CeilToInt(gained * ActiveSubclass.percentage / 100f)
                );
                if (damage > 0)
                    DamageEnemy(damage);
            }

            UpdateUI();
            return gained;
        }

        public int RestorePlayerMana(int amount)
        {
            if (battleEnded || player == null || amount <= 0)
                return 0;

            int previous = player.stats.mana;
            player.stats.mana = Mathf.Min(player.stats.maxMana, player.stats.mana + amount);
            int restored = player.stats.mana - previous;
            if (restored > 0)
                UpdateUI();
            return restored;
        }

        public int FreezeCardsFromHand(int amount)
        {
            return FreezeCardsFromHand(amount, null);
        }

        public int FreezeCardsFromHand(int amount, Card preferredCard)
        {
            if (deckManager == null || amount <= 0)
                return 0;

            List<Card> candidates = new List<Card>();
            for (int i = 0; i < deckManager.hand.Count; i++)
            {
                Card candidate = deckManager.hand[i];
                if (candidate != null &&
                    candidate.data != null &&
                    !candidate.frozen)
                {
                    candidates.Add(candidate);
                }
            }

            int frozen = 0;
            if (preferredCard != null &&
                candidates.Contains(preferredCard) &&
                FreezeCard(preferredCard))
            {
                candidates.Remove(preferredCard);
                frozen++;
            }

            while (frozen < amount && candidates.Count > 0)
            {
                int index = RunRandom.Range(0, candidates.Count);
                Card card = candidates[index];
                candidates.RemoveAt(index);

                if (FreezeCard(card))
                    frozen++;
            }

            if (frozen <= 0)
                return 0;

            if (HasPassive(SubclassPassiveType.MageIceMastery))
            {
                int armor = Mathf.Max(1, ActiveSubclass.amount) *
                            FrozenCardCount;
                if (armor > 0)
                    GainPlayerArmor(armor);
            }

            deckManager.OnDeckChanged?.Invoke();
            RenderHand();
            UpdateUI();
            return frozen;
        }

        public int ShatterFrozenCards(
            int maxCards,
            int damagePerCard,
            int costReductionPerCard,
            int reactivationPerCard)
        {
            return ShatterFrozenCards(
                maxCards,
                damagePerCard,
                costReductionPerCard,
                reactivationPerCard,
                null
            );
        }

        public int ShatterFrozenCards(
            int maxCards,
            int damagePerCard,
            int costReductionPerCard,
            int reactivationPerCard,
            Card preferredCard)
        {
            if (deckManager == null || FrozenCardCount == 0)
                return 0;

            int count = maxCards <= 0
                ? FrozenCardCount
                : Mathf.Min(maxCards, FrozenCardCount);
            int shattered = 0;

            if (preferredCard != null && ShatterFrozenCard(
                preferredCard,
                costReductionPerCard,
                reactivationPerCard))
            {
                shattered++;
            }

            while (shattered < count)
            {
                Card card = GetFirstFrozenCard();
                if (card == null ||
                    !ShatterFrozenCard(
                        card,
                        costReductionPerCard,
                        reactivationPerCard))
                {
                    break;
                }

                shattered++;
            }

            if (shattered <= 0)
                return 0;

            int damage = Mathf.Max(0, damagePerCard) * shattered;
            if (damage > 0)
                DamageEnemy(damage);

            deckManager.OnDeckChanged?.Invoke();
            RenderHand();
            UpdateUI();
            return shattered;
        }

        public int BurnCardsFromHand(int amount)
        {
            return BurnCardsFromHand(amount, null);
        }

        public int BurnCardsFromHand(int amount, Card preferredCard)
        {
            if (deckManager == null || amount <= 0)
                return 0;

            int burned = 0;
            if (preferredCard != null && BurnCard(preferredCard))
            {
                burned++;
            }

            while (burned < amount && deckManager.hand.Count > 0)
            {
                int index = RunRandom.Range(0, deckManager.hand.Count);
                Card card = deckManager.hand[index];
                if (BurnCard(card))
                    burned++;
                else
                    deckManager.hand.Remove(card);
            }

            if (burned <= 0)
                return 0;

            burnedCardsThisTurn += burned;
            burnedCardsThisCombat += burned;

            if (HasPassive(SubclassPassiveType.MageFireMastery))
                DrawCards(burned);

            deckManager.OnDeckChanged?.Invoke();
            RenderHand();
            UpdateUI();
            return burned;
        }

        bool FreezeCard(Card card)
        {
            if (card == null || card.frozen)
                return false;

            if (deckManager == null || !deckManager.hand.Contains(card))
                return false;

            card.Freeze(FrozenPlayerTurns);
            if (!frozenCards.Contains(card))
                frozenCards.Add(card);

            return true;
        }

        bool BurnCard(Card card)
        {
            if (card == null || deckManager == null)
                return false;

            bool removed = deckManager.hand.Remove(card);
            removed |= frozenCards.Remove(card);
            if (!removed)
                return false;

            card.MarkBurned();
            bool explodes =
                HasPassive(SubclassPassiveType.MageFireMastery) &&
                card.burnCount >= 2;

            card.ClearFreeze();
            if (explodes)
            {
                card.ResetBurnCycle();
                ResolveCardEffectsWithoutCost(card);
            }

            deckManager.discard.Add(card);
            return true;
        }

        void UnregisterFrozenCard(Card card)
        {
            if (card == null)
                return;

            frozenCards.Remove(card);
            card.ClearFreeze();
        }

        public void ReturnFrozenCardsToHand()
        {
            EnsureFrozenCardsInHand();
        }

        public void EnsureFrozenCardsInHand()
        {
            if (deckManager == null)
                return;

            RegisterLooseFrozenHandCards();
            bool changed = false;
            for (int i = frozenCards.Count - 1; i >= 0; i--)
            {
                Card card = frozenCards[i];
                if (card == null || !card.frozen)
                {
                    frozenCards.RemoveAt(i);
                    changed = true;
                    continue;
                }

                if (!deckManager.hand.Contains(card))
                {
                    deckManager.hand.Add(card);
                    changed = true;
                }
            }

            if (changed)
                deckManager.OnDeckChanged?.Invoke();
        }

        public void AdvanceFrozenCardsForPlayerTurnEnd()
        {
            if (deckManager == null)
                return;

            RegisterLooseFrozenHandCards();
            bool changed = false;
            for (int i = frozenCards.Count - 1; i >= 0; i--)
            {
                Card card = frozenCards[i];
                if (card == null || !card.frozen)
                {
                    frozenCards.RemoveAt(i);
                    changed = true;
                    continue;
                }

                card.frozenTurnsRemaining--;
                if (card.frozenTurnsRemaining > 0)
                    continue;

                card.ClearFreeze();
                frozenCards.RemoveAt(i);
                changed = true;
            }

            if (changed)
                deckManager.OnDeckChanged?.Invoke();
        }

        void RegisterLooseFrozenHandCards()
        {
            if (deckManager == null || deckManager.hand == null)
                return;

            for (int i = 0; i < deckManager.hand.Count; i++)
            {
                Card card = deckManager.hand[i];
                if (card == null || !card.frozen || frozenCards.Contains(card))
                    continue;

                if (card.frozenTurnsRemaining <= 0)
                    card.frozenTurnsRemaining = FrozenPlayerTurns;

                frozenCards.Add(card);
            }
        }

        int GetFrozenCardCount()
        {
            int count = frozenCards.Count;
            if (deckManager == null || deckManager.hand == null)
                return count;

            for (int i = 0; i < deckManager.hand.Count; i++)
            {
                Card card = deckManager.hand[i];
                if (card != null && card.frozen && !frozenCards.Contains(card))
                    count++;
            }

            return count;
        }

        Card GetFirstFrozenCard()
        {
            for (int i = 0; i < frozenCards.Count; i++)
            {
                if (frozenCards[i] != null && frozenCards[i].frozen)
                    return frozenCards[i];
            }

            if (deckManager == null || deckManager.hand == null)
                return null;

            for (int i = 0; i < deckManager.hand.Count; i++)
            {
                Card card = deckManager.hand[i];
                if (card != null && card.frozen)
                    return card;
            }

            return null;
        }

        bool ShatterFrozenCard(
            Card card,
            int costReductionPerCard,
            int reactivationPerCard)
        {
            if (card == null || !card.frozen)
                return false;

            bool wasStored = frozenCards.Remove(card);
            card.ClearFreeze();
            card.ReduceCost(Mathf.Max(0, costReductionPerCard));
            card.AddReactivation(Mathf.Max(0, reactivationPerCard));

            if ((wasStored || !deckManager.hand.Contains(card)) &&
                !deckManager.hand.Contains(card))
            {
                deckManager.hand.Add(card);
            }

            if (HasPassive(SubclassPassiveType.MageIceMastery))
                ResolveCardEffectsWithoutCost(card, 2);

            return true;
        }

        public void ApplyEnemyStatus(EnemyStatusType statusType, int amount)
        {
            if (enemy == null || amount <= 0)
                return;

            int effectiveAmount = amount;
            if (statusType == EnemyStatusType.Poison &&
                HasPassive(SubclassPassiveType.PoisonAmplifier))
            {
                effectiveAmount += Mathf.Max(0, ActiveSubclass.amount);
            }

            if (statusType == EnemyStatusType.Bleed &&
                HasPassive(SubclassPassiveType.BleedAmplifier))
            {
                effectiveAmount += Mathf.Max(0, ActiveSubclass.amount);
            }

            int previousStun = statusType == EnemyStatusType.Stun
                ? enemy.GetStatus(EnemyStatusType.Stun)
                : 0;

            enemy.AddStatus(statusType, effectiveAmount);

            if (statusType == EnemyStatusType.Stun &&
                HasPassive(SubclassPassiveType.MageElectricOverload))
            {
                int currentStun = enemy.GetStatus(EnemyStatusType.Stun);
                if (previousStun < StunThreshold &&
                    currentStun >= StunThreshold)
                {
                    int baseDamage = Mathf.Max(1, ActiveSubclass.amount);
                    int bonusPercent =
                        Mathf.Max(0, ActiveSubclass.percentage) * currentStun;
                    int overloadDamage = Mathf.RoundToInt(
                        baseDamage * (100 + bonusPercent) / 100f
                    );
                    DamageEnemy(overloadDamage);
                }
            }

            UpdateUI();
        }

        public void ApplyEnemyTurnStartStatuses()
        {
            if (battleEnded || enemy == null)
                return;

            int poison = enemy.GetStatus(EnemyStatusType.Poison);
            if (poison > 0)
            {
                int poisonDamage = Mathf.Max(
                    1,
                    Mathf.CeilToInt(enemy.stats.maxHealth * 0.05f)
                );
                DamageEnemy(poisonDamage);
            }

            if (battleEnded || enemy == null || enemy.stats.health <= 0)
                return;

            int bleed = enemy.GetStatus(EnemyStatusType.Bleed);
            if (bleed > 0)
                DamageEnemy(bleed);

            DecayEnemyOngoingStatuses();
            UpdateUI();
        }

        void DecayEnemyOngoingStatuses()
        {
            if (enemy == null)
                return;

            if (enemy.GetStatus(EnemyStatusType.Weakness) > 0)
            {
                if (enemy.WasStatusAppliedSinceLastTick(EnemyStatusType.Weakness))
                    enemy.ClearStatusAppliedSinceLastTick(EnemyStatusType.Weakness);
                else
                    enemy.ConsumeStatus(EnemyStatusType.Weakness, 1);
            }

            if (enemy.GetStatus(EnemyStatusType.Bleed) > 0)
            {
                if (enemy.WasStatusAppliedSinceLastTick(EnemyStatusType.Bleed))
                    enemy.ClearStatusAppliedSinceLastTick(EnemyStatusType.Bleed);
                else
                    enemy.SetStatus(EnemyStatusType.Bleed, 0);
            }
        }

        public bool ConsumeEnemyStunForTurn()
        {
            if (enemy == null ||
                enemy.GetStatus(EnemyStatusType.Stun) < StunThreshold)
            {
                return false;
            }

            enemy.ConsumeStatus(EnemyStatusType.Stun, StunThreshold);
            UpdateUI();
            return true;
        }

        public int ModifyEnemyAttackDamage(int damage)
        {
            return Mathf.Max(0, damage);
        }

        public int DamageEnemyWithWeakness(
            int baseDamage,
            int requiredWeakness,
            int criticalPercent,
            bool consumeWeakness)
        {
            if (enemy == null)
                return Mathf.Max(0, baseDamage);

            int damage = Mathf.Max(0, baseDamage);
            int weakness = enemy.GetStatus(EnemyStatusType.Weakness);
            if (weakness > 0)
            {
                int percentPerStack = criticalPercent > 0
                    ? criticalPercent
                    : 2;
                int bonusPercent = weakness * percentPerStack;
                if (HasPassive(SubclassPassiveType.WeaknessCriticalDamage))
                    bonusPercent += Mathf.Max(0, ActiveSubclass.amount);

                damage = Mathf.RoundToInt(damage * (100 + bonusPercent) / 100f);
                if (consumeWeakness)
                    enemy.ConsumeStatus(EnemyStatusType.Weakness, weakness);
            }

            DamageEnemy(damage);
            return damage;
        }

        public int DetonateBleed(float percentageToConsume)
        {
            if (enemy == null)
                return 0;

            int bleed = enemy.GetStatus(EnemyStatusType.Bleed);
            if (bleed <= 0)
                return 0;

            float percentage = percentageToConsume <= 0f
                ? 100f
                : percentageToConsume;
            int consumed = Mathf.Max(
                1,
                Mathf.CeilToInt(
                    bleed * Mathf.Clamp(percentage, 0f, 100f) / 100f
                )
            );
            consumed = enemy.ConsumeStatus(EnemyStatusType.Bleed, consumed);
            DamageEnemy(consumed);
            return consumed;
        }

        public int GetArmorAtPlayerTurnStart(int currentArmor)
        {
            cardsPlayedThisTurn = 0;
            pendingFirstCardDamageBonus = 0;
            burnedCardsThisTurn = 0;

            int armor = HasArmorRetentionPassive()
                ? Mathf.Max(
                    0,
                    Mathf.FloorToInt(
                        currentArmor * ActiveSubclass.percentage / 100f
                    )
                )
                : 0;

            if (HasPassive(SubclassPassiveType.MageIceMastery))
            {
                armor += GetFrozenCardCount() *
                         Mathf.Max(1, ActiveSubclass.amount);
            }

            return armor;
        }

        void BeginCardResolution()
        {
            pendingFirstCardDamageBonus =
                cardsPlayedThisTurn == 0 && HasPassive(SubclassPassiveType.FirstCardBonusDamage)
                    ? Mathf.Max(0, ActiveSubclass.amount)
                    : 0;

            if (relicInventory != null)
            {
                pendingFirstCardDamageBonus +=
                    relicInventory.GetFirstCardDamageBonus(
                        cardsPlayedThisTurn
                    );
            }
        }

        void FinishCardResolution(Card card, int paidCost)
        {
            pendingFirstCardDamageBonus = 0;
            cardsPlayedThisTurn++;

            if (HasPassive(SubclassPassiveType.DrawEveryCards) &&
                cardsPlayedThisTurn % ActiveSubclass.triggerCount == 0)
            {
                DrawCards(Mathf.Max(1, ActiveSubclass.amount));
            }

            if (HasPassive(SubclassPassiveType.RestoreManaEveryCards) &&
                cardsPlayedThisTurn % ActiveSubclass.triggerCount == 0)
            {
                RestorePlayerMana(Mathf.Max(1, ActiveSubclass.amount));
            }

            if (HasPassive(SubclassPassiveType.GoldEveryCards) &&
                cardsPlayedThisTurn % ActiveSubclass.triggerCount == 0 &&
                gameManager != null)
            {
                gameManager.dinero += Mathf.Max(0, ActiveSubclass.amount);
            }

            if (HasPassive(SubclassPassiveType.ArmorEveryCards) &&
                cardsPlayedThisTurn % ActiveSubclass.triggerCount == 0)
            {
                GainPlayerArmor(Mathf.Max(1, ActiveSubclass.amount));
            }

            if (relicInventory != null)
                relicInventory.OnCardResolved(card, paidCost);
        }

        bool HasPassive(SubclassPassiveType passiveType)
        {
            return ActiveSubclass != null && ActiveSubclass.passiveType == passiveType;
        }

        bool HasArmorToDamagePassive()
        {
            return HasPassive(SubclassPassiveType.ArmorToDamage) ||
                   HasPassive(SubclassPassiveType.ArmorToDamageAndRetain);
        }

        bool HasArmorRetentionPassive()
        {
            return HasPassive(SubclassPassiveType.RetainArmor) ||
                   HasPassive(SubclassPassiveType.ArmorToDamageAndRetain);
        }

        public string GetRuntimeCardDescription(CardData cardData)
        {
            if (cardData == null)
                return "";

            string description = cardData.description ?? "";
            if (!ArePlayerDamageBuffsStackable)
                return description;

            return description
                .Replace(" No se acumula.", "")
                .Replace("No se acumula.", "")
                .Trim();
        }

        public void DamagePlayer(int damage)
        {
            if (battleEnded || player == null) return;

            int incomingDamage = relicInventory != null
                ? relicInventory.ModifyIncomingDamage(damage)
                : damage;
            DamageResult result = player.TakeDamage(incomingDamage);

            UpdateUI();

            if (statsTracker != null)
                statsTracker.RegisterDamageReceived(result.healthDamage);

            if (player.stats.health <= 0 && gameManager != null)
            {
                battleEnded = true;
                if (statsTracker != null)
                    statsTracker.PopulateStatsText();
                RecordRun(false);
                gameManager.ShowDefeat();
            }
        }

        public void ContinueAfterShop()
        {
            if (battleEnded || deckManager == null || turnManager == null)
                return;

            waveManager.SpawnNext();
            if (relicInventory != null)
                relicInventory.OnCombatStarted();

            deckManager.deck.AddRange(deckManager.hand);
            deckManager.hand.Clear();
            deckManager.Shuffle(deckManager.deck);

            turnManager.NextRound();
            turnManager.StartPlayerTurn();
        }

        public void ApplyPlayerDamageBonus(int amount, int turns)
        {
            int effectiveTurns = relicInventory != null
                ? relicInventory.ModifyBuffDuration(turns)
                : turns;

            if (HasPassive(SubclassPassiveType.StackDamageBuffs))
            {
                playerDamageBonus += amount;
                playerDamageBonusTurnsRemaining =
                    Mathf.Max(0, effectiveTurns);
                return;
            }

            playerDamageBonus = amount;
            playerDamageBonusTurnsRemaining = Mathf.Max(0, effectiveTurns);
        }

        public int HealPlayer(int amount)
        {
            if (player == null || amount <= 0)
                return 0;

            int previous = player.stats.health;
            player.stats.health = Mathf.Min(
                player.stats.maxHealth,
                player.stats.health + amount
            );
            player.stats.Clamp();
            int healed = player.stats.health - previous;
            if (healed > 0)
                UpdateUI();
            return healed;
        }

        public void AdvancePlayerDamageBonusTurn()
        {
            if (playerDamageBonusTurnsRemaining <= 0)
                return;

            playerDamageBonusTurnsRemaining--;

            if (playerDamageBonusTurnsRemaining <= 0)
            {
                playerDamageBonusTurnsRemaining = 0;
                playerDamageBonus = 0;
            }
        }

        public void ResetTemporaryCombatEffects()
        {
            preparedMechanicTargets.Clear();
            awaitingMechanicTargetSource = null;
            awaitingMechanicTargetEffect = null;
            MoveFrozenCardsToDiscard();
            playerDamageBonus = 0;
            playerDamageBonusTurnsRemaining = 0;
            combatDamageBonus = 0;
            burnedCardsThisCombat = 0;
            burnedCardsThisTurn = 0;
        }

        void MoveFrozenCardsToDiscard()
        {
            if (deckManager == null)
            {
                frozenCards.Clear();
                return;
            }

            RegisterLooseFrozenHandCards();
            bool changed = false;
            for (int i = frozenCards.Count - 1; i >= 0; i--)
            {
                Card card = frozenCards[i];
                frozenCards.RemoveAt(i);
                if (card == null)
                    continue;

                deckManager.hand.Remove(card);
                card.ClearFreeze();
                if (!deckManager.discard.Contains(card))
                {
                    deckManager.discard.Add(card);
                    changed = true;
                }
            }

            for (int i = deckManager.hand.Count - 1; i >= 0; i--)
            {
                Card card = deckManager.hand[i];
                if (card == null || !card.frozen)
                    continue;

                deckManager.hand.RemoveAt(i);
                card.ClearFreeze();
                if (!deckManager.discard.Contains(card))
                    deckManager.discard.Add(card);
                changed = true;
            }

            if (changed)
                deckManager.OnDeckChanged?.Invoke();
        }

        void ReduceRandomHandCardCosts(int count)
        {
            if (deckManager == null || deckManager.hand.Count == 0 || count <= 0)
                return;

            List<Card> candidates = new List<Card>(deckManager.hand);
            candidates.RemoveAll(card => card == null || card.effectiveCost <= 0);

            int applied = 0;
            while (applied < count && candidates.Count > 0)
            {
                int index = RunRandom.Range(0, candidates.Count);
                Card card = candidates[index];
                candidates.RemoveAt(index);
                card.ReduceCost();
                applied++;
            }

            if (applied > 0)
            {
                deckManager.OnDeckChanged?.Invoke();
                RenderHand();
            }
        }

        public void RenderHand()
        {
            if (handRenderer != null && deckManager != null)
                handRenderer.Render(deckManager.hand, this);
        }

        public void UpdateUI()
        {
            if (uiManager != null)
                uiManager.Refresh();
            if (turnManager != null)
                turnManager.RefreshEnemyIntentPreview();
        }

        void RecordRun(bool completed)
        {
            if (runRecorded)
                return;

            runRecorded = true;
            CollectionProgress.RegisterRunFinished(
                CharacterRunState.SelectedCharacter,
                statsTracker,
                completed
            );
        }
    }
}
