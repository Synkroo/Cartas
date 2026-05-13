using UnityEngine;

[CreateAssetMenu(menuName = "Cards/Effects/Draw Cards")]
public class DrawEffect : CardEffect
{
    public int amount;

    public override void Apply(BattleManager battle)
    {
        battle.DrawCards(amount);
    }
}