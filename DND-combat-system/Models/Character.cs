namespace RpgBattle;

using System.Text.Json.Serialization;

abstract class Character
{
    public string Name { get; }
    public int MaxHp { get; }
    [JsonIgnore] public int CurrentHp { get; protected set; }
    public int ArmorClass { get; }

    [JsonIgnore]
    public int EffectiveArmorClass
    {
        get
        {
            if (HasEffect(StatusEffectType.Weakened))
                return ArmorClass - 2;
            return ArmorClass;
        }
    }
    public List<Attack> Attacks { get; }
    public List<Resistance> Resistances { get; }
    [JsonIgnore] public LinkedList<StatusEffect> StatusEffects { get; protected set; }
    [JsonIgnore] public int Initiative { get; protected set; }
    [JsonIgnore] public int EffectiveInitiative
    {
        get
        {
            int einitiative = Initiative;
            if (HasEffect(StatusEffectType.Slowed))
                einitiative -= 10;
            else if (HasEffect(StatusEffectType.Haste))
                einitiative += 10;
            return einitiative;
        }
    }
    public int InitiativeModifier { get; }
    [JsonIgnore] public bool IsDown => CurrentHp <= 0;
    [JsonIgnore] public virtual bool IsAlive => CurrentHp > 0;

    [JsonIgnore]
    public bool IsIncapacitated
    {
        get
        {
            if (HasEffect(StatusEffectType.Stunned))
                return true;
            return false;
        }
    }


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
    public int TakeDamage(int amount, DamageType type, int chance)
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
        if (finalDamage > 0 || type == DamageType.Haste || type == DamageType.Slow)
        {
            if (chance == 0)
                chance = Random.Shared.Next(1, 101);
            switch (type)
            {
                case DamageType.Acid:
                    if (chance <= 15 || chance == 100) AddStatus(StatusEffectType.Weakened);
                    if (chance <= 20 || chance == 100) AddStatus(StatusEffectType.Poisoned);
                    break;
                case DamageType.Necrotic:
                    if (chance <= 25 || chance == 100) AddStatus(StatusEffectType.Weakened);
                    break;
                case DamageType.Poison:
                    if (chance <= 20 || chance == 100) AddStatus(StatusEffectType.Poisoned);
                    break;
                case DamageType.Piercing:
                    if (chance <= 20 || chance == 100) AddStatus(StatusEffectType.Bleeding);
                    break;
                case DamageType.Slashing:
                    if (chance <= 10 || chance == 100) AddStatus(StatusEffectType.Bleeding);
                    break;
                case DamageType.Bludgeoning:
                    if (chance <= 30 || chance == 100) AddStatus(StatusEffectType.Stunned);
                    break;
                case DamageType.Lightning:
                    if (chance <= 40 || chance == 100) AddStatus(StatusEffectType.Stunned);
                    break;
                case DamageType.Slow:
                    AddStatus(StatusEffectType.Slowed);
                    break;
                case DamageType.Haste:
                    AddStatus(StatusEffectType.Haste);
                    break;
            }
        }

        CurrentHp = Math.Max(0, CurrentHp - finalDamage);
        return finalDamage;
    }
    public void TakeDamage(int amount)
    {
        CurrentHp = Math.Max(0, CurrentHp - amount);
    }
    public void Heal(int amount)
    {
        if (amount < 0) return;
        CurrentHp = Math.Min(MaxHp, CurrentHp + amount);
    }
    public int RollToHit(Attack attack)
    {
        int roll = Random.Shared.Next(1, 21);

        if (HasEffect(StatusEffectType.Poisoned))
        {
            int secondRoll = Random.Shared.Next(1, 21);
            roll = Math.Min(roll, secondRoll);
        }
        return roll + attack.BonusToHit;
    }
    public int RollDamage(Attack attack)
    {
        int damage = 0;
        for (int i = 0; i < attack.Damage.Count; i++)
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
    public bool HasEffect(StatusEffectType effect)
    {
        foreach (StatusEffect e in StatusEffects)
        {
            if (e.Type == effect)
            {
                return true;
            }
        }
        return false;
    }
    public void AddStatus(StatusEffectType type)
    {
        int turns = 0;
        switch (type)
        {
            case StatusEffectType.Stunned:
                turns = Random.Shared.Next(1, 3); break;
            case StatusEffectType.Bleeding:
                turns = Random.Shared.Next(2, 5); break;
            case StatusEffectType.Poisoned:
                turns = Random.Shared.Next(2, 5); break;
            case StatusEffectType.Weakened:
                turns = Random.Shared.Next(2, 5); break;
            case StatusEffectType.Slowed:
                turns = 3; break;
            case StatusEffectType.Haste:
                turns = 3; break;
            default:
                turns = 0; break;
        }

        if (turns == 0) return;

        if (HasEffect(type))
        {
            foreach (StatusEffect e in StatusEffects)
            {
                if (e.Type == type)
                {
                    e.Duration += turns;
                }
            }
            return;
        }

        StatusEffects.AddLast(new StatusEffect(type, turns));
    }
    public void ManageStatusEffects()
    {
        if (IsDown)
        {
            StatusEffects.Clear();
            return;
        }

        LinkedListNode<StatusEffect>? node = StatusEffects.First;

        while (node != null)
        {
            LinkedListNode<StatusEffect>? next = node.Next;
            StatusEffect effect = node.Value;

            int damage = 0;
            switch (effect.Type)
            {
                case StatusEffectType.Stunned:
                    Console.WriteLine($"{Name} is stunned");
                    break;
                case StatusEffectType.Bleeding:
                    damage = Random.Shared.Next(1, 7);   // 1d6
                    TakeDamage(damage);
                    Console.WriteLine($"{Name} takes {damage} Bleeding damage ");
                    break;
            }

            effect.Duration--;
            if (effect.Duration <= 0)
            {
                StatusEffects.Remove(node);
            }

            node = next;
        }

    }
}
