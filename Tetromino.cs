using System;

namespace TetrisWPF
{
    public class Tetromino
    {
        public int[,] Shape { get; set; }
        public int Id { get; set; }   // 用于颜色
        public int Row { get; set; }
        public int Col { get; set; }

        private static readonly int[][,] Shapes = new int[][,]
        {
            new int[,] { {1,1,1,1} },       // I
            new int[,] { {1,1}, {1,1} },   // O
            new int[,] { {0,1,0}, {1,1,1} }, // T
            new int[,] { {1,0,0}, {1,1,1} }, // J
            new int[,] { {0,0,1}, {1,1,1} }, // L
            new int[,] { {1,1,0}, {0,1,1} }, // S
            new int[,] { {0,1,1}, {1,1,0} }  // Z
        };

        public static Tetromino GetRandom()
        {
            Random rnd = new Random();
            int idx = rnd.Next(Shapes.Length);
            return new Tetromino
            {
                Shape = Shapes[idx],
                Id = idx + 1,
                Row = 0,
                Col = 3
            };
        }

        // 顺时针旋转
        public void Rotate()
        {
            int rows = Shape.GetLength(0);
            int cols = Shape.GetLength(1);
            int[,] newShape = new int[cols, rows];

            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                    newShape[c, rows - 1 - r] = Shape[r, c];

            Shape = newShape;
        }
    }
}
