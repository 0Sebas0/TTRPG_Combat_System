using System.Text.Json.Serialization;

namespace RpgBattle;

class Player : Character
{
    [JsonIgnore] public int DeathSaveSuccesses { get; private set; }
    [JsonIgnore] public int DeathSaveFailures { get; private set; }
    [JsonIgnore] public bool IsDead => DeathSaveFailures >= 3;
    [JsonIgnore]public override bool IsAlive => !IsDead;

    [JsonConstructor]
    public Player(string name, int maxHp, int armorClass, int initiativeModifier, List<Attack> attacks, List<Resistance> resistances)
        : base(name, maxHp, armorClass, initiativeModifier, attacks, resistances) { }

    public override Player Clone()
    {
        return new Player(Name, MaxHp, ArmorClass, InitiativeModifier, new List<Attack>(Attacks), new List<Resistance>(Resistances));
    }
    public void RollDeathSave()
    {
        int roll = Random.Shared.Next(1, 21);
        if (roll == 20)
        {
            Heal(1);
            DeathSaveSuccesses = DeathSaveFailures = 0;
        }
        else if (roll == 1) DeathSaveFailures += 2;
        else if (roll >= 10) DeathSaveSuccesses++;
        else DeathSaveFailures++;

        if(DeathSaveSuccesses == 3)
        {
            Heal(1);
            DeathSaveSuccesses = DeathSaveFailures = 0;
        }
    }
}

