using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using GalaxyGame.Engine;
using GalaxyGame.Engine.Models;

namespace GalaxyGame;

public partial class MainWindow : Window
{
    private readonly GalaxyGameEngine _engine = new();
    private readonly DispatcherTimer _gameTimer = new();
    private readonly HashSet<Key> _pressedKeys = [];
    private readonly HighScoreManager _highScoreManager = new();
    private DateTime _lastUpdate;
    private string _playerName = "Player";
    private bool _scoreSaved;

    // Brushes reutilizables
    private static readonly Brush PlayerBrush = new SolidColorBrush(Color.FromRgb(160, 180, 200));
    private static readonly Brush PlayerCockpit = new SolidColorBrush(Color.FromRgb(80, 200, 255));
    private static readonly Brush PlayerFuselage = new SolidColorBrush(Color.FromRgb(120, 135, 155));
    private static readonly Brush EnemyBrush = new SolidColorBrush(Color.FromRgb(80, 100, 70));
    private static readonly Brush EnemyCockpit = new SolidColorBrush(Color.FromRgb(200, 200, 60));
    private static readonly Brush EnemyDetail = new SolidColorBrush(Color.FromRgb(60, 75, 50));
    private static readonly Brush PlayerBulletBrush = new SolidColorBrush(Color.FromRgb(0, 255, 150));
    private static readonly Brush EnemyBulletBrush = new SolidColorBrush(Color.FromRgb(255, 100, 50));
    private static readonly Brush WaveBrush = new SolidColorBrush(Color.FromRgb(140, 200, 230));
    private static readonly Brush IslandBrush = new SolidColorBrush(Color.FromRgb(90, 120, 55));
    private static readonly Brush IslandDetailBrush = new SolidColorBrush(Color.FromRgb(110, 145, 65));
    private static readonly Brush ShoreBrush = new SolidColorBrush(Color.FromRgb(210, 190, 140));
    private static readonly Brush FoamBrush = new SolidColorBrush(Color.FromRgb(200, 225, 240));

    public MainWindow()
    {
        InitializeComponent();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        _engine.AreaWidth = GameCanvas.Width;
        _engine.AreaHeight = GameCanvas.Height;

        _gameTimer.Interval = TimeSpan.FromMilliseconds(16); // ~60 FPS
        _gameTimer.Tick += GameLoop;
        _gameTimer.Start();
        _lastUpdate = DateTime.UtcNow;

        Focus();
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        _pressedKeys.Add(e.Key);

        if (e.Key == Key.Escape)
        {
            if (_engine.State == GameState.Playing)
            {
                _engine.State = GameState.Paused;
                _gameTimer.Stop();
                PauseOverlay.Visibility = Visibility.Visible;
            }
            else if (_engine.State == GameState.Paused)
            {
                ResumeGame();
            }
            return;
        }

        if (e.Key == Key.Enter)
        {
            if (_engine.State == GameState.Menu)
            {
                _playerName = string.IsNullOrWhiteSpace(PlayerNameInput.Text) ? "Player" : PlayerNameInput.Text.Trim();
                _scoreSaved = false;
                _engine.StartGame();
                MenuOverlay.Visibility = Visibility.Collapsed;
            }
            else if (_engine.State == GameState.GameOver)
            {
                _scoreSaved = false;
                _engine.StartGame();
                GameOverOverlay.Visibility = Visibility.Collapsed;
            }
        }
    }

    private void Window_KeyUp(object sender, KeyEventArgs e)
    {
        _pressedKeys.Remove(e.Key);
    }

    private void GameLoop(object? sender, EventArgs e)
    {
        var now = DateTime.UtcNow;
        double delta = (now - _lastUpdate).TotalSeconds;
        _lastUpdate = now;

        if (delta > 0.1) delta = 0.1; // Cap para evitar saltos grandes

        bool left = _pressedKeys.Contains(Key.Left) || _pressedKeys.Contains(Key.A);
        bool right = _pressedKeys.Contains(Key.Right) || _pressedKeys.Contains(Key.D);
        bool shoot = _pressedKeys.Contains(Key.Space);

        _engine.Update(delta, left, right, shoot);
        Render();

        // Actualizar HUD
        ScoreText.Text = $"Score: {_engine.Player.Score}";
        LivesText.Text = $"Lives: {_engine.Player.Lives}";
        WaveText.Text = $"Wave: {_engine.Wave}";

        if (_engine.State == GameState.GameOver)
        {
            FinalScoreText.Text = $"Score: {_engine.Player.Score}";

            if (!_scoreSaved)
            {
                _scoreSaved = true;
                var entries = _highScoreManager.Add(_playerName, _engine.Player.Score);
                ShowHighScores(entries);
            }

            GameOverOverlay.Visibility = Visibility.Visible;
        }
    }

