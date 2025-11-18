using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Windows.Media.Effects;
using Microsoft.EntityFrameworkCore;  // EF Core 引用
using System.Linq;                     // LINQ 查询

namespace TetrisWPF
{
    public partial class MainWindow : Window
    {
        private const int rows = 20;
        private const int cols = 10;
        private const int cellSize = 24;

        private int[,] grid = new int[rows, cols];

        private Tetromino current;
        private Tetromino next;
        private int score = 0;
        private DispatcherTimer timer;
        private string playerName;
        private bool isPaused = false;
        private User currentUser;  // 数据库中的当前用户对象

        public MainWindow()
        {
            InitializeComponent();

            // 输入玩家名字
            playerName = Microsoft.VisualBasic.Interaction.InputBox("Enter your name(No more than 20 Characters):", "Player Name", "Player");

            // 用户点击 Cancel 或输入空字符串
            if (string.IsNullOrWhiteSpace(playerName))
            {
                // 关闭游戏窗口
                this.Close();
                return; // 退出构造函数
            }

            if (!string.IsNullOrEmpty(playerName) && playerName.Length > 20)
                playerName = playerName.Substring(0, 20);

            // ===================== 数据库操作 =====================
            using (var context = new TetrisContext())
            {
                // 查询是否已有该用户
                currentUser = context.Users.FirstOrDefault(u => u.Name == playerName);
                if (currentUser == null)
                {
                    // 新用户，创建记录
                    currentUser = new User
                    {
                        Name = playerName,
                        CreatedDate = DateTime.Now
                    };
                    context.Users.Add(currentUser);
                    context.SaveChanges();
                }
            }
            // ========================================================

            // 设置右侧面板显示用户名
            PlayerNameText.Text = $"Player: {playerName}";

            // 设置背景
            //GameCanvas.Background = new LinearGradientBrush(Color.FromRgb(20, 20, 40), Color.FromRgb(0, 0, 0), 90);
            //NextCanvas.Background = new LinearGradientBrush(Color.FromRgb(50, 50, 50), Color.FromRgb(30, 30, 30), 90);
            //GameCanvas.Background = Brushes.White;
            //NextCanvas.Background = Brushes.White;
            GameCanvas.Background = new LinearGradientBrush(Color.FromRgb(245, 245, 245), Color.FromRgb(220, 220, 220), 90);
            NextCanvas.Background = new LinearGradientBrush(Color.FromRgb(245, 245, 245), Color.FromRgb(220, 220, 220), 90);

            DrawingBrush gridBrush = new DrawingBrush
            {
                TileMode = TileMode.Tile, // 重复平铺
                Viewport = new Rect(0, 0, cellSize, cellSize), // 每个格子大小
                ViewportUnits = BrushMappingMode.Absolute,
                Drawing = new GeometryDrawing
                {
                    Brush = Brushes.Transparent, // 背景透明
                    Pen = new Pen(
                    new SolidColorBrush(Color.FromArgb(50, 0, 0, 0)), // 浅灰线，透明度50
                    1 // 线宽
                ),
                    Geometry = new RectangleGeometry(new Rect(0, 0, cellSize, cellSize))
                }
            };

            // 设置 Canvas 背景为格子
            GameCanvas.Background = gridBrush;

            // 初始化方块
            current = Tetromino.GetRandom();
            next = Tetromino.GetRandom();
            DrawNext();

            // 初始化计时器
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(500);
            timer.Tick += Timer_Tick;
            timer.Start();

            DrawGrid();
            UpdateScore();

            // 处理键盘事件
            this.KeyDown += MainWindow_KeyDown;

        }

        private void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            TogglePause();
        }

        private void TogglePause()
        {
            if (!isPaused)
            {
                timer.Stop();
                isPaused = true;
                PauseButton.Content = "Resume";
            }
            else
            {
                timer.Start();
                isPaused = false;
                PauseButton.Content = "Pause";
            }
        }

        private void RestartButton_Click(object sender, RoutedEventArgs e)
        {
            RestartGame(true);
        }

