using System.Text.Json.Serialization;
using static System.Net.Mime.MediaTypeNames;

namespace RpgBattle;

class Enemy : Character
{
    // ---------- Tuning knobs: change these to change the enemy's "personality" ----------
    private const double HealBelowHpRatio = 0.4;   
    private const double MassiveDamageRatio = 0.4; 
    private const double MinFinishHitChance = 0.4; 
    private const double MinWeightShare = 0.15;    

    // ---------- Memory ----------
    private Player? _focusTarget;                 
    private Attack? _focusAttack;                 
    // What was OBSERVED: (target, damage type) -> 0 = no damage, 0.5 = half, 1 = normal, 2 = double
    private readonly Dictionary<(Character, DamageType), double> _knownMultipliers = new();

    // One possible move: "use this attack on that target", with the numbers needed to rate it
    private record Option(Attack Attack, Character Target, double HitChance, double Potential, double Multiplier)
    {
        // Expected damage, and wounded targets are up to 2x as attractive
        public double Score =>
            HitChance * Potential * (2.0 - (double)Target.CurrentHp / Target.MaxHp);
    }

    [JsonConstructor]
    public Enemy(string name, int maxHp, int armorClass, int initiativeModifier, List<Attack> attacks, List<Resistance> resistances)
        : base(name, maxHp, armorClass, initiativeModifier, attacks, resistances) { }

    public override Enemy Clone() =>
        new Enemy(Name, MaxHp, ArmorClass, InitiativeModifier, new List<Attack>(Attacks), new List<Resistance>(Resistances));

    // =====================================================================================
    //  DECIDING
    // =====================================================================================
    public EnemyAction? Decide(IEnumerable<Player> players, IEnumerable<Enemy> allies)
    {
        // Step 1: Get still standing targets
        List<Player> opponents = new List<Player>();
        foreach (Player p in players)
            if (!p.IsDown) opponents.Add(p);

        List<Enemy> friends = new List<Enemy>();
        foreach (Enemy e in allies)
            if (!e.IsDown) friends.Add(e); 

        if (opponents.Count == 0) return null;

        // Step 2: forget a focus target that is no longer standing
        if (_focusTarget != null && !opponents.Contains(_focusTarget))
        {
            _focusTarget = null;
            _focusAttack = null;
        }

        // Step 3: the rules. The first rule that returns something is applied.
        EnemyAction? action = TryEmergencyHeal(friends);
        if (action != null) return action;

        List<Option> options = BuildOptions(opponents);
        if (options.Count == 0) return null;        // nothing usable -> skip turn

        action = TryExploitWeakness(options);
        if (action != null) return action;

        action = TryFocus(options);
        if (action != null) return action;

        action = TryFinishOff(options);
        if (action != null) return action;



        return ChooseWeighted(options);
    }

    // ----- Rule 1: heal a badly hurt ally -----
    private EnemyAction? TryEmergencyHeal(List<Enemy> friends)
    {
        Attack? heal = null;
        foreach (Attack a in Attacks)
        {
            if (a.IsHeal) { heal = a; break; }
        }
        if (heal == null) return null;

        Enemy? neediest = null;
        double lowest = double.MaxValue;
        foreach (Enemy e in friends)
        {
            double ratio = (double)e.CurrentHp / e.MaxHp;
            if (ratio <= HealBelowHpRatio && ratio < lowest)
            {
                neediest = e;
                lowest = ratio;
            }
        }
        if (neediest == null) return null;

        string reason = neediest == this ? "tends to its own wounds" : $"tends to {neediest.Name}'s wounds";
        return new EnemyAction(heal, neediest, reason);
    }

    // ----- Rule 2: if an attack should kill someone, do it -----
    private EnemyAction? TryFinishOff(List<Option> options)
    {
        Option? best = null;
        foreach (Option o in options)
        {
            bool shouldKill = o.Potential >= o.Target.CurrentHp;
            if (!shouldKill || o.HitChance < MinFinishHitChance) continue;

            if (best == null || o.HitChance > best.HitChance) best = o;
        }
        if (best == null) return null;

        return new EnemyAction(best.Attack, best.Target, $"sees a chance to finish off {best.Target.Name}");
    }

    // ----- Rule 3: keep hitting the target that took a massive hit -----
    private EnemyAction? TryFocus(List<Option> options)
    {
        if (_focusTarget == null) return null;

        Option? best = null;
        foreach (Option o in options)
        {
            if (o.Target != _focusTarget || o.Multiplier == 0) continue;

            if (o.Attack == _focusAttack)               // the remembered attack still works: use it
                return new EnemyAction(o.Attack, o.Target, $"keeps pressing {o.Target.Name}");

            if (best == null || o.Score > best.Score) best = o;
        }
        if (best == null) return null;

        return new EnemyAction(best.Attack, best.Target, $"keeps pressing {best.Target.Name}");
    }

