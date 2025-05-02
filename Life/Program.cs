using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using ScottPlot;


namespace cli_life
{
    /// <summary>
    /// Классификатор фигур. Содержит шаблоны известных фигур и методы для их распознавания.
    /// </summary>
    public class FigureClassifier
    {
        /// <summary>
        /// Словарь шаблонов фигур с их названиями.
        /// </summary>
        private static readonly Dictionary<string, HashSet<(int, int)>> Patterns = new()
        {
            ["Block"] = new() { (0, 0), (1, 0), (0, 1), (1, 1) },
            ["Blinker"] = new() { (0, 0), (0, 1), (0, 2) },
            ["Beacon"] = new() { (0, 0), (1, 0), (0, 1), (1, 1), (2, 2), (3, 2), (2, 3), (3, 3) },
            ["Glider"] = new() { (0, 0), (1, 1), (2, 1), (0, 2), (1, 2) }
        };

        /// <summary>
        /// Классифицирует фигуру по известным шаблонам
        /// </summary>
        /// <param name="figure">Множество координат фигуры</param>
        /// <returns>Название фигуры или "Unknown"</returns>
        public static string Classify(HashSet<(int, int)> figure)
        {
            var normalized = Normalize(figure);

            foreach (var (name, pattern) in Patterns)
            {
                if (Matches(pattern, normalized))
                    return name;
            }
            return "Unknown";
        }

        /// <summary>
        /// Нормализует координаты фигуры для сравнения
        /// </summary>
        /// <param name="figure">Исходная фигура</param>
        /// <returns>Фигура с координатами относительно (0,0)</returns>
        private static HashSet<(int, int)> Normalize(HashSet<(int, int)> figure)
        {
            if (!figure.Any()) return figure;

            int minX = figure.Min(p => p.Item1);
            int minY = figure.Min(p => p.Item2);
            return new HashSet<(int, int)>(figure.Select(p => (p.Item1 - minX, p.Item2 - minY)));
        }

        /// <summary>
        /// Проверяет совпадение фигуры с паттерном
        /// </summary>
        /// <param name="pattern">Эталонный паттерн</param>
        /// <param name="figure">Проверяемая фигура</param>
        /// <returns>True если фигура соответствует паттерну</returns>
        private static bool Matches(HashSet<(int, int)> pattern, HashSet<(int, int)> figure)
        {
            if (pattern.Count != figure.Count) return false;

            var variations = GenerateAllVariations(figure);
            return variations.Any(v => pattern.SetEquals(v));
        }

        /// <summary>
        /// Генерирует все варианты поворотов и отражений фигуры
        /// </summary>
        /// <param name="figure">Исходная фигура</param>
        /// <returns>Все возможные варианты преобразований</returns>
        private static IEnumerable<HashSet<(int, int)>> GenerateAllVariations(HashSet<(int, int)> figure)
        {
            var variations = new HashSet<HashSet<(int, int)>>();
            var current = figure;

            for (int i = 0; i < 4; i++)
            {
                var normalized = Normalize(current);
                if (!variations.Contains(normalized))
                    variations.Add(normalized);

                var mirrored = Normalize(Mirror(current));
                if (!variations.Contains(mirrored))
                    variations.Add(mirrored);

                current = Rotate90(current);
            }
            return variations;
        }

        /// <summary>
        /// Поворачивает фигуру на 90 градусов
        /// </summary>
        /// <param name="figure">Исходная фигура</param>
        /// <returns>Повернутая фигура</returns>
        private static HashSet<(int, int)> Rotate90(HashSet<(int, int)> figure)
        {
            int maxX = figure.Max(p => p.Item1);
            return new HashSet<(int, int)>(figure.Select(p => (p.Item2, maxX - p.Item1)));
        }