        // 核心重置方法
        private void RestartGame(bool requireConfirmation = true)
        {
            // 暂停游戏，防止方块下落
            bool wasRunning = !isPaused;
            if (wasRunning)
                timer.Stop();

            // 可选的确认对话框
            if (requireConfirmation)
            {
                var result = MessageBox.Show(
                    "Are you sure you want to restart the game? Current progress will be lost.",
                    "Confirm Restart",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                {
                    // 用户点击 No，恢复计时器状态
                    if (wasRunning)
                        timer.Start();
                    return;
                }
            }

            // 清空棋盘
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                    grid[r, c] = 0;

            // 重置分数
            score = 0;
            UpdateScore();

            // 重置当前方块和下一块
            current = Tetromino.GetRandom();
            next = Tetromino.GetRandom();
            DrawNext();

            // 清空 Canvas 并绘制初始状态
            GameCanvas.Children.Clear();
            DrawGrid();

            // 恢复计时器
            if (wasRunning)
                timer.Start();
        }



        private void Timer_Tick(object sender, EventArgs e)
        {
            MoveDown();
        }

        private void MoveDown()
        {
            if (CanMove(current, current.Row + 1, current.Col))
            {
                current.Row++;
            }
            else
            {
                FixToGrid();
                ClearLines();
                current = next;
                next = Tetromino.GetRandom();
                DrawNext();

                if (!CanMove(current, current.Row, current.Col))
                {
                    GameOver();
                }
            }
            DrawGrid();
        }

        // 抽象出的游戏结束方法
        private void GameOver()
        {
            timer.Stop();

            SaveScore();

            using (var context = new TetrisContext())
            {
                var topScores = context.Scores
                    .OrderByDescending(s => s.PlayedScore)
                    .Take(5)
                    .Join(context.Users,
                          s => s.UserId,
                          u => u.Id,
                          (s, u) => new { u.Name, s.PlayedScore, s.ScoreDate })
                    .ToList();

                // 检查当前成绩是否在前5
                bool inTop5 = topScores.Any(s => s.Name == playerName && s.PlayedScore == score);

                string message = $"Game Over!\nYour Score: {score}\n\nTop Scores:\n";
                foreach (var entry in topScores)
                {
                    if (entry.Name == playerName && entry.PlayedScore == score)
                        message += $">> {entry.Name} - {entry.PlayedScore} - {entry.ScoreDate:g} <<\n"; // 用 >> << 标记
                    else
                        message += $"{entry.Name} - {entry.PlayedScore} - {entry.ScoreDate:g}\n";
                }

                // 如果不在前5，则在最后加上
                if (!inTop5)
                {
                    message += $"\nYour Score: >> {playerName} - {score} <<\n";
                }

                MessageBoxResult result = MessageBox.Show(
                    message + "\n\nDo you want to restart the game?",
                    "Game Over",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Information);

                if (result == MessageBoxResult.Yes)
                    RestartGame(false);
                else
                    this.Close();
            }
        }


        private void FixToGrid()
        {
            for (int r = 0; r < current.Shape.GetLength(0); r++)
                for (int c = 0; c < current.Shape.GetLength(1); c++)
                    if (current.Shape[r, c] != 0)
                        grid[current.Row + r, current.Col + c] = current.Id;
        }

        private void ClearLines()
        {
            for (int r = rows - 1; r >= 0; r--)
            {
                bool full = true;
                for (int c = 0; c < cols; c++)
                    if (grid[r, c] == 0) { full = false; break; }

                if (full)
                {
                    score += 100;
                    UpdateScore();
                    // 清除行
                    for (int i = r; i > 0; i--)
                        for (int c = 0; c < cols; c++)
                            grid[i, c] = grid[i - 1, c];
                    for (int c = 0; c < cols; c++)
                        grid[0, c] = 0;
                    r++; // 再检查当前行
                }
            }
        }

        private void UpdateScore()
        {
            ScoreText.Text = score.ToString();
        }

        private void DrawGrid()
        {
            GameCanvas.Children.Clear();

            // 绘制固定方块
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                    if (grid[r, c] != 0)
                        DrawRectAt(r, c, grid[r, c]);

            // 绘制当前方块
            for (int r = 0; r < current.Shape.GetLength(0); r++)
                for (int c = 0; c < current.Shape.GetLength(1); c++)
                    if (current.Shape[r, c] != 0)
                        DrawRectAt(current.Row + r, current.Col + c, current.Id);
        }

        private void DrawRectAt(int row, int col, int id)
        {
            Rectangle rect = new Rectangle
            {
                Width = cellSize - 1,
                Height = cellSize - 1,
                Fill = GetBrushById(id),
                Stroke = Brushes.Black,
                StrokeThickness = 1,
                Effect = new DropShadowEffect
                {
                    Color = Colors.Black,
                    Direction = 320,
                    ShadowDepth = 2,
                    Opacity = 0.4,
                    BlurRadius = 3
                }
            };
            Canvas.SetLeft(rect, col * cellSize);
            Canvas.SetTop(rect, row * cellSize);
            GameCanvas.Children.Add(rect);
        }

        private void DrawNext()
        {
            NextCanvas.Children.Clear();
            for (int r = 0; r < next.Shape.GetLength(0); r++)
                for (int c = 0; c < next.Shape.GetLength(1); c++)
                    if (next.Shape[r, c] != 0)
                    {
                        Rectangle rect = new Rectangle
                        {
                            Width = cellSize - 2,
                            Height = cellSize - 2,
                            Fill = GetBrushById(next.Id),
                            Stroke = Brushes.Black,
                            StrokeThickness = 1,
                            Effect = new DropShadowEffect
                            {
                                Color = Colors.Black,
                                Direction = 320,
                                ShadowDepth = 2,
                                Opacity = 0.4,
                                BlurRadius = 3
                            }
                        };
                        Canvas.SetLeft(rect, c * cellSize);
                        Canvas.SetTop(rect, r * cellSize);
                        NextCanvas.Children.Add(rect);
                    }
        }

        private Brush GetBrushById(int id)
        {
            GradientStopCollection stops;
            switch (id % 7)
            {
                case 0: stops = new GradientStopCollection { new GradientStop(Color.FromRgb(0, 255, 255), 0), new GradientStop(Color.FromRgb(0, 200, 200), 1) }; break;
                case 1: stops = new GradientStopCollection { new GradientStop(Color.FromRgb(255, 255, 0), 0), new GradientStop(Color.FromRgb(200, 200, 0), 1) }; break;
                case 2: stops = new GradientStopCollection { new GradientStop(Color.FromRgb(128, 0, 128), 0), new GradientStop(Color.FromRgb(180, 100, 180), 1) }; break;
                case 3: stops = new GradientStopCollection { new GradientStop(Color.FromRgb(0, 0, 255), 0), new GradientStop(Color.FromRgb(0, 0, 200), 1) }; break;
                case 4: stops = new GradientStopCollection { new GradientStop(Color.FromRgb(255, 165, 0), 0), new GradientStop(Color.FromRgb(200, 120, 0), 1) }; break;
                case 5: stops = new GradientStopCollection { new GradientStop(Color.FromRgb(0, 128, 0), 0), new GradientStop(Color.FromRgb(0, 180, 0), 1) }; break;
                case 6: stops = new GradientStopCollection { new GradientStop(Color.FromRgb(255, 0, 0), 0), new GradientStop(Color.FromRgb(200, 0, 0), 1) }; break;
                default: stops = new GradientStopCollection { new GradientStop(Colors.Gray, 0), new GradientStop(Colors.DarkGray, 1) }; break;
            }
            return new LinearGradientBrush(stops, new Point(0, 0), new Point(1, 1));
        }

        private bool CanMove(Tetromino t, int newRow, int newCol)
        {
            for (int r = 0; r < t.Shape.GetLength(0); r++)
                for (int c = 0; c < t.Shape.GetLength(1); c++)
                    if (t.Shape[r, c] != 0)
                    {
                        int x = newCol + c;
                        int y = newRow + r;
                        if (x < 0 || x >= cols || y < 0 || y >= rows)
                            return false;
                        if (grid[y, x] != 0)
                            return false;
                    }
            return true;
        }

        private void MainWindow_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            switch (e.Key)
            {
                case System.Windows.Input.Key.Left:
                    if (CanMove(current, current.Row, current.Col - 1)) current.Col--;
                    break;
                case System.Windows.Input.Key.Right:
                    if (CanMove(current, current.Row, current.Col + 1)) current.Col++;
                    break;
                case System.Windows.Input.Key.Down:
                    MoveDown();
                    break;
                case System.Windows.Input.Key.Up:
                    current.Rotate();
                    if (!CanMove(current, current.Row, current.Col))
                        current.Rotate(); current.Rotate(); current.Rotate(); // 逆时针回退
                    break;
            }
            DrawGrid();
        }

        private void SaveScore()
        {
            using (var context = new TetrisContext())
            {
                var user = context.Users.FirstOrDefault(u => u.Name == playerName);
                if (user != null)
                {
                    var scoreEntry = new Score
                    {
                        UserId = user.Id,
                        PlayedScore = score,
                        ScoreDate = DateTime.Now
                    };
                    context.Scores.Add(scoreEntry);
                    context.SaveChanges();
                }
            }

            // 可选：保存本地文本备份
            //File.AppendAllText("scores.txt", $"{playerName}: {score}\n");
        }
    }
}
