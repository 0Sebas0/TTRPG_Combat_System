namespace main;
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
