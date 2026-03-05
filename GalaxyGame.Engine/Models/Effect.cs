namespace GalaxyGame.Engine.Models;

public enum EffectType
{
    Explosion,
    Impact
}

public class Effect
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Size { get; set; }
    public EffectType Type { get; set; }
    public double TimeLeft { get; set; }
    public double Duration { get; set; }

    public double Progress => 1.0 - TimeLeft / Duration;
}
