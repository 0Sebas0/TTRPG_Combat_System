namespace RpgBattle;
using System.Text.Json.Serialization;

class Character
{
    public string Name { get; }
    public int MaxHp { get; }
    [JsonIgnore] public int CurrentHp { get; private set; }
    public int ArmorClass { get; }
    public List<Attack> Attacks { get; }
    public List<Resistance> Resistances { get; }
    [JsonIgnore] public LinkedList<StatusEffect> StatusEffects { get; private set; }
    [JsonIgnore] public int Initiative { get; private set; }
    public int InitiativeModifier { get; }
    [JsonIgnore] public bool IsAlive => CurrentHp > 0;

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
    public Character Clone()
    {
        return new Character(Name, MaxHp, ArmorClass, InitiativeModifier, new List<Attack>(Attacks), new List<Resistance>(Resistances));
    }
    public void TakeDamage(int amount, DamageType type)
    {
        if (amount < 0) return;
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
    }
    public void Heal(int amount)
    {
        if (amount < 0) return;
        CurrentHp = Math.Min(MaxHp, CurrentHp + amount);
    }
    public int RollForAttack(Attack attack)
    {
        return Random.Shared.Next(1, 21) + attack.BonusToHit;
    }
    public void RollForInitiative()
    {
        Initiative = Random.Shared.Next(1, 21) + InitiativeModifier;
    }
}