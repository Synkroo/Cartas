using TMPro;
using UnityEngine;
using JuegoDeCartas.Managers;
using System.Collections.Generic;

namespace JuegoDeCartas.UI
{
    public class CombatInfoPanelUI : MonoBehaviour
    {
        [Header("Player Statuses")]
        public Transform playerStatusContainer;
        public CombatStatusIconUI playerStatusTemplate;
        public Sprite playerDamageStatusSprite;
        public Sprite playerArmorStatusSprite;
        public Sprite playerRegenStatusSprite;
        public Sprite playerSubclassStatusSprite;

        [Header("Enemy Current Armor")]
        public TextMeshProUGUI enemyArmorText;
        public GameObject enemyArmorLabelObject;
        public GameObject enemyArmorValueObject;

        [Header("Enemy Statuses")]
        public Transform enemyStatusContainer;
        public CombatStatusIconUI enemyStatusTemplate;
        public Sprite enemyDamageStatusSprite;
        public Sprite enemyArmorStatusSprite;
        public Sprite enemyRegenStatusSprite;
        public Sprite enemyReviveStatusSprite;

        BattleManager battle;
        readonly List<CombatStatusIconUI> playerStatusInstances = new List<CombatStatusIconUI>();
        readonly List<CombatStatusIconUI> enemyStatusInstances = new List<CombatStatusIconUI>();
        bool legacyObjectsHidden;

        public void Init(BattleManager battleManager)
        {
            battle = battleManager;
            ResolveReferences();
            Refresh();
        }

        public void Refresh()
        {
            if (battle == null)
                return;

            ResolveReferences();
            RefreshPlayerInfo();
            RefreshEnemyInfo();
        }

        void RefreshPlayerInfo()
        {
            int damageBonus = Mathf.Max(0, battle.playerDamageBonus);
            int damageTurns = Mathf.Max(0, battle.playerDamageBonusTurnsRemaining);
            int armorPerTurn = Mathf.Max(0, battle.armorPerTurn);
            int regenPerRound = Mathf.Max(0, battle.regenPerRound);

            int statusCount = 0;

            if (battle.ActiveSubclass != null)
            {
                Sprite subclassSprite = battle.ActiveSubclass.icon != null
                    ? battle.ActiveSubclass.icon
                    : playerSubclassStatusSprite;
                SetPlayerStatus(
                    statusCount++,
                    subclassSprite,
                    "P",
                    battle.ActiveSubclass.subclassName,
                    battle.ActiveSubclass.passiveDescription
                );
            }

            if (damageBonus > 0 && damageTurns > 0)
            {
                SetPlayerStatus(
                    statusCount++,
                    playerDamageStatusSprite,
                    damageTurns.ToString(),
                    "Aumento de dano",
                    BuildPlayerDamageBuffDescription(damageBonus, damageTurns)
                );
            }

            if (armorPerTurn > 0)
            {
                SetPlayerStatus(
                    statusCount++,
                    playerArmorStatusSprite,
                    "+" + armorPerTurn,
                    "Armadura por turno",
                    "Ganas " + armorPerTurn + " de armadura al empezar tu turno."
                );
            }

            if (regenPerRound > 0)
            {
                SetPlayerStatus(
                    statusCount++,
                    playerRegenStatusSprite,
                    "+" + regenPerRound,
                    "Regeneracion",
                    "Te curas " + regenPerRound + " al avanzar de ronda."
                );
            }

            HideUnusedPlayerStatuses(statusCount);
        }

