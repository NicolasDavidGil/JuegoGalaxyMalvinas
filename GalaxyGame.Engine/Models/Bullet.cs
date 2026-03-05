namespace GalaxyGame.Engine.Models;

public class Bullet : GameObject
{
    public double SpeedY { get; set; } = -10.0;

    public Bullet()
    {
        Width = 4;
        Height = 12;
    }
}
