namespace GalaxyGame.Engine.Models;

public class Enemy : GameObject
{
    public double SpeedX { get; set; }
    public double SpeedY { get; set; } = 2.0;
    public int Points { get; set; } = 100;

    public Enemy()
    {
        Width = 36;
        Height = 36;
    }
}
