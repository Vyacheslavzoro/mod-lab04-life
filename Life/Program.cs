using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;

namespace cli_life
{
    public class Cell
    {
        public bool IsAlive;
        public readonly List<Cell> neighbors = new List<Cell>();
        private bool IsAliveNext;

        public void DetermineNextLiveState()
        {
            int liveNeighbors = neighbors.Count(x => x.IsAlive);
            IsAliveNext = IsAlive ? (liveNeighbors == 2 || liveNeighbors == 3) : (liveNeighbors == 3);
        }

        public void Advance()
        {
            IsAlive = IsAliveNext;
        }
    }

    public class Board
    {
        public readonly Cell[,] Cells;
        public readonly int CellSize;
        public int Columns => Cells.GetLength(0);
        public int Rows => Cells.GetLength(1);
        public int Width => Columns * CellSize;
        public int Height => Rows * CellSize;

        private readonly Random rand = new Random();

        public Board(int width, int height, int cellSize)
        {
            CellSize = cellSize;
            Cells = new Cell[width / cellSize, height / cellSize];
            for (int x = 0; x < Columns; x++)
                for (int y = 0; y < Rows; y++)
                    Cells[x, y] = new Cell();

            ConnectNeighbors();
        }

        public void Randomize(double liveDensity)
        {
            foreach (var cell in Cells)
                cell.IsAlive = rand.NextDouble() < liveDensity;
        }

        public void Advance()
        {
            foreach (var cell in Cells)
                cell.DetermineNextLiveState();
            foreach (var cell in Cells)
                cell.Advance();
        }

        public void Save(string filePath)
        {
            var lines = new List<string>();
            for (int y = 0; y < Rows; y++)
            {
                var line = new char[Columns];
                for (int x = 0; x < Columns; x++)
                    line[x] = Cells[x, y].IsAlive ? '*' : ' ';
                lines.Add(new string(line));
            }
            File.WriteAllLines(filePath, lines);
        }

        public void Load(string filePath)
        {
            var lines = File.ReadAllLines(filePath);
            for (int y = 0; y < Math.Min(lines.Length, Rows); y++)
            {
                for (int x = 0; x < Math.Min(lines[y].Length, Columns); x++)
                {
                    Cells[x, y].IsAlive = lines[y][x] == '*';
                }
            }
        }

        private void ConnectNeighbors()
        {
            for (int x = 0; x < Columns; x++)
            {
                for (int y = 0; y < Rows; y++)
                {
                    int xL = (x > 0) ? x - 1 : Columns - 1;
                    int xR = (x < Columns - 1) ? x + 1 : 0;
                    int yT = (y > 0) ? y - 1 : Rows - 1;
                    int yB = (y < Rows - 1) ? y + 1 : 0;

                    Cells[x, y].neighbors.Add(Cells[xL, yT]);
                    Cells[x, y].neighbors.Add(Cells[x, yT]);
                    Cells[x, y].neighbors.Add(Cells[xR, yT]);
                    Cells[x, y].neighbors.Add(Cells[xL, y]);
                    Cells[x, y].neighbors.Add(Cells[xR, y]);
                    Cells[x, y].neighbors.Add(Cells[xL, yB]);
                    Cells[x, y].neighbors.Add(Cells[x, yB]);
                    Cells[x, y].neighbors.Add(Cells[xR, yB]);
                }
            }
        }
    }

    public class Config
    {
        public int Width { get; set; } = 50;
        public int Height { get; set; } = 20;
        public int CellSize { get; set; } = 1;
        public double LiveDensity { get; set; } = 0.3;
        public int Delay { get; set; } = 100;
        public string? LoadFile { get; set; }
    }

    class Program
    {
        static Board board;
        static Config config;

        static void LoadConfig()
        {
            string configPath = "config.json";
            if (File.Exists(configPath))
            {
                var json = File.ReadAllText(configPath);
                config = JsonSerializer.Deserialize<Config>(json) ?? new Config();
            }
            else
            {
                config = new Config();
            }
        }

        static void InitializeBoard()
        {
            board = new Board(config.Width, config.Height, config.CellSize);
            if (!string.IsNullOrEmpty(config.LoadFile) && File.Exists(config.LoadFile))
            {
                board.Load(config.LoadFile);
            }
            else
            {
                board.Randomize(config.LiveDensity);
            }
        }

        static void Render()
        {
            Console.SetCursorPosition(0, 0);
            for (int row = 0; row < board.Rows; row++)
            {
                for (int col = 0; col < board.Columns; col++)
                {
                    var cell = board.Cells[col, row];
                    Console.Write(cell.IsAlive ? '*' : ' ');
                }
                Console.WriteLine();
            }
        }

        static void Main(string[] args)
        {
            Console.CursorVisible = false;
            LoadConfig();
            InitializeBoard();

            while (true)
            {
                Render();
                board.Advance();
                Thread.Sleep(config.Delay);

                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(true).Key;
                    if (key == ConsoleKey.S)
                    {
                        board.Save("save.txt");
                        Console.WriteLine("Состояние сохранено в save.txt");
                    }
                    else if (key == ConsoleKey.L)
                    {
                        board.Load("save.txt");
                        Console.WriteLine("Состояние загружено из save.txt");
                    }
                    else if (key == ConsoleKey.Escape)
                    {
                        break;
                    }
                }
            }
        }
    }
}