        /// <summary>
        /// Отражает фигуру по вертикали
        /// </summary>
        /// <param name="figure">Исходная фигура</param>
        /// <returns>Отраженная фигура</returns>
        private static HashSet<(int, int)> Mirror(HashSet<(int, int)> figure)
        {
            int maxX = figure.Max(p => p.Item1);
            return new HashSet<(int, int)>(figure.Select(p => (maxX - p.Item1, p.Item2)));
        }
    }


    public class FigureAnalyzer
    {
        /// <summary>
        /// Находит все изолированные фигуры на поле
        /// </summary>
        /// <param name="board">Игровое поле</param>
        /// <returns>Список найденных фигур</returns>
        public static List<HashSet<(int, int)>> FindAllFigures(Board board)
        {
            var figures = new List<HashSet<(int, int)>>();
            var visited = new bool[board.Columns, board.Rows];

            for (int x = 0; x < board.Columns; x++)
            {
                for (int y = 0; y < board.Rows; y++)
                {
                    if (!visited[x, y] && board.Cells[x, y].IsAlive)
                    {
                        var figure = new HashSet<(int, int)>();
                        ExploreFigure(x, y, board, visited, figure);
                        figures.Add(figure);
                    }
                }
            }
            return figures;
        }

        /// <summary>
        /// Рекурсивно исследует фигуру методом поиска в глубину
        /// </summary>
        /// <param name="x">Начальная координата X</param>
        /// <param name="y">Начальная координата Y</param>
        /// <param name="board">Игровое поле</param>
        /// <param name="visited">Массив посещенных клеток</param>
        /// <param name="figure">Текущая исследуемая фигура</param>
        private static void ExploreFigure(int x, int y, Board board, bool[,] visited, HashSet<(int, int)> figure)
        {
            if (x < 0 || x >= board.Columns || y < 0 || y >= board.Rows) return;
            if (visited[x, y] || !board.Cells[x, y].IsAlive) return;

            visited[x, y] = true;
            figure.Add((x, y));

            foreach (var neighbor in board.Cells[x, y].neighbors)
            {
                var (nx, ny) = FindCellCoordinates(neighbor, board);
                if (nx != -1 && ny != -1)
                    ExploreFigure(nx, ny, board, visited, figure);
            }
        }

        // <summary>
        /// Находит координаты клетки на поле
        /// </summary>
        /// <param name="cell">Искомая клетка</param>
        /// <param name="board">Игровое поле</param>
        /// <returns>Координаты (X,Y) или (-1,-1)</returns>
        private static (int, int) FindCellCoordinates(Cell cell, Board board)
        {
            for (int x = 0; x < board.Columns; x++)
                for (int y = 0; y < board.Rows; y++)
                    if (board.Cells[x, y] == cell)
                        return (x, y);
            return (-1, -1);
        }
    }

    public class Cell
    {
        // Текущее состояние клетки (живая/мертвая).
        public bool IsAlive;

        // Список соседних клеток.
        public readonly List<Cell> neighbors = new List<Cell>();

        // Состояние клетки в следующем поколении.
        public bool IsAliveNext { get; private set; }

        // Определяет следующее состояние клетки.
        public void DetermineNextLiveState()
        {
            int liveNeighbors = neighbors.Count(x => x.IsAlive);
            IsAliveNext = IsAlive ? (liveNeighbors == 2 || liveNeighbors == 3) : (liveNeighbors == 3);
        }

        // Переход к следующему состоянию.
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
        public int AliveCount => Cells.Cast<Cell>().Count(c => c.IsAlive);

        private readonly Random rand = new Random();

        /// <summary>
        /// Создает новое игровое поле
        /// </summary>
        /// <param name="width">Ширина поля</param>
        /// <param name="height">Высота поля</param>
        /// <param name="cellSize">Размер клетки</param>
        public Board(int width, int height, int cellSize)
        {
            CellSize = cellSize;
            Cells = new Cell[width / cellSize, height / cellSize];
            for (int x = 0; x < Columns; x++)
                for (int y = 0; y < Rows; y++)
                    Cells[x, y] = new Cell();

            ConnectNeighbors();
        }

