public record DiceRoll(int Count, int Sides, string damageType);
public record Attack(string Name, int AttackBonus, DiceRoll damage);
class Enemy
{
    public int ID { get; }
    public string Name { get; }
    public int MaxHp { get; }
    public int CurrentHp { get; private set; }
    public int ArmorClass { get; }
    IReadOnlyList<Attack> Attacks { get; }
    public bool IsAlive => CurrentHp > 0;

    public Enemy(int id, string name, int maxHp, int currentHp, int armorClass, IReadOnlyList<Attack> attacks)
    {
        ID = id;
        Name = name;
        MaxHp = maxHp;
        CurrentHp = currentHp;
        ArmorClass = armorClass;
        Attacks = attacks;
    }
    public void TakeDamage(int amount)
    {
        CurrentHp = Math.Max(0, CurrentHp - amount);
    }
    public void HealHp(int amount)
    {
        CurrentHp = Math.Min(MaxHp, CurrentHp + amount);
    }
}

