using UnityEngine;
using UnityEngine.EventSystems;
using JuegoDeCartas.Characters;
using JuegoDeCartas.Challenges;
using JuegoDeCartas.Managers;

namespace JuegoDeCartas.UI
{
    public enum PlayerFrameTooltipMode
    {
        ClassOrSubclass,
        Interest
    }

    public class PlayerFrameHoverTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public PlayerFrameTooltipMode mode;
        public CombatTooltipUI tooltip;
        public BattleManager battle;

        [Header("Interest Text")]
        public string interestTitle = "Interes actual";
        [TextArea(2, 5)]
        public string interestFormat =
            "Con {0} de oro recibiras +{1} al entrar en la siguiente tienda.\n" +
            "Cada {2} de oro: +{3}. Maximo: +{4}.";
        [TextArea(2, 4)]
        public string disabledInterestDescription =
            "Los intereses estan desactivados en este reto.";

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (tooltip == null)
                return;

            if (mode == PlayerFrameTooltipMode.Interest)
            {
                tooltip.Show(this, interestTitle, BuildInterestDescription(
                    battle,
                    interestFormat,
                    disabledInterestDescription
                ));
                return;
            }

            ResolveIdentityTooltip(
                CharacterRunState.SelectedCharacter,
                CharacterRunState.SelectedSubclass,
                out string title,
                out string description
            );
            tooltip.Show(this, title, description);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (tooltip != null)
                tooltip.Hide(this);
        }

        public static void ResolveIdentityTooltip(
            CharacterData character,
            SubclassData subclass,
            out string title,
            out string description)
        {
            if (subclass != null)
            {
                title = subclass.subclassName;
                description = string.IsNullOrWhiteSpace(subclass.passiveDescription)
                    ? subclass.description
                    : subclass.passiveDescription;
                return;
            }

            title = character != null ? character.characterName : "";
            description = character == null
                ? ""
                : string.IsNullOrWhiteSpace(character.mechanicDescription)
                    ? character.description
                    : character.mechanicDescription;
        }

        public static string BuildInterestDescription(
            BattleManager battle,
            string format,
            string disabledDescription)
        {
            if (ChallengeRunState.IsShopDisabled)
                return disabledDescription;

            ShopManager shop = battle != null &&
                               battle.gameManager != null
                ? battle.gameManager.shopManager
                : null;
            int gold = battle != null &&
                       battle.gameManager != null
                ? battle.gameManager.dinero
                : 0;
            if (shop == null)
                return string.Format(format, gold, 0, 0, 0, 0);

            return string.Format(
                format,
                gold,
                shop.CalculateInterest(gold),
                shop.goldPerInterestStep,
                shop.interestPerStep,
                shop.maxInterest
            );
        }
    }
}
