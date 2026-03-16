namespace GalaxyGame.Engine.Models;

public class Ship : GameObject
{
    public double SpeedX { get; set; } = 1.5;
    public int Health { get; set; } = 3;
    public int MaxHealth { get; set; } = 3;
    public int Points { get; set; } = 500;

    public Ship()
    {
        Width = 80;
        Height = 30;
    }
}