    private void ShowHighScores(List<Engine.Models.ScoreEntry> entries)
    {
        var ranked = entries.Select((e, i) => new
        {
            Rank = $"{i + 1}.",
            e.PlayerName,
            e.Score
        }).ToList();
        HighScoresList.ItemsSource = ranked;
    }

    private void ResumeGame()
    {
        _engine.State = GameState.Playing;
        PauseOverlay.Visibility = Visibility.Collapsed;
        _pressedKeys.Clear();
        _lastUpdate = DateTime.UtcNow;
        _gameTimer.Start();
        Focus();
    }

    private void BtnContinuar_Click(object sender, RoutedEventArgs e)
    {
        ResumeGame();
    }

    private void BtnReiniciar_Click(object sender, RoutedEventArgs e)
    {
        PauseOverlay.Visibility = Visibility.Collapsed;
        _scoreSaved = false;
        _pressedKeys.Clear();
        _engine.StartGame();
        _lastUpdate = DateTime.UtcNow;
        _gameTimer.Start();
        Focus();
    }

    private void BtnSalir_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void Render()
    {
        GameCanvas.Children.Clear();

        // Islas (debajo de todo)
        foreach (var island in _engine.Islands)
        {
            DrawIsland(island);
        }

        // Olas / textura del mar
        foreach (var star in _engine.Stars)
        {
            var wave = new Rectangle
            {
                Width = star.Size * 2.5,
                Height = star.Size * 0.8,
                Fill = WaveBrush,
                Opacity = star.Opacity * 0.4,
                RadiusX = 1,
                RadiusY = 1
            };
            Canvas.SetLeft(wave, star.X);
            Canvas.SetTop(wave, star.Y);
            GameCanvas.Children.Add(wave);
        }

        if (_engine.State != GameState.Playing && _engine.State != GameState.GameOver) return;

        // Jugador - Mirage Dagger
        if (_engine.Player.IsAlive)
            DrawPlayer(_engine.Player);

        // Enemigos
        foreach (var enemy in _engine.Enemies)
        {
            DrawEnemy(enemy);
        }

        // Balas del jugador
        foreach (var bullet in _engine.PlayerBullets)
        {
            var rect = new Rectangle
            {
                Width = bullet.Width,
                Height = bullet.Height,
                Fill = PlayerBulletBrush,
                RadiusX = 2,
                RadiusY = 2
            };
            Canvas.SetLeft(rect, bullet.X);
            Canvas.SetTop(rect, bullet.Y);
            GameCanvas.Children.Add(rect);
        }

        // Balas enemigas
        foreach (var bullet in _engine.EnemyBullets)
        {
            var ellipse = new Ellipse
            {
                Width = 6,
                Height = 6,
                Fill = EnemyBulletBrush
            };
            Canvas.SetLeft(ellipse, bullet.X - 1);
            Canvas.SetTop(ellipse, bullet.Y);
            GameCanvas.Children.Add(ellipse);
        }

        // Efectos visuales (explosiones e impactos)
        foreach (var effect in _engine.Effects)
        {
            DrawEffect(effect);
        }
    }

    private void DrawEffect(Effect effect)
    {
        if (effect.Type == EffectType.Explosion)
            DrawExplosion(effect);
        else
            DrawImpact(effect);
    }

