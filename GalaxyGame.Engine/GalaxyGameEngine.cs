using GalaxyGame.Engine.Models;

namespace GalaxyGame.Engine;

public enum GameState
{
    Menu,
    Playing,
    Paused,
    GameOver
}

public class GalaxyGameEngine
{
    private readonly Random _random = new();

    public Player Player { get; } = new();
    public List<Enemy> Enemies { get; } = [];
    public List<Bullet> PlayerBullets { get; } = [];
    public List<Bullet> EnemyBullets { get; } = [];
    public List<Star> Stars { get; } = [];
    public List<IslandPatch> Islands { get; } = [];
    public List<Effect> Effects { get; } = [];
    public GameState State { get; set; } = GameState.Menu;

    public double AreaWidth { get; set; } = 500;
    public double AreaHeight { get; set; } = 700;

    public int Wave { get; private set; } = 1;

    private double _enemySpawnTimer;
    private double _enemySpawnInterval = 1.5;
    private int _enemiesSpawnedInWave;
    private int _enemiesPerWave = 6;
    private double _islandSpawnTimer;

    public void StartGame()
    {
        Player.X = AreaWidth / 2 - Player.Width / 2;
        Player.Y = AreaHeight - 80;
        Player.IsAlive = true;
        Player.Lives = 3;
        Player.Score = 0;
        Player.ShootCooldown = 0;

        Enemies.Clear();
        PlayerBullets.Clear();
        EnemyBullets.Clear();
        Effects.Clear();

        Wave = 1;
        _enemySpawnTimer = 0;
        _enemySpawnInterval = 1.5;
        _enemiesSpawnedInWave = 0;
        _enemiesPerWave = 6;

        InitStars();
        InitIslands();
        State = GameState.Playing;
    }

    private void InitStars()
    {
        Stars.Clear();
        for (int i = 0; i < 80; i++)
        {
            Stars.Add(new Star
            {
                X = _random.NextDouble() * AreaWidth,
                Y = _random.NextDouble() * AreaHeight,
                Speed = 0.5 + _random.NextDouble() * 2.5,
                Size = 1 + _random.NextDouble() * 2,
                Opacity = 0.3 + _random.NextDouble() * 0.7
            });
        }
    }

    private void InitIslands()
    {
        Islands.Clear();
        _islandSpawnTimer = 0;
        for (int i = 0; i < 3; i++)
        {
            SpawnIsland(_random.NextDouble() * AreaHeight);
        }
    }

    private void SpawnIsland(double startY)
    {
        double w = 70 + _random.NextDouble() * 130;
        double h = 50 + _random.NextDouble() * 90;
        Islands.Add(new IslandPatch
        {
            X = _random.NextDouble() * (AreaWidth - w),
            Y = startY,
            Width = w,
            Height = h,
            Speed = 1.0 + _random.NextDouble() * 0.5,
            Seed = _random.Next()
        });
    }

    private void UpdateIslands(double deltaTime)
    {
        foreach (var island in Islands)
        {
            island.Y += island.Speed * 60 * deltaTime;
        }
        Islands.RemoveAll(i => i.Y > AreaHeight + 120);

        _islandSpawnTimer -= deltaTime;
        if (_islandSpawnTimer <= 0)
        {
            SpawnIsland(-120);
            _islandSpawnTimer = 2.0 + _random.NextDouble() * 3.0;
        }
    }

    public void Update(double deltaTime, bool moveLeft, bool moveRight, bool shooting)
    {
        if (State != GameState.Playing) return;

        UpdateStars(deltaTime);
        UpdateIslands(deltaTime);
        UpdatePlayer(deltaTime, moveLeft, moveRight, shooting);
        UpdateBullets(deltaTime);
        SpawnEnemies(deltaTime);
        UpdateEnemies(deltaTime);
        CheckCollisions();
        CleanupDead();
        UpdateEffects(deltaTime);
        CheckWaveComplete();
    }

    private void UpdateStars(double deltaTime)
    {
        foreach (var star in Stars)
        {
            star.Y += star.Speed * 60 * deltaTime;
            if (star.Y > AreaHeight)
            {
                star.Y = 0;
                star.X = _random.NextDouble() * AreaWidth;
            }
        }
    }

    private void UpdatePlayer(double deltaTime, bool moveLeft, bool moveRight, bool shooting)
    {
        double speed = Player.Speed * 60 * deltaTime;

        if (moveLeft) Player.X -= speed;
        if (moveRight) Player.X += speed;

        Player.X = Math.Clamp(Player.X, 0, AreaWidth - Player.Width);

        Player.ShootCooldown -= deltaTime;
        if (shooting && Player.ShootCooldown <= 0)
        {
            PlayerBullets.Add(new Bullet
            {
                X = Player.X + Player.Width / 2 - 2,
                Y = Player.Y - 12,
                SpeedY = -10.0
            });
            Player.ShootCooldown = 0.2;
        }
    }

    private void UpdateBullets(double deltaTime)
    {
        foreach (var bullet in PlayerBullets)
        {
            bullet.Y += bullet.SpeedY * 60 * deltaTime;
            if (bullet.Y < -20) bullet.IsAlive = false;
        }

        foreach (var bullet in EnemyBullets)
        {
            bullet.Y += bullet.SpeedY * 60 * deltaTime;
            if (bullet.Y > AreaHeight + 20) bullet.IsAlive = false;
        }
    }