        /// <summary>
        /// Инициализирует поле случайными значениями
        /// </summary>
        /// <param name="liveDensity">Плотность заполнения</param>
        public void Randomize(double liveDensity)
        {
            foreach (var cell in Cells)
                cell.IsAlive = rand.NextDouble() < liveDensity;
        }

        /// <summary>
        /// Переход к следующему поколению
        /// </summary>
        public void Advance()
        {
            foreach (var cell in Cells)
                cell.DetermineNextLiveState();
            foreach (var cell in Cells)
                cell.Advance();
        }

        /// <summary>
        /// Сохраняет текущее состояние в файл
        /// </summary>
        /// <param name="filePath">Путь к файлу</param>
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

        /// <summary>
        /// Загружает состояние из файла
        /// </summary>
        /// <param name="filePath">Путь к файлу</param>
        public void Load(string filePath)
        {
            var lines = File.ReadAllLines(filePath)
                .Select(line => line.Trim())
                .Where(line => !string.IsNullOrEmpty(line))
                .ToArray();

            for (int y = 0; y < Math.Min(lines.Length, Rows); y++)
            {
                for (int x = 0; x < Math.Min(lines[y].Length, Columns); x++)
                {
                    char c = lines[y][x];
                    Cells[x, y].IsAlive = c == '1' || c == '*';
                }
            }
        }

        /// <summary>
        /// Устанавливает связи между соседними клетками
        /// </summary>
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
        public int StableGenerations { get; set; } = 10;
    }

    public class SimulationResult
    {
        public double Density { get; set; }
        public List<int> PopulationHistory { get; set; } = new();
    }

    public class Program
    {
        private static List<SimulationResult> results = new();
        private static readonly double[] Densities = { 0.1, 0.3, 0.5, 0.7, 0.9 };

        static void RunSimulation(double density)
        {
            var board = new Board(50, 20, 1);
            board.Randomize(density);

            var result = new SimulationResult { Density = density };
            int stableCount = 0;
            const int stableThreshold = 20;

            while (stableCount < stableThreshold && result.PopulationHistory.Count < 1000)
            {
                board.Advance();
                result.PopulationHistory.Add(board.AliveCount);

                // Проверка стабильности последних 5 поколений
                if (result.PopulationHistory.Count > 5)
                {
                    var last = result.PopulationHistory.TakeLast(5).ToArray();
                    if (last.Distinct().Count() == 1) stableCount++;
                    else stableCount = 0;
                }
            }

            results.Add(result);
        }

        static void SaveResults()
        {
            // Сохранение сырых данных
            using var writer = new StreamWriter("data.txt");
            writer.WriteLine("Generation," + string.Join(",", Densities.Select(d => $"Density {d}")));

            int maxGen = results.Max(r => r.PopulationHistory.Count);
            for (int gen = 0; gen < maxGen; gen++)
            {
                var line = $"{gen}";
                foreach (var density in Densities)
                {
                    var result = results.First(r => r.Density == density);
                    line += "," + (gen < result.PopulationHistory.Count
                        ? result.PopulationHistory[gen].ToString()
                        : "NA");
                }
                writer.WriteLine(line);
            }

            // Построение графика
            var plt = new Plot();
            plt.Title("Динамика численности клеток");
            plt.XLabel("Номер поколения");
            plt.YLabel("Количество живых клеток");
            plt.Legend.IsVisible = true;

            var colors = new[] { Colors.Blue, Colors.Green, Colors.Red, Colors.Orange, Colors.Purple };

            for (int i = 0; i < results.Count; i++)
            {
                var result = results[i];
                double[] xs = Enumerable.Range(0, result.PopulationHistory.Count)
                    .Select(x => (double)x).ToArray();
                double[] ys = result.PopulationHistory.Select(y => (double)y).ToArray();

                var scatter = plt.Add.Scatter(xs, ys);
                scatter.Label = $"Плотность {result.Density:0.0}";
                scatter.Color = colors[i];
                scatter.LineWidth = 2;
                scatter.MarkerSize = 0;
            }

            plt.SavePng("plot.png", 1200, 800);
        }

