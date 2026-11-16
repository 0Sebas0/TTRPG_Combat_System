# TTRPG Combat System

A turn-based, console RPG battle simulator inspired by **Dungeons & Dragons 5th Edition**, written in C# (.NET 10).

Build a party of up to five heroes, pick a turn-order style, and fight a randomly generated group of monsters. Enemies are not purely random: they remember what they have seen and adapt their tactics during the fight.

---

## Features

- **Character selection screen** with paging, inspection, and in-game character creation
- **Data-driven content**: heroes and monsters live in JSON files, so you can add new ones without touching the code
- **D&D-style combat**: d20 attack rolls against Armor Class, critical hits, dice-based damage, initiative
- **Damage types** with resistance, vulnerability, and immunity
- **Status effects**: Poisoned, Stunned, Bleeding, Weakened, Slowed, Haste
- **Death saving throws** for downed heroes
- **Two turn-order modes**: Static and Dynamic
- **Enemy AI** with memory, weakness exploitation, focus fire, and emergency healing
- **A rare boss encounter** (about 4% chance each battle)

---

## Getting started

### Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- A terminal that supports Unicode and ANSI escape codes (Windows Terminal, macOS Terminal, most Linux terminals)

### Run it

```bash
git clone <your-repository-url>
cd <repository-folder>
dotnet run --project DND-combat-system
```

You can also open `DND-combat-system.slnx` in Visual Studio 2022 (17.14+) or JetBrains Rider and press **F5**.

---

## How to play

### Character selection

| Key | Action |
|-----|--------|
| `1`-`9` | Add the character at that position on the current page to your team |
| `←` / `→` | Previous / next page |
| `I` | Inspect a character (stats, attacks, resistances) |
| `D` | Remove the last character you added |
| `C` | Create a new custom character (saved to `characters.json`) |
| `P` | Start a battle |
| `E` | Exit |

A team holds up to **5** characters.

### In battle

1. Choose a turn-order mode: **[S] Static** or **[D] Dynamic**.
2. On your turn, press the number of an attack.
3. Choose a target group: **[T] Teammate**, **[E] Enemy**, or **[B] Back**, then pick the target by number.
4. Enemies act automatically. Press any key to advance after each turn.

You **win** when every enemy is dead. You **lose** when every hero is unconscious or dead.

---

## Game rules (as implemented)

### Attacks and damage

- **To hit:** `d20 + attack bonus` must be at least the target's Armor Class.
- **Damage:** roll the attack's dice (e.g. `2d6`) and add the damage bonus.
- **Critical hit:** a natural 20 doubles the damage and guarantees the damage type's status effect.
- **Resistance** halves damage (rounded down), **Vulnerability** doubles it, **Immunity** reduces it to 0.
- **Healing attacks** (`Heal` damage type) restore HP instead of dealing damage.

### Initiative and turn order

Everyone rolls `d20 + initiative modifier` once at the start of the battle.

| Mode | Behavior |
|------|----------|
| **Static** | Turn order is fixed for each round. `Slow` and `Haste` attacks have no effect. |
| **Dynamic** | The order is rebuilt at the end of every round. `Slowed` gives -10 initiative, `Haste` gives +10. `Slow`/`Haste` spells apply automatically (no attack roll). |

### Status effects

Each damaging hit has a chance to apply an effect, depending on its damage type. Effects tick down once per turn of the affected character. Applying an effect that is already active extends its duration.

| Effect | What it does | Duration | Caused by (chance) |
|--------|--------------|----------|--------------------|
| **Poisoned** | Disadvantage on attack rolls (roll twice, keep the lower) | 2-4 turns | Acid 20%, Poison 20% |
| **Stunned** | Skips the turn | 1-2 turns | Bludgeoning 30%, Lightning 40% |
| **Bleeding** | Takes 1d6 damage each turn | 2-4 turns | Piercing 20%, Slashing 10% |
| **Weakened** | Armor Class -2 | 2-4 turns | Acid 15%, Necrotic 25% |
| **Slowed** | Initiative -10 (Dynamic mode) | 3 turns | `Slow` spells |
| **Haste** | Initiative +10 (Dynamic mode) | 3 turns | `Haste` spells |

### Death saves

When a hero drops to 0 HP, they become **unconscious** and roll a death save on their turn:

