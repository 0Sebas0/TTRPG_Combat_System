using System.Net.NetworkInformation;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Linq;

public enum DamageType { Bludgeoning, Slashing, Piercing, Fire, Cold, Acid, Lightning, Thunder, Poison, Force, Necrotic, Psychic, Radiant }
public enum ResistanceLevel { Resistance, Vulnerability, Immunity }
public enum StatusEffect { Poisoned, Stunned, Restrained, Unconscious, Prone }
public record DiceRoll(int Count, int Sides);
public record Attack(string Name, int BonusToHit, int DamageBonus, DamageType DamageType, DiceRoll Damage);
public record Resistance(DamageType DamageType, ResistanceLevel ResistanceLevel);
class Character
{
    public string Name { get; }
    public int MaxHp { get; }
    public int CurrentHp { get; private set; }
    public int ArmorClass { get; }
    public List<Attack> Attacks { get; }
    public List<Resistance> Resistances { get; }
    public LinkedList<StatusEffect> StatusEffects { get; private set; }
    public int Initiative { get; private set; }
    public int InitiativeModifier { get; }
    public bool IsAlive => CurrentHp > 0;

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

class UI
{
    public void CharacterSelectionUI(List<Character> availableCharacters, LinkedList<Character> playerTeam)
    {
        Console.WriteLine("-------------------- SELECT YOUR FIGHTERS --------------------");
        ShowAvailableCharacters(availableCharacters, playerTeam);
        Console.WriteLine("--------------------------------------------------------------");
        Console.WriteLine();
        ShowChosencharacters(playerTeam);
        Console.WriteLine();
        Console.WriteLine("--------------------------------------------------------------");
        ShowCharacterSelectionControls(availableCharacters);
        Console.WriteLine();

    }
    public void ShowAvailableCharacters(List<Character> availableCharacters, LinkedList<Character> playerTeam)
    {
        int num = 1;
        foreach (Character character in availableCharacters)
        {
            bool selected = false;
            foreach (Character character2 in playerTeam)
            {
                if (character2.Name == character.Name)
                {
                    selected = true;
                    break;
                }
            }
            if (selected)
            {
                Console.WriteLine($"{num}. {character.Name} (selected)");
            }
            else
            {
                Console.WriteLine($"{num}. {character.Name}");
            }
            num++;
        }
    }
    public void ShowChosencharacters(LinkedList<Character> playerTeam)
    {
        Console.Write("Your team: ");
        foreach (Character character in playerTeam)
        {
            Console.Write($"[{character.Name}] ");
        }
        Console.WriteLine();
    }
    public void ShowCharacterSelectionControls(List<Character> availableCharacters)
    {
        Console.WriteLine($"[1-{availableCharacters.Count()}] Pick character [I] Inspect [D] Remove last pick [C] Create a character [N] Next");
    }
    public void ShowCharacterStats(Character c)
    {
        Console.WriteLine("--------------------------------------------------------------");
        Console.WriteLine();
        Console.WriteLine($"=== {c.Name} ===");
        Console.WriteLine();
        Console.WriteLine("--- STATS ---");
        Console.WriteLine($"HP: {c.MaxHp}   AC: {c.ArmorClass}");
        Console.Write($"Initiative: "); if (c.InitiativeModifier > 0) Console.Write('+'); Console.WriteLine(c.InitiativeModifier);
        Console.WriteLine();
        Console.WriteLine("--- Attacks ---");
        foreach (Attack attack in c.Attacks)
        {
            Console.Write($"{attack.Name} ATK");
            if (attack.BonusToHit >= 0) Console.Write('+');
            Console.Write($"{attack.BonusToHit} ");
            Console.Write($"{attack.Damage.Count}d{attack.Damage.Sides}");
            if (attack.DamageBonus > 0) Console.Write("+");
            if (attack.DamageBonus != 0) Console.Write($"{attack.DamageBonus}"); Console.WriteLine($" {attack.DamageType}");
        }
        Console.WriteLine();
        Console.WriteLine("--- Other ---");

        int count = 0;
        Console.Write("Resistances: ");
        foreach (Resistance resistance in c.Resistances)
        {
            if (resistance.ResistanceLevel == ResistanceLevel.Resistance)
            {
                Console.Write($"{resistance.DamageType} ");
                count++;
            }

        }
        if (count == 0) Console.WriteLine("none");
        else Console.WriteLine();
        Console.WriteLine();

        count = 0;
        Console.Write("Vulnerabilities: ");
        foreach (Resistance resistance in c.Resistances)
        {
            if (resistance.ResistanceLevel == ResistanceLevel.Vulnerability)
            {
                Console.Write($"{resistance.DamageType} ");
                count++;
            }
        }
        if (count == 0) Console.WriteLine("none");
        else Console.WriteLine();
        Console.WriteLine();

        count = 0;
        Console.Write("Immunities: ");
        foreach (Resistance resistance in c.Resistances)
        {
            if (resistance.ResistanceLevel == ResistanceLevel.Immunity)
            {
                Console.Write($"{resistance.DamageType} ");
                count++;
            }
        }
        if (count == 0) Console.WriteLine("none");
        else Console.WriteLine();


        Console.ReadKey();
    }
    public void ShowValidDamageTypes()
    {
        DamageType[] all = Enum.GetValues<DamageType>();

        const int columns = 3;
        int rows = (int)Math.Ceiling(all.Length / (double)columns); // 13 / 3 -> 5 rows

        Console.WriteLine("Choose a damage type:");

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                int index = row + col * rows; // fill column by column

                if (index < all.Length)
                {
                    // {text,-18} pads to 18 characters, left-aligned
                    Console.Write($"{index + 1,2}. {all[index],-14}");
                }
            }
            Console.WriteLine(); // end of this row
        }
    }
    public string AskName(string prompt = "Name")
    {
        while (true)
        {
            Console.Write($"{prompt}: ");
            string? name = Console.ReadLine()?.Trim();

            if (!string.IsNullOrWhiteSpace(name)) return name;

            Console.WriteLine("The name cannot be empty.");
        }
    }

    public int AskInt(string prompt, int min, int max)
    {
        while (true)
        {
            Console.Write($"{prompt} [{min} to {max}]: ");

            if (int.TryParse(Console.ReadLine(), out int value) && value >= min && value <= max)
                return value;

            Console.WriteLine($"Invalid. Enter a whole number from {min} to {max}.");
        }
    }

    public bool AskYesNo(string prompt)
    {
        Console.Write($"{prompt} [Y/N]: ");
        while (true)
        {
            char key = char.ToLower(Console.ReadKey(true).KeyChar);
            if (key == 'y') { Console.WriteLine("Yes"); return true; }
            if (key == 'n') { Console.WriteLine("No"); return false; }
        }
    }

    public DiceRoll AskDiceRoll()
    {
        int count = AskInt("Number of damage dice", 1, 20);

        int[] validSides = { 4, 6, 8, 10, 12, 20 }; // the dice that exist in D&D 5e
        while (true)
        {
            Console.Write("Die type (4, 6, 8, 10, 12 or 20): d");

            if (int.TryParse(Console.ReadLine(), out int sides) && validSides.Contains(sides))
                return new DiceRoll(count, sides);

            Console.WriteLine("That die does not exist. Pick one from the list.");
        }
    }

    public DamageType AskDamageType()
    {
        DamageType[] all = Enum.GetValues<DamageType>();
        ShowValidDamageTypes(); // already prints "Choose a damage type:"

        while (true)
        {
            Console.Write("> ");

            if (int.TryParse(Console.ReadLine(), out int choice) && choice >= 1 && choice <= all.Length)
                return all[choice - 1];

            Console.WriteLine("Invalid choice. Enter a number from the list.");
        }
    }

    public ResistanceLevel AskResistanceLevel()
    {
        Console.WriteLine("1. Resistance    (half damage)");
        Console.WriteLine("2. Vulnerability (double damage)");
        Console.WriteLine("3. Immunity      (no damage)");

        int choice = AskInt("Choose", 1, 3);
        return (ResistanceLevel)(choice - 1); // enum values start at 0: Resistance=0, Vulnerability=1, Immunity=2
    }


}


//--------------------------- <<Main game loop>> ---------------------------
class Game
{
    private List<Character> _availableCharacters = new();
    private List<Character> _availableEnemies = new();
    private LinkedList<Character> _playerTeam = new();
    private LinkedList<Character> _enemyTeam = new();
    private UI _interface = new UI();
    public Game()
    {
        ReadJson();
    }

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
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

namespace main
{
    class Program
    {
        static int Main()
        {
            Game Game = new Game();
            Game.Run();
            return 0;
        }

    }
}

// Notes:
/// review UI class Ask methods
/// review Character creation methods


