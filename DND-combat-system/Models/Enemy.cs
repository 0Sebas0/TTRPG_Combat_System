using System.Text.Json.Serialization;

namespace RpgBattle;

class Enemy : Character
{
    [JsonConstructor]
    public Enemy(string name, int maxHp, int armorClass, int initiativeModifier, List<Attack> attacks, List<Resistance> resistances)
        : base(name, maxHp, armorClass, initiativeModifier, attacks, resistances) { }

    public override Enemy Clone() =>
        new Enemy(Name, MaxHp, ArmorClass, InitiativeModifier, new List<Attack>(Attacks), new List<Resistance>(Resistances));
}