| Roll | Result |
|------|--------|
| 20 | Regain 1 HP, counters reset |
| 10-19 | 1 success |
| 2-9 | 1 failure |
| 1 | 2 failures |

Three successes bring the hero back to 1 HP. Three failures mean the hero is **dead**.

---

## Enemy AI

Each enemy evaluates its options every turn. The first rule that applies wins:

1. **Emergency heal**: if the enemy knows a healing attack and an ally is at 40% HP or below, heal the most wounded ally.
2. **Exploit weakness**: if the enemy has *seen* a target take double damage from a damage type, use it.
3. **Focus fire**: if a target took a massive hit (40% or more of its max HP), keep attacking it.
4. **Finish off**: if an attack can kill a target and the hit chance is at least 40%, go for the kill.
5. **Weighted random**: pick among all useful options, favoring higher scores.

An option's score is `hit chance × expected damage × (2 − target HP ratio)`, so wounded targets are more attractive. Options the enemy knows are useless (e.g. a target immune to that damage type) get weight 0.

Enemies **learn** from observed results: after each hit they compare raw damage to damage dealt and remember whether a target resists, ignores, or is weak to that damage type. This memory lasts for one battle.

---

## Data files

Heroes and monsters are loaded from JSON at startup. Both files are copied next to the executable on build.

### `characters.json` (heroes) and `enemies.json` (monsters)

```json
{
  "name": "Goblin",
  "maxHp": 7,
  "armorClass": 15,
  "initiativeModifier": 2,
  "attacks": [
    {
      "name": "Scimitar",
      "bonusToHit": 4,
      "damageBonus": 2,
      "damageType": "Slashing",
      "damage": { "count": 1, "sides": 6 }
    }
  ],
  "resistances": [
    { "damageType": "Fire", "resistanceLevel": "Resistance" }
  ]
}
```

**Damage types:** `Bludgeoning`, `Slashing`, `Piercing`, `Fire`, `Cold`, `Acid`, `Lightning`, `Thunder`, `Poison`, `Force`, `Necrotic`, `Psychic`, `Radiant`, plus the special types `Heal`, `Haste`, `Slow`.

**Resistance levels:** `Resistance`, `Vulnerability`, `Immunity`.

> **Note:** characters created in-game are saved to `characters.json` in the *output* folder (`bin/.../`), and a `.bak` backup is kept. Copy it back into the project folder if you want to keep it in source control.

---

## Project structure

```
.
├── DND-combat-system.slnx
├── DND-combat-system/
│   ├── Program.cs                 # Entry point
│   ├── characters.json            # Playable heroes
│   ├── enemies.json               # Monster library
│   ├── Collections/
│   │   └── CircularLinkedList.cs  # Linked list with wrap-around navigation (Dynamic turns)
│   ├── Game/
│   │   └── Game.cs                # Menus, character creation, battle loop, combat resolution
│   ├── Models/
│   │   ├── Character.cs           # Abstract base: HP, AC, damage, status effects, rolls
│   │   ├── Player.cs              # Hero: death saves
│   │   ├── Enemy.cs               # Monster: AI decision-making and learning
│   │   ├── StatusEffect.cs        # A status effect and its remaining duration
│   │   ├── Records.cs             # DiceRoll, Attack, Resistance, AttackResult, EnemyAction
│   │   └── Enums.cs               # DamageType, ResistanceLevel, StatusEffectType
│   └── UI/
│       └── ConsoleUI.cs           # All console drawing and input prompts
└── README.md
```

---

## Architecture overview

- **Models** hold the data and rules of a single character (`Character` → `Player` / `Enemy`).
- **Records** are small immutable value objects loaded from JSON.
- **UI** is the only class that formats output and reads simple input.
- **Game** ties everything together: menus, the battle loop, and attack resolution.
- **Collections** contains a custom data structure used by the Dynamic turn order.

---

## Known limitations

- Enemies never use `Slow` or `Haste` attacks (the AI only considers damaging options).
- Critical hits double the *total* damage, while 5e doubles only the dice.
- Natural 1 does not always miss and natural 20 does not always hit (the roll is compared to AC like any other).
- Healing cannot target unconscious allies.
- Healing does not reset a hero's death-save counters.
- No ability scores, saving throws, advantage from sources other than Poisoned, spell slots, or movement.