        static void Main(string[] args)
        {
            foreach (var density in Densities)
            {
                RunSimulation(density);
            }
            SaveResults();
            Console.WriteLine("Анализ завершен. Результаты сохранены в data.txt и plot.png");
        }
    }
}

    
    /*
    public class Program
    {
        private static List<int> populationHistory = new();
        private static int stableGenerations;
        static Board board;
        static Config config;

        public static Config LoadConfig(string configPath)
        {
            if (File.Exists(configPath))
            {
                var json = File.ReadAllText(configPath);
                return JsonSerializer.Deserialize<Config>(json) ?? new Config();
            }
            return new Config();
        }

        /// <summary>
        /// Загружает конфигурацию из файла
        /// </summary>
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
            stableGenerations = config.StableGenerations;
        }

        /// <summary>
        /// Инициализирует игровое поле
        /// </summary>
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

        /// <summary>
        /// Отрисовывает текущее состояние поля
        /// </summary>
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

        /// <summary>
        /// Анализирует и выводит информацию о фигурах
        /// </summary>
        static void AnalyzeAndReport()
        {
            var figures = FigureAnalyzer.FindAllFigures(board);
            var counts = new Dictionary<string, int>
            {
                ["Block"] = 0,
                ["Blinker"] = 0,
                ["Beacon"] = 0,
                ["Glider"] = 0,
                ["Unknown"] = 0
            };

            foreach (var figure in figures)
            {
                string type = FigureClassifier.Classify(figure);
                counts[type]++;
            }

            Console.WriteLine("\nРезультаты анализа:");
            foreach (var entry in counts)
                Console.WriteLine($"{entry.Key}: {entry.Value}");
        }

        /// <summary>
        /// Проверяет достижение стабильного состояния
        /// </summary>
        /// <returns>True если состояние стабильно</returns>
        public static bool IsStable()
        {
            if (populationHistory.Count < stableGenerations) return false;
            var lastValues = populationHistory.TakeLast(stableGenerations);
            return lastValues.All(v => v == lastValues.First());
        }

        /// <summary>
        /// Сохраняет данные и строит график
        /// </summary>
        static void SaveDataAndPlot()
        {
            try
            {
                File.WriteAllLines("data.txt", populationHistory.Select((v, i) => $"{i}\t{v}"));

                // Создаем новый объект Plot
                var plt = new Plot();

                // Создаем данные
                double[] x = Enumerable.Range(0, populationHistory.Count)
                    .Select(i => (double)i).ToArray();
                double[] y = populationHistory.Select(v => (double)v).ToArray();

                // Добавляем данные на график
                var scatter = plt.Add.Scatter(x, y);
                scatter.Color = Colors.Blue;
                scatter.LineWidth = 2;

                // Настраиваем оформление
                plt.Title("Динамика численности клеток");
                plt.XLabel("Поколение");
                plt.YLabel("Живые клетки");

                // Сохраняем с указанием размера изображения
                plt.SavePng("plot.png", 800, 400);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при построении графика: {ex.Message}");
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

                populationHistory.Add(board.AliveCount);

                if (IsStable())
                {
                    Console.WriteLine($"\nСистема стабилизировалась на поколении {populationHistory.Count - stableGenerations}");
                    break;
                }

                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(true).Key;
                    switch (key)
                    {
                        case ConsoleKey.S:
                            board.Save("save.txt");
                            Console.WriteLine("Состояние сохранено в save.txt");
                            break;
                        case ConsoleKey.L:
                            board.Load("save.txt");
                            Console.WriteLine("Состояние загружено из save.txt");
                            break;
                        case ConsoleKey.A:
                            AnalyzeAndReport();
                            break;
                        case ConsoleKey.Escape:
                            SaveDataAndPlot();
                            return;
                    }
                }
            }
            SaveDataAndPlot();
        }
    }
}
    */