namespace RpgBattle;
using System.Text.Json;
using System.Text.Json.Serialization;

class Game
{
    private List<Character> _availableCharacters = new();
    private List<Character> _availableEnemies = new();
    private LinkedList<Character> _playerTeam = new();
    private LinkedList<Character> _enemyTeam = new();
    private UI _interface = new UI();
    private int _page = 0;
    private const int PageSize = 9;
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
        _availableCharacters = LoadFromFile("characters.json");
        _availableEnemies = LoadFromFile("enemies.json");
    }
    private List<Character> LoadFromFile(string path)
    {
        if (!File.Exists(path))
        {
            Console.WriteLine($"File not found: {path}");
            return new List<Character>();
        }

        try
        {
            string json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<List<Character>>(json, _jsonOptions)
                   ?? new List<Character>();
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"Bad JSON in {path}: {ex.Message}");
            return new List<Character>();
        }
    }
    private void SaveToFile(string path, List<Character> characters)
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

        if (!char.IsDigit(keyChar)) return false;

        index = (keyChar - '0') - 1;
        return index >= 0 && index < listSize;
    }
    private bool IsSelected(Character character)
    {
        foreach (Character c in _playerTeam)
        {
            if (c.Name == character.Name) return true;
        }
        return false;
    }
    private void InspectCharacter(List<Character> availableCharacters)
    {
        Console.WriteLine($"Which character you want to inspect? [1-{availableCharacters.Count()}]");
        var key = Console.ReadKey(true);
        int index;
        if (TryGetIndex(key.KeyChar, availableCharacters.Count, out index))
        {
            _interface.ShowCharacterStats(availableCharacters[index]);
        }

    }
    private Character? CreateCustomCharacter()
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

            Character character = new Character(name, hp, ac, initiativeMod, attacks, resistances);

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
            _interface.CharacterSelectionUI(_availableCharacters, _playerTeam);

            var key = Console.ReadKey(true);

            int index;

            if (TryGetIndex(key.KeyChar, _availableCharacters.Count(), out index) && !IsSelected(_availableCharacters[index]))
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
                    Character? created = CreateCustomCharacter();
                    if (created != null)
                    {
                        _availableCharacters.Add(created);
                        SaveToFile("characters.json", _availableCharacters);
                        _playerTeam.AddLast(created.Clone());
                    }
                    break;
                case 'n':
                    if (_playerTeam.Count > 0) run = false;
                    break;
            }
        }
    }
    public void Run()
    {
        CharacterSelection();
    }
}

// TODO:
// Implement available character selection list paging