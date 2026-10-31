namespace RpgBattle;

using System.Net.Http.Headers;
using System.Reflection.Metadata.Ecma335;
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
        bool run = true;
        while (run)
        {
            Console.Clear();
            Console.Write("\x1b[3J");
            _interface.CharacterSelectionUI(_availableCharacters, _playerTeam, _page, PageSize);

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

            if (TryGetIndex(key.KeyChar, _availableCharacters.Count, out index) && !IsSelected(_availableCharacters[index]))
            {
                _playerTeam.AddLast(_availableCharacters[index].Clone());
            }

            char keyBind = char.ToLower(key.KeyChar);

            switch (keyBind)
            {
                case 'd':
                    if (_playerTeam.Count > 0) _playerTeam.RemoveLast();
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
        char key = char.ToLower(Console.ReadKey(true).KeyChar);
        while (true)
        {
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
        bool turnOrderStatic = AskTurnOrder();
        _enemyTeam.Clear();
        GenerateEnemies();

        if (turnOrderStatic)
        {
            Queue<Character> turnOrder = CreateQueue();
            bool run = true;
            // turns
            while (run)
            {
                for (int i = 0; i < turnOrder.Count; i++)
                {
                    Console.Clear();
                    Console.Write("\x1b[3J");

                    Character current = turnOrder.Peek();
                    _interface.BattleUI(_playerTeam, _enemyTeam, turnOrder, current);

                    if (current is Enemy)
                    {
                        if (current.IsAlive)
                        {
                            //Choose action
                            //Choose target
                            //Attack
                        }
                    }
                    else if (current is Player player)
                    {
                        if (player.IsAlive)
                        {
                            if (player.IsDown)
                            {
                                player.RollDeathSave();
                            }
                            else
                            {
                                _interface.ShowCharacterAttacks(player, turnOrderStatic);
                            }
                        retry1:
                            char attackInput = char.ToLower(Console.ReadKey(true).KeyChar);

                            int attackIndex;
                            if (TryGetIndex(attackInput, player.Attacks.Count, out attackIndex))
                            {
                                Console.WriteLine($"Choose a target: [T] Teammate [E] Enemy");

                            retry2:
                                char targetInput = char.ToLower(Console.ReadKey(true).KeyChar);
                                int targetIndex;
                                switch (targetInput)
                                {
                                    case 't':
                                        _interface.ShowTargets(_playerTeam);

                                    retry3:
                                        char targetIndexIn = char.ToLower(Console.ReadKey(true).KeyChar);
                                        if (TryGetIndex(targetIndexIn, _playerTeam.Count, out targetIndex))
                                        {
                                            Character target = FindTargetInList(_playerTeam, targetIndex);

                                            bool hit() => current.RollToHit(current.Attacks[attackIndex]) > target.ArmorClass;
                                            if (hit())
                                            {
                                                target.TakeDamage(current.RollDamage(current.Attacks[attackIndex]), current.Attacks[attackIndex].DamageType);
                                            }
                                            else Console.WriteLine("Attack missed");
                                        }
                                        else goto retry3;

                                        break;
                                    case 'e':
                                        _interface.ShowTargets(_enemyTeam);
                                        break;
                                    default:
                                        goto retry2;
                                }
                            }
                            else goto retry1;
                        }
                    }

                    Console.ReadKey();

                    //Character goes to the back of the queue
                    turnOrder.Dequeue();
                    turnOrder.Enqueue(current);
                }
                char key = char.ToLower(Console.ReadKey(true).KeyChar);
                switch (key)
                {
                    case 'e':
                        _enemyTeam.Clear();
                        run = false;
                        break;
                }
            }

        }
        else if (!turnOrderStatic)
        {

        }

        //bool run = true;
        //while (run)
        //{
        //    
        //    
        //    

        //    
        //    
        //    
        //    
        //    
        //    
        //    
        //    
        //    
        //    
        //    
        //}
    }

    private T FindTargetInList<T>(LinkedList<T> targets, int targetIndex) where T : Character
    {
        T target = targets.First();
        int i = 1;
        foreach (T t in targets)
        {
            if (i < targetIndex) break;
            target = t;
            i++;
        }
        return target;
    }
    private Queue<Character> CreateQueue()
    {
        List<Character> list = new List<Character>();
        Queue<Character> queue = new Queue<Character>();

        foreach (Character character in _playerTeam)
        {
            character.RollForInitiative();
            list.Add(character);
        }
        foreach (Character character in _enemyTeam)
        {
            character.RollForInitiative();
            list.Add(character);
        }

        list.Sort((a, b) => a.Initiative.CompareTo(b.Initiative));

        foreach (Character character in list)
        {
            queue.Enqueue(character);
        }
        return queue;

    }
    private void GenerateEnemies()
    {
        if (Random.Shared.Next(1, 101) == 67)
        {
            Attack Scythe = new Attack("Scythe", 3, 3, DamageType.Slashing, new DiceRoll(1, 12));
            Attack Shitarmagedon = new Attack("Shitarmagedon", 3, 3, DamageType.Acid, new DiceRoll(10, 10));
            List<Attack> attacks = new List<Attack>();

            attacks.Add(Shitarmagedon);
            attacks.Add(Scythe);

            Enemy Boss = new Enemy("SHIT DEMON", 98, 16, 5, attacks, new List<Resistance>());
            _enemyTeam.AddLast(Boss);
            return;
        }

        int count = Random.Shared.Next(1, 3 * _playerTeam.Count + 1);
        for (int i = 0; i < count; i++)
        {
            int index = Random.Shared.Next(0, _availableEnemies.Count);
            _enemyTeam.AddLast(_availableEnemies[index].Clone());
        }
    }

    public void Run()
    {
        CharacterSelection();
    }
}