    private void DrawExplosion(Effect effect)
    {
        double progress = effect.Progress;
        double maxR = effect.Size;

        // Anillo exterior de fuego (naranja, se expande)
        double outerR = maxR * (0.3 + progress * 0.7);
        double outerOpacity = 1.0 - progress;
        var outer = new Ellipse
        {
            Width = outerR * 2,
            Height = outerR * 2,
            Fill = new RadialGradientBrush(
                Color.FromArgb((byte)(outerOpacity * 200), 255, 200, 0),
                Color.FromArgb((byte)(outerOpacity * 150), 255, 80, 0))
        };
        Canvas.SetLeft(outer, effect.X - outerR);
        Canvas.SetTop(outer, effect.Y - outerR);
        GameCanvas.Children.Add(outer);

        // Núcleo brillante (blanco-amarillo, se encoge)
        if (progress < 0.6)
        {
            double coreR = maxR * 0.4 * (1.0 - progress * 1.2);
            double coreOpacity = 1.0 - progress * 1.5;
            if (coreR > 0 && coreOpacity > 0)
            {
                var core = new Ellipse
                {
                    Width = coreR * 2,
                    Height = coreR * 2,
                    Fill = new RadialGradientBrush(
                        Color.FromArgb((byte)(coreOpacity * 255), 255, 255, 220),
                        Color.FromArgb((byte)(coreOpacity * 200), 255, 255, 100))
                };
                Canvas.SetLeft(core, effect.X - coreR);
                Canvas.SetTop(core, effect.Y - coreR);
                GameCanvas.Children.Add(core);
            }
        }

        // Fragmentos de escombros
        var rng = new Random((int)(effect.Duration * 1000));
        int debrisCount = 8;
        for (int i = 0; i < debrisCount; i++)
        {
            double angle = 2 * Math.PI * i / debrisCount + rng.NextDouble() * 0.5;
            double dist = maxR * progress * (0.5 + rng.NextDouble() * 0.5);
            double dx = Math.Cos(angle) * dist;
            double dy = Math.Sin(angle) * dist;
            double debrisSize = 3 * (1.0 - progress);
            if (debrisSize < 0.5) continue;

            var debris = new Rectangle
            {
                Width = debrisSize,
                Height = debrisSize,
                Fill = new SolidColorBrush(Color.FromArgb(
                    (byte)(outerOpacity * 255), 255, (byte)(100 + rng.Next(100)), 0)),
                RenderTransform = new RotateTransform(rng.Next(360))
            };
            Canvas.SetLeft(debris, effect.X + dx - debrisSize / 2);
            Canvas.SetTop(debris, effect.Y + dy - debrisSize / 2);
            GameCanvas.Children.Add(debris);
        }

        // Anillo de humo (gris, aparece al final)
        if (progress > 0.3)
        {
            double smokeProgress = (progress - 0.3) / 0.7;
            double smokeR = maxR * (0.5 + smokeProgress * 0.6);
            double smokeOpacity = 0.3 * (1.0 - smokeProgress);
            var smoke = new Ellipse
            {
                Width = smokeR * 2,
                Height = smokeR * 2,
                Stroke = new SolidColorBrush(Color.FromArgb(
                    (byte)(smokeOpacity * 255), 80, 80, 80)),
                StrokeThickness = 2,
                Fill = Brushes.Transparent
            };
            Canvas.SetLeft(smoke, effect.X - smokeR);
            Canvas.SetTop(smoke, effect.Y - smokeR);
            GameCanvas.Children.Add(smoke);
        }
    }

    private void DrawImpact(Effect effect)
    {
        double progress = effect.Progress;
        double maxR = effect.Size;

        // Flash blanco central
        double flashR = maxR * (0.5 + progress * 0.5);
        double flashOpacity = 1.0 - progress;
        var flash = new Ellipse
        {
            Width = flashR * 2,
            Height = flashR * 2,
            Fill = new RadialGradientBrush(
                Color.FromArgb((byte)(flashOpacity * 220), 255, 255, 255),
                Color.FromArgb((byte)(flashOpacity * 100), 255, 255, 100))
        };
        Canvas.SetLeft(flash, effect.X - flashR);
        Canvas.SetTop(flash, effect.Y - flashR);
        GameCanvas.Children.Add(flash);

        // Chispas que salen del impacto
        var rng = new Random((int)(effect.X * 100 + effect.Y));
        int sparkCount = 6;
        for (int i = 0; i < sparkCount; i++)
        {
            double angle = 2 * Math.PI * i / sparkCount + rng.NextDouble();
            double dist = maxR * 0.8 * progress;
            double sx = Math.Cos(angle) * dist;
            double sy = Math.Sin(angle) * dist;
            double sparkSize = 2.5 * (1.0 - progress);
            if (sparkSize < 0.3) continue;

            var spark = new Ellipse
            {
                Width = sparkSize,
                Height = sparkSize,
                Fill = new SolidColorBrush(Color.FromArgb(
                    (byte)(flashOpacity * 255), 255, 255, (byte)(150 + rng.Next(105))))
            };
            Canvas.SetLeft(spark, effect.X + sx - sparkSize / 2);
            Canvas.SetTop(spark, effect.Y + sy - sparkSize / 2);
            GameCanvas.Children.Add(spark);
        }
    }

