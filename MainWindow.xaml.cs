using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Windows.Media.Effects;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace TetrisWPF
{
    public partial class MainWindow : Window
    {
        // game grid dimensions
        private const int rows = 20;
        private const int cols = 10;

        // size of each cell in pixels
        private const int cellSize = 24;

        // the game grid, 0 means empty, other numbers represent different tetromino IDs
        private int[,] grid = new int[rows, cols];

        // current and next tetrominoes
        private Tetromino current;
        private Tetromino next;

        // current score
        private int score = 0;

        // game timer
        private DispatcherTimer timer;

        // current player name
        private string playerName;

        // pause state
        private bool isPaused = false;

        // current user object
        private User currentUser;

        public MainWindow()
        {
            InitializeComponent();

            // User input for player name
            playerName = Microsoft.VisualBasic.Interaction.InputBox("Enter your name(No more than 20 Characters):", "Player Name", "Player");

            // If user cancels or inputs empty name, close the game
            if (string.IsNullOrWhiteSpace(playerName))
            {
                // close the window
                this.Close();
                return; // exit constructor
            }

            if (!string.IsNullOrEmpty(playerName) && playerName.Length > 20)
                playerName = playerName.Substring(0, 20);

            // database part: check or create user
            using (var context = new TetrisContext())
            {
                // query if the user exists in the database
                currentUser = context.Users.FirstOrDefault(u => u.Name == playerName);
                if (currentUser == null)
                {
                    // it's a new user, create one
                    currentUser = new User
                    {
                        Name = playerName,
                        CreatedDate = DateTime.Now
                    };
                    context.Users.Add(currentUser);
                    context.SaveChanges();
                }
            }

            // display username on the right panel
            PlayerNameText.Text = $"Player: {playerName}";

            // set backgrounds
            // we tried dark themes but they were not visually appealing
            //GameCanvas.Background = new LinearGradientBrush(Color.FromRgb(20, 20, 40), Color.FromRgb(0, 0, 0), 90);
            //NextCanvas.Background = new LinearGradientBrush(Color.FromRgb(50, 50, 50), Color.FromRgb(30, 30, 30), 90);
            // this theme looks too plain
            //GameCanvas.Background = Brushes.White;
            //NextCanvas.Background = Brushes.White;
            // finally we settled on a subtle gradient
            GameCanvas.Background = new LinearGradientBrush(Color.FromRgb(245, 245, 245), Color.FromRgb(220, 220, 220), 90);
            NextCanvas.Background = new LinearGradientBrush(Color.FromRgb(245, 245, 245), Color.FromRgb(220, 220, 220), 90);

            // We want a grid pattern on the canvas as background, so the player can better see the cells, improve game experience
            DrawingBrush gridBrush = new DrawingBrush
            {
                // tile the pattern, repeatly fill the whole area
                TileMode = TileMode.Tile,
                // set tile size for every cell, 0, 0 stands for top-left corner, the start point of the tile
                Viewport = new Rect(0, 0, cellSize, cellSize),
                // use absolute units for the tile size, that being said, we use pixels as the unit here
                ViewportUnits = BrushMappingMode.Absolute,
                // define the actual drawing for each tile
                Drawing = new GeometryDrawing
                {
                    // transparent fill for the cell
                    Brush = Brushes.Transparent,
                    Pen = new Pen(
                    // semi-transparent black lines with low opacity
                    new SolidColorBrush(Color.FromArgb(50, 0, 0, 0)),
                    // line thickness
                    1
                ),
                    // define a rectangle geometry for the tile
                    Geometry = new RectangleGeometry(new Rect(0, 0, cellSize, cellSize))
                }
            };

            // set the DrawingBrush as the background of the GameCanvas
            GameCanvas.Background = gridBrush;

            // initialize current and next tetrominoes
            current = Tetromino.GetRandom();
            next = Tetromino.GetRandom();
            // draw the next tetromino on the NextCanvas
            DrawNext();

            // initialize and start the timer
            timer = new DispatcherTimer();
            // set interval to 500 milliseconds
            timer.Interval = TimeSpan.FromMilliseconds(500);
            // bind the Tick event to the Timer_Tick method, Tick event fires every interval, it acutually calls the move down method
            timer.Tick += Timer_Tick;
            // start the timer
            timer.Start();

            // initial draw and score update
            DrawGrid();
            UpdateScore();

            // handle key down events for controlling the tetrominoes
            this.KeyDown += MainWindow_KeyDown;

        }

        // Pause/Resume button click event handler
        private void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            TogglePause();
        }

        // Pause/Resume toggle method
        private void TogglePause()
        {
            // toggle the isPaused flag and update timer and button content accordingly
            if (!isPaused)
            {
                // pause the game
                timer.Stop();
                // set the flag
                isPaused = true;
                // update button content
                PauseButton.Content = "Resume";
            }
            // vise versa
            else
            {
                timer.Start();
                isPaused = false;
                PauseButton.Content = "Pause";
            }
        }

        // Restart button click event handler
        private void RestartButton_Click(object sender, RoutedEventArgs e)
        {
            RestartGame(true);
        }

        // Restart game method with optional confirmation
        private void RestartGame(bool requireConfirmation = true)
        {
            // pause the timer if the game is running
            bool wasRunning = !isPaused;
            if (wasRunning)
                timer.Stop();

            // ask for confirmation if required
            if (requireConfirmation)
            {
                var result = MessageBox.Show(
                    "Are you sure you want to restart the game? Current progress will be lost.",
                    "Confirm Restart",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                {
                    // resume the timer if the user cancels the restart
                    if (wasRunning)
                        timer.Start();
                    return;
                }
            }

            // reset the canvas grid
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                    grid[r, c] = 0;

            // reset score to zero
            score = 0;
            UpdateScore();

            // reset current and next tetrominoes
            current = Tetromino.GetRandom();
            next = Tetromino.GetRandom();
            DrawNext();

            // clear and redraw the game canvas
            GameCanvas.Children.Clear();
            DrawGrid();

            // reset timer and resume if it was running
            if (wasRunning)
                timer.Start();
        }


        // the timer tick event handler
        private void Timer_Tick(object sender, EventArgs e)
        {
            // let the current tetromino move down
            MoveDown();
        }

        // move the current tetromino down by one row
        private void MoveDown()
        {
            // check if it can move down
            if (CanMove(current, current.Row + 1, current.Col))
            {
                // move down
                current.Row++;
            }
            // cannot move down, fix it to the grid
            else
            {
                // fix to grid
                FixToGrid();
                // clear full lines if any
                ClearLines();
                // spawn a new tetromino from next
                current = next;
                // generate a new next tetromino
                next = Tetromino.GetRandom();
                // draw the next tetromino on the NextCanvas
                DrawNext();

                // check if the new tetromino can be placed, if not, game over
                if (!CanMove(current, current.Row, current.Col))
                {
                    GameOver();
                }
            }
            // redraw the grid after movement
            DrawGrid();
        }

        // the game over method
        private void GameOver()
        {
            // stop the timer
            timer.Stop();

            // save the score to database
            SaveScore();

            // retrieve top 5 scores from database
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

                // check if current score is in top 5
                bool inTop5 = topScores.Any(s => s.Name == playerName && s.PlayedScore == score);

                // build the game over message
                string message = $"Game Over!\nYour Score: {score}\n\nTop Scores:\n";

                // highlight the player's score if in top 5
                foreach (var entry in topScores)
                {
                    if (entry.Name == playerName && entry.PlayedScore == score)
                        // use >> << to highlight the current player's score
                        message += $">> {entry.Name} - {entry.PlayedScore} - {entry.ScoreDate:g} <<\n";
                    else
                        message += $"{entry.Name} - {entry.PlayedScore} - {entry.ScoreDate:g}\n";
                }

                // if not in top 5, also show the player's score at the end
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

        // fix the current tetromino to the grid, called by MoveDown when it cannot move down further
        private void FixToGrid()
        {
            // iterate through the shape of the current tetromino
            for (int r = 0; r < current.Shape.GetLength(0); r++)
                for (int c = 0; c < current.Shape.GetLength(1); c++)
                    // if the cell is not empty, fix it to the grid
                    if (current.Shape[r, c] != 0)
                        // give the grid cell the tetromino ID, if the cell id is not zero, it means occupied(refer to CanMove method)
                        grid[current.Row + r, current.Col + c] = current.Id;
        }

        // clear full lines from the grid and update score, triggered after every tetromino is fixed
        private void ClearLines()
        {
            // check each row from bottom to top
            // in each loop, r stands for the current row being checked
            for (int r = rows - 1; r >= 0; r--)
            {
                // set a variable to track if the row is full
                bool full = true;
                // check each column in the row
                for (int c = 0; c < cols; c++)
                    // if any cell is empty(grid[r, c] == 0), the row is not full
                    if (grid[r, c] == 0) { full = false; break; }

                // otherwise, the row is full
                if (full)
                {
                    // increase score
                    score += 100;
                    // update score display
                    UpdateScore();
                    // clear the row and move everything above it down by one row
                    // shift down all rows above r
                    for (int i = r; i > 0; i--)
                        for (int c = 0; c < cols; c++)
                            // use the row above to fill the current row
                            grid[i, c] = grid[i - 1, c];
                    // clear the top row
                    for (int c = 0; c < cols; c++)
                        grid[0, c] = 0;
                    // check the same row index again since rows have shifted down
                    r++;
                }
            }
        }

        // update the score display
        private void UpdateScore()
        {
            ScoreText.Text = score.ToString();
        }

        // draw the entire grid including fixed blocks and current falling block
        private void DrawGrid()
        {
            // clear the canvas first
            // because WPF cannot directly update existing rectangles easily
            // so we redraw everything each time when the grid changes such as block moves or lines are cleared
            GameCanvas.Children.Clear();

            // draw fixed blocks, i.e., the blocks already fixed on the grid
            // iterate through the grid array
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                    // if the cell is not empty, draw a rectangle
                    if (grid[r, c] != 0)
                        // draw rectangle at the specified row and column with the corresponding tetromino ID for color
                        DrawRectAt(r, c, grid[r, c]);

            // draw current falling block
            // current.Shape.GetLength(0) gives the number of rows in the tetromino shape
            // current.Shape.GetLength(1) gives the number of columns in the tetromino shape
            for (int r = 0; r < current.Shape.GetLength(0); r++)
                for (int c = 0; c < current.Shape.GetLength(1); c++)
                    // if the cell in the tetromino shape is not empty, draw it
                    if (current.Shape[r, c] != 0)
                        DrawRectAt(current.Row + r, current.Col + c, current.Id);
        }

        // draw a single rectangle at the specified row and column with the given tetromino ID for color
        private void DrawRectAt(int row, int col, int id)
        {
            // create a rectangle shape with size cellSize and color based on tetromino ID
            Rectangle rect = new Rectangle
            {
                // -1 to create a small gap between cells for better visual
                Width = cellSize - 1,
                Height = cellSize - 1,
                // set fill color using a helper method
                Fill = GetBrushById(id),
                // add a black border
                Stroke = Brushes.Black,
                // set the border thickness
                StrokeThickness = 1,
                // give it a slight shadow effect for better visual
                Effect = new DropShadowEffect
                {
                    Color = Colors.Black,
                    Direction = 320,
                    ShadowDepth = 2,
                    Opacity = 0.4,
                    BlurRadius = 3
                }
            };
            // position the rectangle on the canvas
            Canvas.SetLeft(rect, col * cellSize);
            Canvas.SetTop(rect, row * cellSize);
            // add the rectangle to the canvas
            GameCanvas.Children.Add(rect);
        }

        // draw the next tetromino in the NextCanvas panel
        private void DrawNext()
        {
            // clear the NextCanvas first
            NextCanvas.Children.Clear();
            // draw the next tetromino
            // go through the shape array of the next tetromino
            for (int r = 0; r < next.Shape.GetLength(0); r++)
                for (int c = 0; c < next.Shape.GetLength(1); c++)
                    // if the cell is not empty, draw a rectangle
                    if (next.Shape[r, c] != 0)
                    {
                        // create a rectangle shape with size cellSize and color based on tetromino ID
                        Rectangle rect = new Rectangle
                        {
                            // -2 to create a small gap between cells for better visual
                            Width = cellSize - 2,
                            Height = cellSize - 2,
                            // set fill color using a helper method
                            Fill = GetBrushById(next.Id),
                            // add a black border
                            Stroke = Brushes.Black,
                            // set the border thickness
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
                        // position the rectangle on the NextCanvas
                        Canvas.SetLeft(rect, c * cellSize);
                        Canvas.SetTop(rect, r * cellSize);
                        // add the rectangle to the NextCanvas
                        NextCanvas.Children.Add(rect);
                    }
        }

        // get a brush (color) based on the tetromino ID
        private Brush GetBrushById(int id)
        {
            // define different gradient colors for different tetromino IDs
            // GradientStopCollection is a class in WPF that represents a collection of GradientStop objects
            // 0 to 1 represents the start and end points of the gradient with corresponding colors
            GradientStopCollection stops;
            switch (id % 7)
            {
                // case 0 stands for I shape with cyan color
                case 0: stops = new GradientStopCollection { new GradientStop(Color.FromRgb(0, 255, 255), 0), new GradientStop(Color.FromRgb(0, 200, 200), 1) }; break;
                // case 1 stands for O shape with yellow color
                case 1: stops = new GradientStopCollection { new GradientStop(Color.FromRgb(255, 255, 0), 0), new GradientStop(Color.FromRgb(200, 200, 0), 1) }; break;
                // case 2 stands for T shape with purple color
                case 2: stops = new GradientStopCollection { new GradientStop(Color.FromRgb(128, 0, 128), 0), new GradientStop(Color.FromRgb(180, 100, 180), 1) }; break;
                // case 3 stands for J shape with blue color
                case 3: stops = new GradientStopCollection { new GradientStop(Color.FromRgb(0, 0, 255), 0), new GradientStop(Color.FromRgb(0, 0, 200), 1) }; break;
                // case 4 stands for L shape with orange color
                case 4: stops = new GradientStopCollection { new GradientStop(Color.FromRgb(255, 165, 0), 0), new GradientStop(Color.FromRgb(200, 120, 0), 1) }; break;
                // case 5 stands for S shape with green color
                case 5: stops = new GradientStopCollection { new GradientStop(Color.FromRgb(0, 128, 0), 0), new GradientStop(Color.FromRgb(0, 180, 0), 1) }; break;
                // case 6 stands for Z shape with red color
                case 6: stops = new GradientStopCollection { new GradientStop(Color.FromRgb(255, 0, 0), 0), new GradientStop(Color.FromRgb(200, 0, 0), 1) }; break;
                // default case, should not happen
                default: stops = new GradientStopCollection { new GradientStop(Colors.Gray, 0), new GradientStop(Colors.DarkGray, 1) }; break;
            }
            return new LinearGradientBrush(stops, new Point(0, 0), new Point(1, 1));
        }

        // check if the tetromino can move to the specified row and column
        private bool CanMove(Tetromino t, int newRow, int newCol)
        {
            // check each cell in the tetromino shape
            for (int r = 0; r < t.Shape.GetLength(0); r++)
                for (int c = 0; c < t.Shape.GetLength(1); c++)
                    // if the cell is not empty in the tetromino shape
                    if (t.Shape[r, c] != 0)
                    {
                        // calculate the target position on the grid
                        int x = newCol + c;
                        int y = newRow + r;
                        // check bounds and collision with existing blocks
                        if (x < 0 || x >= cols || y < 0 || y >= rows)
                            return false;
                        // check if the target cell is already occupied
                        if (grid[y, x] != 0)
                            return false;
                    }
            return true;
        }

        // handle key down events for controlling the tetrominoes
        private void MainWindow_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            // tetrismino control logic, use arrow keys, move left, right, down, rotate(up arrow)
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
                    // if rotation causes collision with other blocks or out of bounds, rotate back
                    if (!CanMove(current, current.Row, current.Col))
                        // use 3 more rotations to revert back to original orientation
                        current.Rotate(); current.Rotate(); current.Rotate();
                    break;
            }
            // redraw the grid after movement
            DrawGrid();
        }

        // save the current score to the database
        private void SaveScore()
        {
            using (var context = new TetrisContext())
            {
                // find the user by name in the database
                var user = context.Users.FirstOrDefault(u => u.Name == playerName);
                if (user != null)
                {
                    var scoreEntry = new Score
                    {
                        UserId = user.Id,
                        PlayedScore = score,
                        ScoreDate = DateTime.Now
                    };
                    // add the score entry to the Scores table
                    context.Scores.Add(scoreEntry);
                    // save changes to the database
                    context.SaveChanges();
                }
            }

            // Alternative: save to a local text file (not used in this version)
            // File.AppendAllText("scores.txt", $"{playerName}: {score}\n");
        }
    }
}
