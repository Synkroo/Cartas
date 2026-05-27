using System;
using System.Collections.Generic;
using JuegoDeCartas.Cards;
using UnityEngine;

namespace JuegoDeCartas.Enemies
{
    public enum EnemyTier
    {
        Normal = 0,
        MiniBoss = 1,
        Boss = 2
    }

    public enum EnemyMechanicType
    {
        None = 0,
        StatsPerTurn = 1,
        AddJunkToDeckOnTurnStart = 2,
        ReviveOnDeath = 3
    }

    public enum EnemyCardDestination
    {
        DrawPile = 0,
        DiscardPile = 1
    }

    [Serializable]
    public class EnemyMechanicData
    {
        public string mechanicName;
        public EnemyMechanicType mechanicType = EnemyMechanicType.None;

        [Header("Stats Per Turn")]
        [HideInInspector]
        public int armorPerTurn;
        [HideInInspector]
        public int healPerTurn;
        [HideInInspector]
        public int damageRampPerTurn;

        [Header("Add Junk To Deck")]
        [HideInInspector]
        public CardData junkCard;
        [HideInInspector]
        public int cardsToAdd = 1;
        [HideInInspector]
        public EnemyCardDestination cardDestination = EnemyCardDestination.DrawPile;
        [HideInInspector]
        public bool shuffleIntoDrawPile = true;
        [HideInInspector]
        public int firstTriggerTurn = 1;
        [HideInInspector]
        public int triggerEveryXTurns = 1;

        [Header("Revive")]
        [HideInInspector]
        public int reviveCount = 1;
        [HideInInspector]
        public float maxHealthPercentOnRevive = 80f;
        [HideInInspector]
        public float armorPercentOnRevive = 0f;
        [HideInInspector]
        public float minDamagePercentOnRevive = 120f;
        [HideInInspector]
        public float maxDamagePercentOnRevive = 120f;
    }

    [CreateAssetMenu(fileName = "EnemyData", menuName = "Game/Enemy")]
    public class EnemyData : ScriptableObject
    {
        public string enemyName;

        [Header("Stats")]
        public int maxHealth = 30;

        [Header("Damage Range")]
        public int minDamage = 5;
        public int maxDamage = 10;

        [Header("Tier")]
        public EnemyTier enemyTier = EnemyTier.Normal;

        [Header("Combat")]
        public int startArmor;
        public int goldRewardOverride;
        [HideInInspector]
        public List<EnemyMechanicData> mechanics = new List<EnemyMechanicData>();

        [Header("Visual")]
        public Sprite sprite;
        public RuntimeAnimatorController animatorController;

        public bool IsBoss => enemyTier == EnemyTier.Boss;
        public bool IsMiniBoss => enemyTier == EnemyTier.MiniBoss;
    }
}