    private void DrawIsland(IslandPatch island)
    {
        var rng = new Random(island.Seed);
        int pointCount = 14;
        double cx = island.Width / 2;
        double cy = island.Height / 2;

        // Generar contorno irregular de la isla
        var coastPoints = new PointCollection();
        for (int i = 0; i < pointCount; i++)
        {
            double angle = 2 * Math.PI * i / pointCount;
            double rx = cx * (0.55 + rng.NextDouble() * 0.45);
            double ry = cy * (0.55 + rng.NextDouble() * 0.45);
            coastPoints.Add(new Point(cx + Math.Cos(angle) * rx, cy + Math.Sin(angle) * ry));
        }

        // Espuma / orilla (un poco más grande)
        var foam = new Polygon
        {
            Points = coastPoints,
            Fill = Brushes.Transparent,
            Stroke = FoamBrush,
            StrokeThickness = 4,
            Opacity = 0.6
        };
        Canvas.SetLeft(foam, island.X);
        Canvas.SetTop(foam, island.Y);
        GameCanvas.Children.Add(foam);

        // Playa / costa (contorno arena)
        var shore = new Polygon
        {
            Points = coastPoints,
            Fill = ShoreBrush
        };
        Canvas.SetLeft(shore, island.X);
        Canvas.SetTop(shore, island.Y);
        GameCanvas.Children.Add(shore);

        // Terreno principal (verde)
        var innerPoints = new PointCollection();
        for (int i = 0; i < coastPoints.Count; i++)
        {
            var p = coastPoints[i];
            double dx = p.X - cx;
            double dy = p.Y - cy;
            innerPoints.Add(new Point(cx + dx * 0.85, cy + dy * 0.85));
        }

        var land = new Polygon
        {
            Points = innerPoints,
            Fill = IslandBrush
        };
        Canvas.SetLeft(land, island.X);
        Canvas.SetTop(land, island.Y);
        GameCanvas.Children.Add(land);

        // Detalle interior (parche de vegetación más clara)
        var detailPoints = new PointCollection();
        double offsetX = (rng.NextDouble() - 0.5) * cx * 0.3;
        double offsetY = (rng.NextDouble() - 0.5) * cy * 0.3;
        for (int i = 0; i < innerPoints.Count; i++)
        {
            var p = innerPoints[i];
            double dx = p.X - cx;
            double dy = p.Y - cy;
            detailPoints.Add(new Point(cx + offsetX + dx * 0.5, cy + offsetY + dy * 0.5));
        }

        var detail = new Polygon
        {
            Points = detailPoints,
            Fill = IslandDetailBrush
        };
        Canvas.SetLeft(detail, island.X);
        Canvas.SetTop(detail, island.Y);
        GameCanvas.Children.Add(detail);
    }

    private void DrawPlayer(Player player)
    {
        double w = player.Width, h = player.Height;

        // Alas delta (triángulo principal)
        var wings = new Polygon
        {
            Points =
            [
                new Point(w * 0.50, 0),       // nariz
                new Point(w, h * 0.80),        // punta ala derecha
                new Point(w * 0.85, h * 0.88), // borde trasero ala derecha
                new Point(w * 0.58, h),        // tobera derecha
                new Point(w * 0.42, h),        // tobera izquierda
                new Point(w * 0.15, h * 0.88), // borde trasero ala izquierda
                new Point(0, h * 0.80)         // punta ala izquierda
            ],
            Fill = PlayerBrush
        };
        Canvas.SetLeft(wings, player.X);
        Canvas.SetTop(wings, player.Y);
        GameCanvas.Children.Add(wings);

        // Fuselaje central (franja oscura)
        var fuselage = new Polygon
        {
            Points =
            [
                new Point(w * 0.50, h * 0.02),
                new Point(w * 0.60, h * 0.50),
                new Point(w * 0.58, h),
                new Point(w * 0.42, h),
                new Point(w * 0.40, h * 0.50)
            ],
            Fill = PlayerFuselage
        };
        Canvas.SetLeft(fuselage, player.X);
        Canvas.SetTop(fuselage, player.Y);
        GameCanvas.Children.Add(fuselage);

        // Cabina (canopy)
        var cockpit = new Polygon
        {
            Points =
            [
                new Point(w * 0.50, h * 0.18),
                new Point(w * 0.55, h * 0.32),
                new Point(w * 0.50, h * 0.40),
                new Point(w * 0.45, h * 0.32)
            ],
            Fill = PlayerCockpit
        };
        Canvas.SetLeft(cockpit, player.X);
        Canvas.SetTop(cockpit, player.Y);
        GameCanvas.Children.Add(cockpit);

        // Llama de escape central
        var flame = new Polygon
        {
            Points =
            [
                new Point(6, 0),
                new Point(0, 12),
                new Point(12, 12)
            ],
            Fill = new SolidColorBrush(Color.FromRgb(255, 165, 0)),
            Opacity = 0.85
        };
        Canvas.SetLeft(flame, player.X + w * 0.50 - 6);
        Canvas.SetTop(flame, player.Y + h);
        GameCanvas.Children.Add(flame);
    }

