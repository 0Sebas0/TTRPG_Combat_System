using System.Text.Json.Serialization;

namespace RpgBattle;

class Enemy : Character
{
    [JsonConstructor]
    public Enemy(string name, int maxHp, int armorClass, int initiativeModifier, List<Attack> attacks, List<Resistance> resistances)
        : base(name, maxHp, armorClass, initiativeModifier, attacks, resistances) { }

    public override Enemy Clone() =>
        new Enemy(Name, MaxHp, ArmorClass, InitiativeModifier, new List<Attack>(Attacks), new List<Resistance>(Resistances));

    public Attack ChooseAttack()
    {
        //60% chance to choose default attack, 40% chance to choose stronger attack
        // if only 1 attack choose it
        // if more attacks distribute chances 1st 60% 2nd 40% 3rd 20% 4rd 15% etc. minimal percentage for attack is 15%
        return null;
    }
    public Character GetTarget(LinkedList<Player> playerTeam, LinkedList<Enemy> allyTeam)
    {
        // haste and heal targets only allies
        // for haste choose the slowest ally
        // for heal choose the ally with least Hp

        //other damage types target players
        //if target not chosen, choose target at random
        //if chosen check the attack memory if empty ChooseAttack()
        //if attack dealt massive damage to a player and player is still alive, remember it and use other time too.
        //if current target is dead clear memory related to it
        return null;
    }
}
