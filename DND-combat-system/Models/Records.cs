namespace RpgBattle;
public record DiceRoll(int Count, int Sides);
public record Attack(string Name, int BonusToHit, int DamageBonus, DamageType DamageType, DiceRoll Damage);
public record Resistance(DamageType DamageType, ResistanceLevel ResistanceLevel);