    private void DrawEnemy(Enemy enemy)
    {
        double w = enemy.Width, h = enemy.Height;

        // Fuselaje + alas (Harrier apuntando hacia abajo)
        var body = new Polygon
        {
            Points =
            [
                new Point(w * 0.38, 0),        // cola izquierda
                new Point(w * 0.62, 0),         // cola derecha
                new Point(w * 0.64, h * 0.15),  // cuerpo superior derecho
                new Point(w * 0.70, h * 0.30),  // raíz ala derecha
                new Point(w, h * 0.50),          // punta ala derecha
                new Point(w * 0.82, h * 0.58),   // borde trasero ala derecha
                new Point(w * 0.62, h * 0.50),   // vuelta ala derecha
                new Point(w * 0.58, h * 0.85),   // cuerpo inferior derecho
                new Point(w * 0.50, h),           // nariz (abajo)
                new Point(w * 0.42, h * 0.85),   // cuerpo inferior izquierdo
                new Point(w * 0.38, h * 0.50),   // vuelta ala izquierda
                new Point(w * 0.18, h * 0.58),   // borde trasero ala izquierda
                new Point(0, h * 0.50),           // punta ala izquierda
                new Point(w * 0.30, h * 0.30),   // raíz ala izquierda
                new Point(w * 0.36, h * 0.15)    // cuerpo superior izquierdo
            ],
            Fill = EnemyBrush
        };
        Canvas.SetLeft(body, enemy.X);
        Canvas.SetTop(body, enemy.Y);
        GameCanvas.Children.Add(body);

        // Franja central del fuselaje
        var detail = new Polygon
        {
            Points =
            [
                new Point(w * 0.44, h * 0.02),
                new Point(w * 0.56, h * 0.02),
                new Point(w * 0.58, h * 0.85),
                new Point(w * 0.50, h * 0.98),
                new Point(w * 0.42, h * 0.85)
            ],
            Fill = EnemyDetail
        };
        Canvas.SetLeft(detail, enemy.X);
        Canvas.SetTop(detail, enemy.Y);
        GameCanvas.Children.Add(detail);

        // Aletas de cola (izquierda)
        var tailL = new Polygon
        {
            Points =
            [
                new Point(w * 0.38, 0),
                new Point(w * 0.22, h * 0.05),
                new Point(w * 0.32, h * 0.10)
            ],
            Fill = EnemyBrush
        };
        Canvas.SetLeft(tailL, enemy.X);
        Canvas.SetTop(tailL, enemy.Y);
        GameCanvas.Children.Add(tailL);

        // Aletas de cola (derecha)
        var tailR = new Polygon
        {
            Points =
            [
                new Point(w * 0.62, 0),
                new Point(w * 0.78, h * 0.05),
                new Point(w * 0.68, h * 0.10)
            ],
            Fill = EnemyBrush
        };
        Canvas.SetLeft(tailR, enemy.X);
        Canvas.SetTop(tailR, enemy.Y);
        GameCanvas.Children.Add(tailR);

        // Cabina del piloto
        var cockpit = new Ellipse
        {
            Width = 5,
            Height = 5,
            Fill = EnemyCockpit
        };
        Canvas.SetLeft(cockpit, enemy.X + w * 0.50 - 2.5);
        Canvas.SetTop(cockpit, enemy.Y + h * 0.62);
        GameCanvas.Children.Add(cockpit);
    }
}