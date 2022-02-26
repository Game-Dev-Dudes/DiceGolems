using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TileAttackOverride : TileOverride {
    public void Execute(CombatCharacter defensiveCharacter, CombatCharacter offensiveCharacter, List<Die> dice, Tile tile) {
        //generate damage
        int damage = DieUtility.SumDice(dice);

        damage = (int)ActionFilterUtility.ApplyFiltersOfType(damage, offensiveCharacter.ActionFilters, FilterType.AttackActor);
        damage = (int)ActionFilterUtility.ApplyFiltersOfType(damage, defensiveCharacter.ActionFilters, FilterType.AttackRecipient);

        //Execute the action
        defensiveCharacter.TakeDamage(damage);
    }
}