    // ----- Rule 4: use a damage type I have SEEN a target take extra damage from -----
    private EnemyAction? TryExploitWeakness(List<Option> options)
    {
        Option? best = null;
        foreach (Option o in options)
        {
            if (o.Multiplier < 2) continue;
            if (best == null || o.Score > best.Score) best = o;
        }
        if (best == null) return null;

        return new EnemyAction(best.Attack, best.Target,
            $"remembers that {best.Target.Name} is weak to {best.Attack.DamageType}");
    }

    // ----- Rule 5 (default): weighted random choice among everything that works -----
    private EnemyAction ChooseWeighted(List<Option> options)
    {
        double bestScore = 0;
        foreach (Option o in options)
            if (o.Score > bestScore) bestScore = o.Score;

        // Known-useless options get weight 0. Everything else gets at least 15% of the best weight.
        double[] weights = new double[options.Count];
        double total = 0;
        for (int i = 0; i < options.Count; i++)
        {
            weights[i] = options[i].Multiplier == 0 ? 0 : Math.Max(options[i].Score, bestScore * MinWeightShare);
            total += weights[i];
        }

        // Everything is known to be useless: still attack something, but say so
        if (total <= 0)
        {
            Option desperate = options[Random.Shared.Next(options.Count)];
            return new EnemyAction(desperate.Attack, desperate.Target, "finds nothing that works and lashes out anyway");
        }

        // Roulette wheel: roll a number in [0, total), then walk the options subtracting weights
        double roll = Random.Shared.NextDouble() * total;
        for (int i = 0; i < options.Count; i++)
        {
            roll -= weights[i];
            if (roll < 0)
                return new EnemyAction(options[i].Attack, options[i].Target, "");
        }
        return new EnemyAction(options[^1].Attack, options[^1].Target, "");   // safety net for rounding
    }

    // ----- Building the list of options -----
    private List<Option> BuildOptions(List<Player> opponents)
    {
        List<Option> options = new List<Option>();
        foreach (Attack attack in Attacks)
        {
            if (!attack.IsDamaging) continue;           // heal / haste / slow are handled elsewhere

            foreach (Player target in opponents)
            {
                double multiplier = KnownMultiplier(target, attack.DamageType);
                double potential = attack.AverageDamage * multiplier;
                options.Add(new Option(attack, target, HitChance(attack, target), potential, multiplier));
            }
        }
        return options;
    }

    // Probability that "d20 + bonus" reaches the target's armor class
    private double HitChance(Attack attack, Character target)
    {
        int needed = target.EffectiveArmorClass - attack.BonusToHit;
        double chance = Math.Clamp((21 - needed) / 20.0, 0.05, 0.95);
        if (HasEffect(StatusEffectType.Poisoned)) chance *= chance;   // disadvantage: roll twice, keep the lower
        return chance;
    }

    private double KnownMultiplier(Character target, DamageType type)
    {
        if (_knownMultipliers.TryGetValue((target, type), out double multiplier))
            return multiplier;
        return 1.0;     // never seen it: assume normal damage
    }

    // =====================================================================================
    //  LEARNING (Game calls this right after the attack)
    // =====================================================================================
    public void Learn(EnemyAction action, AttackResult result)
    {
        if (!action.Attack.IsDamaging) return;          // healing teaches nothing about resistances
        if (action.Target is not Player target) return;

        // A miss tells me nothing about resistances. A raw damage of 0 or less says nothing either.
        if (result.Hit && result.RawDamage > 0)
        {
            double? observed = null;

            if (result.DamageDealt > result.RawDamage) observed = 2.0;                      // vulnerability
            else if (result.DamageDealt == result.RawDamage) observed = 1.0;                 // normal
            else if (result.DamageDealt > 0) observed = 0.5;                                 // resistance
            else if (result.RawDamage >= 2) observed = 0.0;                                  // 

            if (observed != null)
                _knownMultipliers[(target, action.Attack.DamageType)] = observed.Value;
        }

        // Memory of a massive hit
        if (target.IsDown)
        {
            if (_focusTarget == target) { _focusTarget = null; _focusAttack = null; }
        }
        else if (result.Hit && result.DamageDealt >= MassiveDamageRatio * target.MaxHp)
        {
            _focusTarget = target;
            _focusAttack = action.Attack;
        }
    }
}