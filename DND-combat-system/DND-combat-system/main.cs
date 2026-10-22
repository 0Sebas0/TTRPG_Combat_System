using System.Text.Json;
using System.Text.Json.Serialization;

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
    public List<StatusEffect> StatusEffects { get; private set; }
    public int Initiative { get; private set; }
    public int InitiativeModifier { get; }

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
        StatusEffects = new List<StatusEffect>();
    }


    public bool IsAlive => CurrentHp > 0;
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
        ShowCharacterSelectionControls();
        Console.WriteLine();

    }
    public void ShowAvailableCharacters(List<Character> availableCharacters, LinkedList<Character> playerTeam)
    {
        int num = 1;
        foreach (Character character in availableCharacters)
        {
            if (playerTeam.Contains(character))
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
            Console.Write($"{character.Name} ");
        }
        Console.WriteLine();
    }
    public void ShowCharacterSelectionControls()
    {
        Console.WriteLine("Press: number(1-9) - to select a character D - delete last selected character");
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

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public Game()
    {
        ReadJson();
    }
    public void ReadJson()
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
    public void Run()
    {
        while (true)
        {
            Console.Clear();
            _interface.CharacterSelectionUI(_availableCharacters, _playerTeam);

            var key = Console.ReadKey(true);
            if (char.IsDigit(key.KeyChar))
            {
                int index = (key.KeyChar - '0') - 1;

                if (index >= 0 && index < _availableCharacters.Count && !_playerTeam.Contains(_availableCharacters[index]))
                {
                    _playerTeam.AddLast(_availableCharacters[index]);
                }
            }
            if(char.IsLetter(key.KeyChar))
            {
                char keyBind = char.ToLower((char)key.KeyChar);

                switch(keyBind)
                {
                    case 'd':
                        if(_playerTeam.Count > 0) _playerTeam.RemoveLast();
                        break;
                    case 'i':
                        ///////////// character stats inspection, need to write a helper method for converting number input
                        /// I + number - open character stats
                        break;
                }
            }
        }
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


