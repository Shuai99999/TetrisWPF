using System;

namespace TetrisWPF
{
    public class Tetromino
    {
        // shape matrix, 0 for empty, 1 for filled
        public int[,] Shape { get; set; }
        // unique identifier for the tetromino type, mostly for color mapping
        public int Id { get; set; }
        // current position on the board
        public int Row { get; set; }
        public int Col { get; set; }

        private static readonly int[][,] Shapes = new int[][,]
        {
            // I, the long bar
            new int[,] { {1,1,1,1} },       
            // O, the square
            new int[,] { {1,1}, {1,1} },   
            // T, the T shape
            new int[,] { {0,1,0}, {1,1,1} }, 
            // J, the J shape
            new int[,] { {1,0,0}, {1,1,1} }, 
            // L, the L shape
            new int[,] { {0,0,1}, {1,1,1} }, 
            // S, the S shape
            new int[,] { {1,1,0}, {0,1,1} }, 
            // Z, the Z shape
            new int[,] { {0,1,1}, {1,1,0} }  
        };

        // Get a random tetromino
        public static Tetromino GetRandom()
        {
            Random rnd = new Random();
            int idx = rnd.Next(Shapes.Length);
            return new Tetromino
            {
                Shape = Shapes[idx],
                Id = idx + 1,
                // Start near the top center of the board
                Row = 0,
                Col = 3
            };
        }

        // rotate the tetromino 90 degrees clockwise
        public void Rotate()
        {
            // get old dimensions
            // GetLength(0) gives number of rows, GetLength(1) gives number of columns
            int rows = Shape.GetLength(0);
            int cols = Shape.GetLength(1);
            // set new dimensions with swapped rows and cols, coz after a clockwise rotation, the rows become cols and the cols become rows
            int[,] newShape = new int[cols, rows];

            // perform rotation
            // use new index to iterate through new shape
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                    // set newShape's value based on old shape's value
                    // new row = old col, new col = (old rows - 1) - old row
                    newShape[c, rows - 1 - r] = Shape[r, c];
            // update shape to new rotated shape
            Shape = newShape;
        }
    }
}
