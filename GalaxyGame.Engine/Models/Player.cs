namespace GalaxyGame.Engine.Models;

public class Player : GameObject
{
    public int Lives { get; set; } = 3;
    public int Score { get; set; }
    public double Speed { get; set; } = 6.0;
    public double ShootCooldown { get; set; }

    public Player()
    {
        Width = 40;
        Height = 40;
    }
}