    private void SpawnEnemies(double deltaTime)
    {
        if (_enemiesSpawnedInWave >= _enemiesPerWave) return;

        _enemySpawnTimer -= deltaTime;
        if (_enemySpawnTimer <= 0)
        {
            double x = _random.NextDouble() * (AreaWidth - 36);
            var enemy = new Enemy
            {
                X = x,
                Y = -40,
                SpeedX = (_random.NextDouble() - 0.5) * 2,
                SpeedY = 1.0 + Wave * 0.3,
                Points = 100 + (Wave - 1) * 20
            };
            Enemies.Add(enemy);
            _enemiesSpawnedInWave++;
            _enemySpawnTimer = _enemySpawnInterval;
        }
    }

    private void UpdateEnemies(double deltaTime)
    {
        foreach (var enemy in Enemies)
        {
            enemy.Y += enemy.SpeedY * 60 * deltaTime;
            enemy.X += enemy.SpeedX * 60 * deltaTime;

            if (enemy.X <= 0 || enemy.X >= AreaWidth - enemy.Width)
                enemy.SpeedX = -enemy.SpeedX;

            if (enemy.Y > AreaHeight + 40)
                enemy.IsAlive = false;

            // Enemigos disparan aleatoriamente
            if (_random.NextDouble() < 0.005)
            {
                EnemyBullets.Add(new Bullet
                {
                    X = enemy.X + enemy.Width / 2 - 2,
                    Y = enemy.Y + enemy.Height,
                    SpeedY = 5.0
                });
            }
        }
    }

    private void CheckCollisions()
    {
        // Balas del jugador vs enemigos
        foreach (var bullet in PlayerBullets)
        {
            foreach (var enemy in Enemies)
            {
                if (bullet.CollidesWith(enemy))
                {
                    bullet.IsAlive = false;
                    enemy.IsAlive = false;
                    Player.Score += enemy.Points;
                    Effects.Add(new Effect
                    {
                        X = enemy.X + enemy.Width / 2,
                        Y = enemy.Y + enemy.Height / 2,
                        Size = Math.Max(enemy.Width, enemy.Height) * 1.5,
                        Type = EffectType.Explosion,
                        Duration = 0.5,
                        TimeLeft = 0.5
                    });
                }
            }
        }

        // Balas enemigas vs jugador
        foreach (var bullet in EnemyBullets)
        {
            if (bullet.CollidesWith(Player))
            {
                bullet.IsAlive = false;
                Player.Lives--;
                if (Player.Lives <= 0)
                {
                    Player.IsAlive = false;
                    State = GameState.GameOver;
                    Effects.Add(new Effect
                    {
                        X = Player.X + Player.Width / 2,
                        Y = Player.Y + Player.Height / 2,
                        Size = Math.Max(Player.Width, Player.Height) * 2.0,
                        Type = EffectType.Explosion,
                        Duration = 0.8,
                        TimeLeft = 0.8
                    });
                }
                else
                {
                    Effects.Add(new Effect
                    {
                        X = Player.X + Player.Width / 2,
                        Y = Player.Y + Player.Height / 2,
                        Size = 25,
                        Type = EffectType.Impact,
                        Duration = 0.25,
                        TimeLeft = 0.25
                    });
                }
            }
        }

        // Enemigos vs jugador
        foreach (var enemy in Enemies)
        {
            if (enemy.CollidesWith(Player))
            {
                enemy.IsAlive = false;
                Effects.Add(new Effect
                {
                    X = enemy.X + enemy.Width / 2,
                    Y = enemy.Y + enemy.Height / 2,
                    Size = Math.Max(enemy.Width, enemy.Height) * 1.5,
                    Type = EffectType.Explosion,
                    Duration = 0.5,
                    TimeLeft = 0.5
                });
                Player.Lives--;
                if (Player.Lives <= 0)
                {
                    Player.IsAlive = false;
                    State = GameState.GameOver;
                    Effects.Add(new Effect
                    {
                        X = Player.X + Player.Width / 2,
                        Y = Player.Y + Player.Height / 2,
                        Size = Math.Max(Player.Width, Player.Height) * 2.0,
                        Type = EffectType.Explosion,
                        Duration = 0.8,
                        TimeLeft = 0.8
                    });
                }
                else
                {
                    Effects.Add(new Effect
                    {
                        X = Player.X + Player.Width / 2,
                        Y = Player.Y + Player.Height / 2,
                        Size = 25,
                        Type = EffectType.Impact,
                        Duration = 0.25,
                        TimeLeft = 0.25
                    });
                }
            }
        }
    }

    private void CleanupDead()
    {
        PlayerBullets.RemoveAll(b => !b.IsAlive);
        EnemyBullets.RemoveAll(b => !b.IsAlive);
        Enemies.RemoveAll(e => !e.IsAlive);
    }

    private void UpdateEffects(double deltaTime)
    {
        foreach (var effect in Effects)
        {
            effect.TimeLeft -= deltaTime;
        }
        Effects.RemoveAll(e => e.TimeLeft <= 0);
    }

    private void CheckWaveComplete()
    {
        if (_enemiesSpawnedInWave >= _enemiesPerWave && Enemies.Count == 0)
        {
            Wave++;
            _enemiesSpawnedInWave = 0;
            _enemiesPerWave += 2;
            _enemySpawnInterval = Math.Max(0.4, _enemySpawnInterval - 0.1);
        }
    }
}
