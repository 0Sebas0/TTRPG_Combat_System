using System.Text.Json.Serialization;

namespace RpgBattle;

public record DiceRoll(int Count, int Sides);

public record Attack(string Name, int BonusToHit, int DamageBonus, DamageType DamageType, DiceRoll Damage)
{

    [JsonIgnore] public bool IsHeal => DamageType == DamageType.Heal;
    [JsonIgnore] public bool IsSpeedSpell => DamageType == DamageType.Haste || DamageType == DamageType.Slow;
    [JsonIgnore] public bool IsDamaging => !IsHeal && !IsSpeedSpell;

    // Average of the dice (e.g. 2d6 = 2 * 3.5 = 7) plus the flat bonus
    [JsonIgnore] public double AverageDamage => Damage.Count * (Damage.Sides + 1) / 2.0 + DamageBonus;
}

public record Resistance(DamageType DamageType, ResistanceLevel ResistanceLevel);

// What PerformAttack reports back, so the attacker can learn from it
public record AttackResult(bool Hit, int RawDamage, int DamageDealt);

// What the enemy AI decided to do. 'internal' because Character is internal too.
internal record EnemyAction(Attack Attack, Character Target, string Reason);