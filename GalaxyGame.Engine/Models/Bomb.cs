namespace GalaxyGame.Engine.Models;

public class Bomb : GameObject
{
    public double SpeedY { get; set; } = 4.0;

    public Bomb()
    {
        Width = 10;
        Height = 10;
    }
}