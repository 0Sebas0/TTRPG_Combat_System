namespace RpgBattle;

class StatusEffect
{
    public StatusEffectType Type { get; }
    public int Duration { get; set; }
    public StatusEffect(StatusEffectType type, int duration)
    {
        Type = type;
        Duration = duration;
    }
    
}