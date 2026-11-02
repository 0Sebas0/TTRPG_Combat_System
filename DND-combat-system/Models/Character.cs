namespace RpgBattle;

using System.Text.Json.Serialization;

abstract class Character
{
    public string Name { get; }
    public int MaxHp { get; }
    [JsonIgnore] public int CurrentHp { get; protected set; }
    public int ArmorClass { get; }
    public List<Attack> Attacks { get; }
    public List<Resistance> Resistances { get; }
    [JsonIgnore] public LinkedList<StatusEffect> StatusEffects { get; protected set; }
    [JsonIgnore] public int Initiative { get; protected set; }
    public int InitiativeModifier { get; }
    [JsonIgnore] public bool IsDown => CurrentHp <= 0;
    [JsonIgnore] public virtual bool IsAlive => CurrentHp > 0;


    [JsonConstructor]
    public Character(string name, int maxHp, int armorClass, int initiativeModifier, List<Attack> attacks, List<Resistance> resistances)
    {
        Name = name;
        MaxHp = maxHp;
        CurrentHp = maxHp;
        ArmorClass = armorClass;
        Attacks = attacks;
        Resistances = resistances;
        Initiative = 0;
        InitiativeModifier = initiativeModifier;
        StatusEffects = new LinkedList<StatusEffect>();
    }

    public abstract Character Clone();
    public int TakeDamage(int amount, DamageType type)
    {
        if (amount < 0) return 0;
        int finalDamage = amount;

        foreach (Resistance resistance in Resistances)
        {
            if (resistance.DamageType == type)
            {
                switch (resistance.ResistanceLevel)
                {
                    case ResistanceLevel.Vulnerability:
                        finalDamage *= 2;
                        break;

                    case ResistanceLevel.Immunity:
                        finalDamage = 0;
                        break;

                    case ResistanceLevel.Resistance:
                        finalDamage /= 2;
                        break;
                }
                break;
            }
        }

        CurrentHp = Math.Max(0, CurrentHp - finalDamage);
        return finalDamage;
    }
    public void Heal(int amount)
    {
        if (amount < 0) return;
        CurrentHp = Math.Min(MaxHp, CurrentHp + amount);
    }
    public int RollToHit(Attack attack)
    {
        return Random.Shared.Next(1, 21) + attack.BonusToHit;
    }
    public int RollDamage(Attack attack)
    {
        int damage = 0;
        for(int i = 0; i< attack.Damage.Count;i++)
        {
            damage += Random.Shared.Next(1, attack.Damage.Sides + 1);
        }
        damage += attack.DamageBonus;
        return damage;
    }
    public void RollForInitiative()
    {
        Initiative = Random.Shared.Next(1, 21) + InitiativeModifier;
    }
}