        void RefreshEnemyInfo()
        {
            var enemy = battle.enemy;
            if (enemy == null)
            {
                SetEnemyArmorVisible(false);
                HideUnusedEnemyStatuses(0);
                return;
            }

            int statusCount = 0;
            int currentArmor = Mathf.Max(0, enemy.stats.armor);

            SetEnemyArmorVisible(false);
            if (currentArmor > 0)
            {
                SetEnemyStatus(
                    statusCount++,
                    enemyArmorStatusSprite,
                    currentArmor.ToString(),
                    "Armadura actual",
                    "Bloquea los proximos " + currentArmor + " puntos de dano."
                );
            }

            if (enemy.data != null && enemy.data.mechanics != null)
            {
                for (int i = 0; i < enemy.data.mechanics.Count; i++)
                {
                    var mechanic = enemy.data.mechanics[i];
                    if (mechanic == null)
                        continue;

                    if (mechanic.mechanicType == Enemies.EnemyMechanicType.StatsPerTurn)
                    {
                        if (mechanic.damageRampPerTurn > 0)
                        {
                            int currentBonus = enemy.GetMechanicCurrentDamageAccumulation(i);
                            SetEnemyStatus(
                                statusCount++,
                                enemyDamageStatusSprite,
                                currentBonus.ToString(),
                                "Aumento de dano infinito",
                                BuildEnemyDamageRampDescription(mechanic.damageRampPerTurn, currentBonus)
                            );
                        }

                        if (mechanic.healPerTurn > 0)
                        {
                            SetEnemyStatus(
                                statusCount++,
                                enemyRegenStatusSprite,
                                "+" + mechanic.healPerTurn,
                                "Regeneracion infinita",
                                BuildEnemyHealDescription(mechanic.healPerTurn)
                            );
                        }

                        if (mechanic.armorPerTurn > 0)
                        {
                            SetEnemyStatus(
                                statusCount++,
                                enemyArmorStatusSprite,
                                "+" + mechanic.armorPerTurn,
                                "Armadura por turno",
                                BuildEnemyArmorDescription(mechanic.armorPerTurn)
                            );
                        }

                        continue;
                    }

                    if (mechanic.mechanicType == Enemies.EnemyMechanicType.ReviveOnDeath)
                    {
                        int remainingRevives = enemy.GetRemainingRevivesForMechanic(i);
                        if (remainingRevives > 0)
                        {
                            SetEnemyStatus(
                                statusCount++,
                                enemyReviveStatusSprite,
                                remainingRevives.ToString(),
                                "Renacer",
                                BuildEnemyReviveDescription(mechanic, remainingRevives)
                            );
                        }
                    }
                }
            }

            HideUnusedEnemyStatuses(statusCount);
        }

        static void SetText(TextMeshProUGUI text, string value)
        {
            if (text != null)
                text.text = value;
        }

        void SetPlayerStatus(int index, Sprite sprite, string badgeValue, string title, string description)
        {
            CombatStatusIconUI status = GetOrCreateStatus(index, playerStatusContainer, playerStatusTemplate, playerStatusInstances);
            if (status != null)
                status.SetStatus(true, sprite, badgeValue, title, description);
        }

        void SetEnemyStatus(int index, Sprite sprite, string badgeValue, string title, string description)
        {
            CombatStatusIconUI status = GetOrCreateStatus(index, enemyStatusContainer, enemyStatusTemplate, enemyStatusInstances);
            if (status != null)
                status.SetStatus(true, sprite, badgeValue, title, description);
        }

        CombatStatusIconUI GetOrCreateStatus(int index, Transform container, CombatStatusIconUI template, List<CombatStatusIconUI> instances)
        {
            if (template == null || container == null)
                return null;

            while (instances.Count <= index)
            {
                CombatStatusIconUI instance = Instantiate(template, container);
                instance.gameObject.SetActive(true);
                instances.Add(instance);
            }

            return instances[index];
        }

        void HideUnusedPlayerStatuses(int usedCount)
        {
            for (int i = usedCount; i < playerStatusInstances.Count; i++)
            {
                if (playerStatusInstances[i] != null)
                    playerStatusInstances[i].SetStatus(false, string.Empty, string.Empty, string.Empty);
            }

            if (playerStatusTemplate != null)
                playerStatusTemplate.gameObject.SetActive(false);
        }

        void HideUnusedEnemyStatuses(int usedCount)
        {
            for (int i = usedCount; i < enemyStatusInstances.Count; i++)
            {
                if (enemyStatusInstances[i] != null)
                    enemyStatusInstances[i].SetStatus(false, string.Empty, string.Empty, string.Empty);
            }

            if (enemyStatusTemplate != null)
                enemyStatusTemplate.gameObject.SetActive(false);
        }

