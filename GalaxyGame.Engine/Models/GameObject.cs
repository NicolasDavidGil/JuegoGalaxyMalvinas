namespace GalaxyGame.Engine.Models;

public abstract class GameObject
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
    public bool IsAlive { get; set; } = true;

    public bool CollidesWith(GameObject other)
    {
        return IsAlive && other.IsAlive &&
               X < other.X + other.Width &&
               X + Width > other.X &&
               Y < other.Y + other.Height &&
               Y + Height > other.Y;
    }
}
