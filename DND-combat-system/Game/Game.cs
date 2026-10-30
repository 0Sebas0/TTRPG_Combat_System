namespace RpgBattle;

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
    private void Battle()
    {
        GenerateEnemies();
        while (true)
        {
            Console.Clear();
            _interface.BattleUI(_playerTeam, _enemyTeam);
            Console.ReadKey();
        }
    }
    private void GenerateEnemies()
    {
        var rand = new Random();
        int count = rand.Next(1, 10);
        for (int i = 0; i < count; i++)
        {
            int index = rand.Next(0, _availableEnemies.Count);
            _enemyTeam.AddLast(_availableEnemies[index].Clone());
        }
    }
    public void Run()
    {
        CharacterSelection();
    }
}