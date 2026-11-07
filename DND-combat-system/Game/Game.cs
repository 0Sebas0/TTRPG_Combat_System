namespace RpgBattle;

using System.Net.Http.Headers;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.ExceptionServices;
using System.Text.Json;
using System.Text.Json.Serialization;

class Game
{
    private List<Player> _availableCharacters = new();
    private List<Enemy> _availableEnemies = new();
    private LinkedList<Player> _playerTeam = new();
    private LinkedList<Enemy> _enemyTeam = new();
    private UI _interface = new UI();
    private int _page = 0;
    private const int PageSize = 9;
    private int PageCount() => Math.Max(1, (_availableCharacters.Count + PageSize - 1) / PageSize);
    public Game()
    {
        ReadJson();
    }

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    private void ReadJson()
    {
        _availableCharacters = LoadFromFile<Player>("characters.json");
        _availableEnemies = LoadFromFile<Enemy>("enemies.json");
    }
    private List<T> LoadFromFile<T>(string path) where T : Character
    {
        if (!File.Exists(path))
        {
            Console.WriteLine($"File not found: {path}");
            return new List<T>();
        }

        try
        {
            string json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<List<T>>(json, _jsonOptions)
                   ?? new List<T>();
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"Bad JSON in {path}: {ex.Message}");
            return new List<T>();
        }
    }
    private void SaveToFile(string path, List<Player> characters)
    {
        try
        {
            // Keep a backup of the old file, in case something goes wrong
            if (File.Exists(path))
            {
                File.Copy(path, path + ".bak", overwrite: true);
            }

            string json = JsonSerializer.Serialize(characters, _jsonOptions);
            File.WriteAllText(path, json);
        }
        catch (IOException ex)
        {
            Console.WriteLine($"Could not save to {path}: {ex.Message}");
        }
        catch (UnauthorizedAccessException ex)
        {
            Console.WriteLine($"No permission to write {path}: {ex.Message}");
        }
    }
    private bool TryGetIndex(char keyChar, int listSize, out int index)
    {
        index = -1;

        if (keyChar < '1' || keyChar > '9') return false;

        index = _page * PageSize + (keyChar - '1');
        return index < listSize;
    }
    private bool IsSelected(Character character)
    {
        foreach (Character c in _playerTeam)
        {
            if (c.Name == character.Name) return true;
        }
        return false;
    }
    private bool TryGetChoice(char key, int count, out int index)
    {
        index = key - '1';                       // '1' -> 0, '2' -> 1, ...
        return key >= '1' && key <= '9' && index < count;
    }
    private void InspectCharacter(List<Player> availableCharacters)
    {
        int onPage = Math.Min(PageSize, availableCharacters.Count - _page * PageSize);
        Console.WriteLine($"Which character you want to inspect? [1-{onPage}]");
        var key = Console.ReadKey(true);
        int index;
        if (TryGetIndex(key.KeyChar, availableCharacters.Count, out index))
        {
            _interface.ShowCharacterStats(availableCharacters[index]);
        }

    }
    private Player? CreateCustomCharacter()
    {
        while (true) // repeats only if the player chooses "Redo"
        {
            Console.Clear();
            Console.WriteLine("-------------------- CREATE A CHARACTER --------------------");
            Console.WriteLine();

            string name = AskUniqueName();
            int hp = _interface.AskInt("HP", 1, 999);
            int ac = _interface.AskInt("Armor Class (AC)", 1, 30);
            int initiativeMod = _interface.AskInt("Initiative bonus", -5, 10);

            List<Attack> attacks = AskAttacks();
            List<Resistance> resistances = AskResistances();

            Player character = new Player(name, hp, ac, initiativeMod, attacks, resistances);

            _interface.ShowCharacterStats(character); // shows a summary and waits for a key

            Console.WriteLine();
            Console.WriteLine("[S] Save  [R] Redo  [C] Cancel");

            char choice;
            do
            {
                choice = char.ToLower(Console.ReadKey(true).KeyChar);
            }
            while (choice != 's' && choice != 'r' && choice != 'c');

            if (choice == 's') return character;
            if (choice == 'c') return null;
            // choice == 'r' -> the while(true) starts over
        }
    }

    private string AskUniqueName()
    {
        while (true)
        {
            string name = _interface.AskName("Name");
            if (!NameExists(name)) return name;

            Console.WriteLine("A character with that name already exists. Choose another.");
        }
    }

    private bool NameExists(string name)
    {
        foreach (Character c in _availableCharacters)
        {
            if (string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }

    private List<Attack> AskAttacks()
    {
        const int maxAttacks = 4;
        List<Attack> attacks = new List<Attack>();

        Console.WriteLine();
        Console.WriteLine($"--- Add attacks (1 to {maxAttacks}) ---");

        do
        {
            attacks.Add(CreateCustomAttack());
        }
        while (attacks.Count < maxAttacks && _interface.AskYesNo("Add another attack?"));

        return attacks;
    }

    private Attack CreateCustomAttack()
    {
        Console.WriteLine();
        string name = _interface.AskName("Attack name");
        int bonusToHit = _interface.AskInt("Bonus to hit", -5, 20);
        DiceRoll damage = _interface.AskDiceRoll();
        int damageBonus = _interface.AskInt("Damage bonus", -5, 20);
        DamageType damageType = _interface.AskDamageType();

        return new Attack(name, bonusToHit, damageBonus, damageType, damage);
    }

    private List<Resistance> AskResistances()
    {
        List<Resistance> resistances = new List<Resistance>();

        Console.WriteLine();
        Console.WriteLine("--- Resistances, vulnerabilities, immunities ---");

        while (_interface.AskYesNo("Add one?"))
        {
            ResistanceLevel level = _interface.AskResistanceLevel();
            DamageType type = _interface.AskDamageType();

            if (HasEntryFor(resistances, type))
            {
                Console.WriteLine($"{type} already has an entry.");
            }
            else
            {
                resistances.Add(new Resistance(type, level));
            }
        }

        return resistances;
    }
    private bool HasEntryFor(List<Resistance> list, DamageType type)
    {
        foreach (Resistance r in list)
        {
            if (r.DamageType == type) return true;
        }
        return false;
    }

    private void CharacterSelection()
    {
        const int MaxCharacterCount = 5;
        int characterCount = 0;
        bool run = true;
        while (run)
        {
            Console.Clear();
            Console.Write("\x1b[3J");
            _interface.CharacterSelectionUI(_availableCharacters, _playerTeam, _page, PageSize, MaxCharacterCount, characterCount);

            var key = Console.ReadKey(true);

            if (key.Key == ConsoleKey.RightArrow)
            {
                if (_page < PageCount() - 1) _page++;
                continue;
            }
            if (key.Key == ConsoleKey.LeftArrow)
            {
                if (_page > 0) _page--;
                continue;
            }

            int index;

            if (TryGetIndex(key.KeyChar, _availableCharacters.Count, out index) && !IsSelected(_availableCharacters[index]) && characterCount < MaxCharacterCount)
            {
                _playerTeam.AddLast(_availableCharacters[index].Clone());
                characterCount++;
            }

            char keyBind = char.ToLower(key.KeyChar);

            switch (keyBind)
            {
                case 'd':
                    if (_playerTeam.Count > 0) _playerTeam.RemoveLast();
                    characterCount--;
                    break;
                case 'i':
                    InspectCharacter(_availableCharacters);
                    break;
                case 'c':
                    Player? created = CreateCustomCharacter();
                    if (created != null)
                    {
                        _availableCharacters.Add(created);
                        SaveToFile("characters.json", _availableCharacters);
                        _playerTeam.AddLast(created.Clone());
                        _page = PageCount() - 1;
                    }
                    break;
                case 'p':
                    if (_playerTeam.Count > 0) Battle();
                    break;
                case 'e':
                    run = false;
                    break;
            }
        }
    }

    private bool AskTurnOrder()
    {
        Console.WriteLine("Choose the turn Order: [S] Static [D] Dynamic");
        while (true)
        {
            char key = char.ToLower(Console.ReadKey(true).KeyChar);
            switch (key)
            {
                case 's':
                    return true;
                case 'd':
                    return false;
                default:
                    continue;
            }
        }
    }
    private void Battle()
    {
        bool turnOrderStatic;
        turnOrderStatic = AskTurnOrder();
        _enemyTeam.Clear();
        GenerateEnemies();

        if (turnOrderStatic)
        {
            RollForInitiative();
            Queue<Character> turnOrder = CreateQueue();
            // turns
            while (CheckWinCondition() == 0)
            {
                // One Round loop
                while (turnOrder.Count > 0)
                {
                    Console.Clear();
                    Console.Write("\x1b[3J");
                    if (CheckWinCondition() != 0) break;

                    Character current = turnOrder.Peek();
                    _interface.BattleUI(_playerTeam, _enemyTeam, turnOrder, current);

                    if (current is Enemy enemy)
                    {
                        if (enemy.IsAlive && !enemy.IsIncapacitated)
                        {
                            EnemyAction? action = enemy.Decide(_playerTeam, _enemyTeam);
                            if (action == null)
                            {
                                Console.WriteLine($"{enemy.Name} has nothing useful to do.");
                                Thread.Sleep(1800);
                            }
                            else
                            {
                                if (action.Reason != "")
                                {
                                    Console.WriteLine($"{enemy.Name} {action.Reason}.");
                                    Thread.Sleep(1800);
                                }
                                AttackResult result = PerformAttack(enemy, action.Attack, action.Target, turnOrderStatic);
                                enemy.Learn(action, result);
                            }
                        }
                    }
                    else if (current is Player player)
                    {
                        if (player.IsAlive)
                        {
                            if (player.IsDown)
                            {
                                int roll = player.RollDeathSave();
                                Console.WriteLine($"{player.Name} is down and rolls a death save.");
                                Thread.Sleep(1800);
                                Console.WriteLine($"{player.Name} rolled a {roll}.");
                                Thread.Sleep(2000);
                            }
                            else if (!player.IsIncapacitated)
                            {
                                Character? target;
                                Attack attack;
                                do
                                {
                                    _interface.ShowCharacterAttacks(player, turnOrderStatic);
                                    attack = AskAttack(player);
                                    target = AskTarget(attack);   // null means "Back"
                                }
                                while (target == null);
                                Console.Clear();
                                Console.Write("\x1b[3J");
                                _interface.BattleUI(_playerTeam, _enemyTeam, turnOrder, current);

                                PerformAttack(player, attack, target, turnOrderStatic);
                            }
                        }
                    }
                    if (current.StatusEffects.Count > 0)
                    {
                        current.ManageStatusEffects();
                        Thread.Sleep(1800);
                    }
                    if (current.IsAlive)
                        Console.WriteLine();
                    Console.WriteLine("Press any key to continue");
                    Console.ReadKey();

                    turnOrder.Dequeue();

                    if (turnOrder.Count == 0)
                    {
                        //Clean up
                        CleanUpTheDead<Enemy>(_enemyTeam);
                        turnOrder = CreateQueue();
                    }
                }
            }
            if (CheckWinCondition() == 1)
            {
                Console.Clear();
                Console.Write("\x1b[3J");
                _interface.BattleUI(_playerTeam, _enemyTeam, new Queue<Character>(), _playerTeam.First());
                Console.WriteLine("------ You Won :D ------");
                Console.WriteLine();
                Console.WriteLine("Press any key to go back to main menu.");
            }
            else if (CheckWinCondition() == -1)
            {
                Console.Clear();
                Console.Write("\x1b[3J");
                _interface.BattleUI(_playerTeam, _enemyTeam, new Queue<Character>(), _enemyTeam.First());
                Console.WriteLine("------ You Lost :( ------");
                Console.WriteLine();
                Console.WriteLine("Press any key to go back to main menu.");
            }
            Console.ReadKey();

        }
        else if (!turnOrderStatic)
        {
            CircularLinkedList<Character> turnOrder = CreateCricularList();
            LinkedListNode<Character> current = turnOrder.First;

            if (current.Value.StatusEffects.Count > 0)
            {
                current.Value.ManageStatusEffects();
                Thread.Sleep(1800);
            }

            while (CheckWinCondition() == 0)
            {
                // One Round loop
                while (CheckWinCondition() == 0)
                {
                    Console.Clear();
                    Console.Write("\x1b[3J");
                    if (CheckWinCondition() != 0) break;

                    _interface.BattleUI(_playerTeam, _enemyTeam, turnOrder, current.Value);

                    if (current.Value is Enemy enemy)
                    {
                        if (enemy.IsAlive && !enemy.IsIncapacitated)
                        {
                            EnemyAction? action = enemy.Decide(_playerTeam, _enemyTeam);
                            if (action == null)
                            {
                                Console.WriteLine($"{enemy.Name} has nothing useful to do.");
                                Thread.Sleep(1800);
                            }
                            else
                            {
                                if (action.Reason != "")
                                {
                                    Console.WriteLine($"{enemy.Name} {action.Reason}.");
                                    Thread.Sleep(1800);
                                }
                                AttackResult result = PerformAttack(enemy, action.Attack, action.Target, turnOrderStatic);
                                enemy.Learn(action, result);
                            }
                        }
                    }
                    else if (current.Value is Player player)
                    {
                        if (player.IsAlive)
                        {
                            if (player.IsDown)
                            {
                                int roll = player.RollDeathSave();
                                Console.WriteLine($"{player.Name} is down and rolls a death save.");
                                Thread.Sleep(1800);
                                Console.WriteLine($"{player.Name} rolled a {roll}.");
                                Thread.Sleep(2000);
                            }
                            else if (!player.IsIncapacitated)
                            {
                                Character? target;
                                Attack attack;
                                do
                                {
                                    _interface.ShowCharacterAttacks(player, turnOrderStatic);
                                    attack = AskAttack(player);
                                    target = AskTarget(attack);   // null means "Back"
                                }
                                while (target == null);
                                Console.Clear();
                                Console.Write("\x1b[3J");
                                _interface.BattleUI(_playerTeam, _enemyTeam, turnOrder, current.Value);

                                PerformAttack(player, attack, target, turnOrderStatic);
                            }
                        }

                        if (current.Value.IsAlive)
                            Console.WriteLine();
                        Console.WriteLine("Press any key to continue");
                        Console.ReadKey();


                        if (current.Value.HasEffect(StatusEffectType.Slowed) || current.Value.HasEffect(StatusEffectType.Haste))
                        {
                            LinkedListNode<Character> iterator = turnOrder.First;
                            while(true)
                            {
                                if(iterator == current)
                                {
                                    current = iterator;
                                    break;
                                }
                                iterator = turnOrder.NextOf(iterator);
                            }

                            turnOrder = CreateCricularList();
                        }
                        current = turnOrder.NextOf(current);
                        CleanUpTheDead<Enemy>(_enemyTeam);
                    }
                }
                _enemyTeam.Clear();
                _playerTeam.Clear();
            }
        }
    }
    private Queue<Character> CreateQueue()
    {
        List<Character> list = new List<Character>();
        Queue<Character> queue = new Queue<Character>();

        foreach (Character character in _playerTeam)
        {
            list.Add(character);
        }
        foreach (Character character in _enemyTeam)
        {
            list.Add(character);
        }

        list.Sort((a, b) => b.Initiative.CompareTo(a.Initiative));

        foreach (Character character in list)
        {
            if (character.IsAlive)
                queue.Enqueue(character);
        }
        return queue;

    }
    private CircularLinkedList<Character> CreateCricularList()
    {
        List<Character> list = new List<Character>();
        CircularLinkedList<Character> cll = new CircularLinkedList<Character>();

        foreach (Character character in _playerTeam)
        {
            list.Add(character);
        }
        foreach (Character character in _enemyTeam)
        {
            list.Add(character);
        }

        list.Sort((a, b) => b.EffectiveInitiative.CompareTo(a.EffectiveInitiative));

        foreach (Character character in list)
        {
            if (character.IsAlive)
                cll.AddLast(character);
        }
        return cll;
    }
    private void RollForInitiative()
    {
        foreach (Player character in _playerTeam)
        {
            character.RollForInitiative();
        }
        foreach (Enemy character in _enemyTeam)
        {
            character.RollForInitiative();
        }
    }
    private void GenerateEnemies()
    {
        if (Random.Shared.Next(1, 101) < 5) // 5% chance of spawning
        {
            Attack Gynimas = new Attack("Gynimas", 5, 3, DamageType.Piercing, new DiceRoll(1, 12));
            Attack Kolokviumas = new Attack("Kolokviumas", 5, 3, DamageType.Necrotic, new DiceRoll(3, 12));
            List<Attack> attacks = new List<Attack>();

            attacks.Add(Gynimas);
            attacks.Add(Kolokviumas);

            Enemy Boss = new Enemy("Rokas Štrimaitis", 200, 16, 6, attacks, new List<Resistance>());
            _enemyTeam.AddLast(Boss);
            return;
        }

        int count = Random.Shared.Next(1, 2 * _playerTeam.Count + 1);
        for (int i = 0; i < count; i++)
        {
            int index = Random.Shared.Next(0, _availableEnemies.Count);
            _enemyTeam.AddLast(_availableEnemies[index].Clone());
        }
    }
    private List<Character> GetTargets(bool players)
    {
        List<Character> targets = new List<Character>();

        if (players)
        {
            foreach (Player p in _playerTeam)
            {
                if (!p.IsDown) targets.Add(p);
            }
        }
        else
        {
            foreach (Enemy e in _enemyTeam)
            {
                if (!e.IsDown) targets.Add(e);
            }
        }
        return targets;
    }
    private Attack AskAttack(Player player)
    {
        while (true)
        {
            char key = char.ToLower(Console.ReadKey(true).KeyChar);
            if (TryGetChoice(key, player.Attacks.Count, out int index))
                return player.Attacks[index];
        }
    }
    private Character? AskTarget(Attack attack)
    {
        bool healing = attack.DamageType == DamageType.Heal;
        Console.WriteLine("Choose a target: [T] Teammate  [E] Enemy  [B] Back");

        while (true)
        {
            char key = char.ToLower(Console.ReadKey(true).KeyChar);

            List<Character> targets;
            if (key == 't') targets = GetTargets(true);
            else if (key == 'e') targets = GetTargets(false);
            else if (key == 'b') return null;
            else continue;

            if (targets.Count == 0)
            {
                Console.WriteLine("No valid targets there.");
                continue;
            }

            _interface.ShowTargets(targets);

            while (true)
            {
                char pick = char.ToLower(Console.ReadKey(true).KeyChar);
                if (TryGetChoice(pick, targets.Count, out int index))
                    return targets[index];
            }
        }
    }
    private AttackResult PerformAttack(Character attacker, Attack attack, Character target, bool turnOrderStatic)
    {
        if (attack.DamageType == DamageType.Heal)
        {
            int amount = attacker.RollDamage(attack);
            target.Heal(amount);
            Console.WriteLine($"{attacker.Name} uses {attack.Name} and heals {target.Name} for {amount} HP.");
            Thread.Sleep(1800);
            return new AttackResult(true, amount, amount);
        }

        if ((attack.DamageType == DamageType.Slow || attack.DamageType == DamageType.Haste) && !turnOrderStatic)
        {
            Console.WriteLine($"{attacker.Name} uses {attack.Name} on {target.Name}, {target.Name} is {attack.DamageType}ed");
            target.TakeDamage(0, attack.DamageType, 100);
            Thread.Sleep(1800);
            return new AttackResult(false, 0, 0);
        }

        int roll = attacker.RollToHit(attack);
        Console.WriteLine($"{attacker.Name} attacks {target.Name} with {attack.Name}");
        Thread.Sleep(1800);

        Console.Write($"Rolled {roll - attack.BonusToHit}");
        if (attack.BonusToHit < 0) Console.Write("-");
        else Console.Write("+");
        Console.WriteLine($"{attack.BonusToHit}");
        Thread.Sleep(1800);

        int chance = 0;
        int rawDamage = 0;
        int dealt = 0;
        bool hit = roll >= target.EffectiveArmorClass;
        if (hit)
        {
            int damage = attacker.RollDamage(attack);
            if (roll - attack.BonusToHit == 20)
            {
                damage = 2 * damage;
                chance = 100;
                Console.WriteLine("Critical hit!");
            }
            else
                Console.WriteLine("Attack hits");
            Thread.Sleep(1800);
            rawDamage = damage;   // the number BEFORE resistances are applied
            dealt = target.TakeDamage(damage, attack.DamageType, chance);
            Console.WriteLine($"{target.Name} takes {dealt} {attack.DamageType} damage");

        }
        else
        {
            Console.WriteLine("Attack missess");
        }
        Thread.Sleep(2000);
        return new AttackResult(hit, rawDamage, dealt);
    }

    private void CleanUpTheDead<T>(LinkedList<T> team) where T : Character
    {
        LinkedListNode<T>? node = team.First;

        while (node != null)
        {
            LinkedListNode<T>? next = node.Next;

            if (!node.Value.IsAlive)
                team.Remove(node);

            node = next;
        }
    }
    private int CheckWinCondition()
    {
        bool allUnconscious = true;
        foreach (Player player in _playerTeam)
        {
            if (!player.IsDown) allUnconscious = false;
        }

        // If all player characters are unconscious Player loses
        if (allUnconscious) return -1;

        // If all enemies are dead Player wins
        bool allDead = true;
        foreach (Enemy enemy in _enemyTeam)
        {
            if (enemy.IsAlive) allDead = false;
        }
        if (allDead) return 1;

        //else game continues
        return 0;
    }


    public void Run()
    {
        CharacterSelection();
    }
}