        void ResolveReferences()
        {
            if (playerStatusContainer == null)
            {
                Transform found = transform.Find("Jugador/PlayerStatuses");
                if (found != null)
                    playerStatusContainer = found;
            }

            if (playerStatusTemplate == null && playerStatusContainer != null)
                playerStatusTemplate = playerStatusContainer.GetComponentInChildren<CombatStatusIconUI>(true);

            if (enemyStatusContainer == null)
            {
                Transform found = transform.Find("Enemigo/EnemyStatuses");
                if (found != null)
                    enemyStatusContainer = found;
            }

            if (enemyStatusTemplate == null && enemyStatusContainer != null)
                enemyStatusTemplate = enemyStatusContainer.GetComponentInChildren<CombatStatusIconUI>(true);

            if (enemyArmorText == null)
            {
                Transform value = transform.Find("Enemigo/ArmaduraValor");
                if (value != null)
                    enemyArmorText = value.GetComponent<TextMeshProUGUI>();
            }

            if (enemyArmorLabelObject == null)
            {
                Transform label = transform.Find("Enemigo/ArmaduraLabel");
                if (label != null)
                    enemyArmorLabelObject = label.gameObject;
            }

            if (enemyArmorValueObject == null && enemyArmorText != null)
                enemyArmorValueObject = enemyArmorText.gameObject;

            if (!legacyObjectsHidden)
            {
                HideLegacyPlayerObjects();
                HideLegacyEnemyObjects();
                legacyObjectsHidden = true;
            }
        }

        void HideLegacyPlayerObjects()
        {
            HideChild("Jugador/ArmaduraLabel");
            HideChild("Jugador/ArmaduraValor");
            HideChild("Jugador/ProximaArmaduraLabel");
            HideChild("Jugador/ProximaArmaduraValor");
            HideChild("Jugador/ProximaCuracionLabel");
            HideChild("Jugador/ProximaCuracionValor");
        }

        void HideLegacyEnemyObjects()
        {
            HideChild("Enemigo/ProximaArmaduraLabel");
            HideChild("Enemigo/ProximaArmaduraValor");
            HideChild("Enemigo/ProximaCuracionLabel");
            HideChild("Enemigo/ProximaCuracionValor");
            HideChild("Enemigo/Icono Armadura Enemigo");
            HideChild("Enemigo/Icono Curacion Enemigo");
            HideChild("Enemigo/Icono Dano Enemigo");
        }

        void HideChild(string relativePath)
        {
            Transform child = transform.Find(relativePath);
            if (child != null)
                child.gameObject.SetActive(false);
        }

        void SetEnemyArmorVisible(bool visible)
        {
            if (enemyArmorLabelObject != null)
                enemyArmorLabelObject.SetActive(visible);

            if (enemyArmorValueObject != null)
                enemyArmorValueObject.SetActive(visible);
        }

        static string BuildEnemyDamageRampDescription(int damagePerTurn, int currentBonus)
        {
            return "Escala " + damagePerTurn + " en dano min/max cada turno. Actualmente: +" + currentBonus + ".";
        }

        string BuildPlayerDamageBuffDescription(int damageBonus, int damageTurns)
        {
            string description =
                "Rugido: +" + damageBonus + ". Quedan " + damageTurns + " turnos.";

            if (battle.ArePlayerDamageBuffsStackable)
                description += " Los aumentos se acumulan y reinician la duracion.";

            return description;
        }

        static string BuildEnemyHealDescription(int healPerTurn)
        {
            return "Se cura " + healPerTurn + " de vida cada turno. Actualmente: +" + healPerTurn + " por turno.";
        }

        static string BuildEnemyArmorDescription(int armorPerTurn)
        {
            return "Gana " + armorPerTurn + " de armadura cada turno. Actualmente: +" + armorPerTurn + " por turno.";
        }

        static string BuildEnemyReviveDescription(Enemies.EnemyMechanicData mechanic, int remainingRevives)
        {
            return "Puede revivir " + remainingRevives + " vez/veces. Al revivir pasa a " +
                   mechanic.maxHealthPercentOnRevive + "% de vida maxima y " +
                   mechanic.minDamagePercentOnRevive + "%/" + mechanic.maxDamagePercentOnRevive +
                   "% de dano min/max.";
        }
    }
}
