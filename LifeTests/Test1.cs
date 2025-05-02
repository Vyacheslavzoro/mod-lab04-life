using Microsoft.VisualStudio.TestTools.UnitTesting;
using cli_life;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Life.Tests
{
    [TestClass]
    public class LifeTests
    {
        /// <summary>
        /// Тест проверяет, что клетка умирает при недостатке соседей (меньше 2)
        /// </summary>
        [TestMethod]
        public void CellDiesWhenUnderpopulated()
        {
            var cell = new Cell { IsAlive = true };
            // Добавляем 1 живого соседа
            cell.neighbors.Add(new Cell { IsAlive = true });
            cell.DetermineNextLiveState();
            Assert.IsFalse(cell.IsAliveNext);
        }

        /// <summary>
        /// Тест проверяет, что клетка выживает с 2 или 3 соседями
        /// </summary>
        [TestMethod]
        public void CellSurvivesWithTwoOrThreeNeighbors()
        {
            var cell = new Cell { IsAlive = true };
            // Добавляем 2 живых соседа
            cell.neighbors.AddRange(new[] {
                new Cell { IsAlive = true },
                new Cell { IsAlive = true }
            });
            cell.DetermineNextLiveState();
            Assert.IsTrue(cell.IsAliveNext);
        }

        /// <summary>
        /// Тест проверяет корректность инициализации размера доски
        /// </summary>
        [TestMethod]
        public void BoardInitializesCorrectSize()
        {
            var board = new Board(60, 20, 1);
            Assert.AreEqual(60, board.Columns);
            Assert.AreEqual(20, board.Rows);
        }

        /// <summary>
        /// Тест проверяет правильность подключения соседей клетки
        /// </summary>
        [TestMethod]
        public void BoardConnectsNeighborsCorrectly()
        {
            var board = new Board(3, 3, 1);
            Assert.AreEqual(8, board.Cells[1, 1].neighbors.Count);
        }

        /// <summary>
        /// Тест проверяет корректность работы метода Randomize с разными значениями плотности
        /// </summary>
        [TestMethod]
        public void RandomizeWithDifferentDensities()
        {
            var board = new Board(50, 50, 1);

            // Тестируем несколько значений плотности
            var testCases = new[] {
            new { Density = 0.1, ExpectedMin = 150, ExpectedMax = 350 },
            new { Density = 0.5, ExpectedMin = 1000, ExpectedMax = 1500 },
            new { Density = 0.9, ExpectedMin = 2000, ExpectedMax = 2500 }
        };

            foreach (var testCase in testCases)
            {
                board.Randomize(testCase.Density);
                int aliveCount = board.AliveCount;

                Assert.IsTrue(aliveCount >= testCase.ExpectedMin,
                    $"Для плотности {testCase.Density} ожидалось минимум {testCase.ExpectedMin} живых клеток, получено {aliveCount}");

                Assert.IsTrue(aliveCount <= testCase.ExpectedMax,
                    $"Для плотности {testCase.Density} ожидалось максимум {testCase.ExpectedMax} живых клеток, получено {aliveCount}");
            }
        }

        /// <summary>
        /// Тест проверяет идентификацию фигуры "Блок"
        /// </summary>
        [TestMethod]
        public void ClassifierIdentifiesBlock()
        {
            var block = new HashSet<(int, int)> { (0, 0), (1, 0), (0, 1), (1, 1) };
            Assert.AreEqual("Block", FigureClassifier.Classify(block));
        }

        /// <summary>
        /// Тест проверяет идентификацию фигуры "Планер"
        /// </summary>
        [TestMethod]
        public void ClassifierIdentifiesGlider()
        {
            var glider = new HashSet<(int, int)> { (0, 0), (1, 1), (2, 1), (0, 2), (1, 2) };
            Assert.AreEqual("Glider", FigureClassifier.Classify(glider));
        }

        /// <summary>
        /// Тест проверяет обнаружение одиночной фигуры на доске
        /// </summary>
        [TestMethod]
        public void AnalyzerFindsSingleFigure()
        {
            var board = new Board(5, 5, 1);
            board.Cells[2, 2].IsAlive = true;
            var figures = FigureAnalyzer.FindAllFigures(board);
            Assert.AreEqual(1, figures.Count);
        }

        /// <summary>
        /// Тест проверяет корректность классификации известных фигур
        /// </summary>
        [TestMethod]
        public void ClassifyKnownFiguresCorrectly()
        {
            var block = new HashSet<(int, int)> { (0, 0), (1, 0), (0, 1), (1, 1) };
            var blinker = new HashSet<(int, int)> { (0, 0), (0, 1), (0, 2) };
            var glider = new HashSet<(int, int)> { (0, 0), (1, 1), (2, 1), (0, 2), (1, 2) };

            Assert.AreEqual("Block", FigureClassifier.Classify(block));
            Assert.AreEqual("Blinker", FigureClassifier.Classify(blinker));
            Assert.AreEqual("Glider", FigureClassifier.Classify(glider));

            var rotatedBlinker = new HashSet<(int, int)> { (0, 0), (1, 0), (2, 0) };
            Assert.AreEqual("Blinker", FigureClassifier.Classify(rotatedBlinker));
        }

        /// <summary>
        /// Тест проверяет, что рандомизация учитывает плотность заполнения
        /// </summary>
        [TestMethod]
        public void RandomizeRespectsDensity()
        {
            var board = new Board(100, 100, 1);
            board.Randomize(0.3);
            double ratio = (double)board.AliveCount / (100 * 100);
            Assert.IsTrue(ratio > 0.25 && ratio < 0.35);
        }

        /// <summary>
        /// Тест проверяет работу зацикливания краев доски
        /// </summary>
        [TestMethod]
        public void EdgeWrappingWorks()
        {
            var board = new Board(3, 3, 1);
            var cell = board.Cells[0, 0];
            CollectionAssert.Contains(cell.neighbors, board.Cells[2, 2]);
        }

        /// <summary>
        /// Тест проверяет загрузку конфигурации по умолчанию
        /// </summary>
        [TestMethod]
        public void ConfigLoadsDefaults()
        {
            var config = new Config();
            Assert.AreEqual(50, config.Width);
        }

        /// <summary>
        /// Тест проверяет оживление мертвой клетки с 3 соседями
        /// </summary>
        [TestMethod]
        public void DeadCellComesToLife()
        {
            var cell = new Cell { IsAlive = false };
            // Добавляем 3 живых соседа
            cell.neighbors.AddRange(new Cell[3] {
                new Cell { IsAlive = true },
                new Cell { IsAlive = true },
                new Cell { IsAlive = true }
            });
            cell.DetermineNextLiveState();
            Assert.IsTrue(cell.IsAliveNext);
        }

        /// <summary>
        /// Тест проверяет корректность работы осциллятора "Маяк"
        /// </summary>
        [TestMethod]
        public void BeaconOscillatesCorrectly()
        {
            var board = new Board(6, 6, 1);

            // Вручную создаем фигуру "Маяк"
            board.Cells[1, 1].IsAlive = true;
            board.Cells[2, 1].IsAlive = true;
            board.Cells[1, 2].IsAlive = true;
            board.Cells[4, 3].IsAlive = true;
            board.Cells[3, 4].IsAlive = true;
            board.Cells[4, 4].IsAlive = true;

            var initialState = new bool[board.Columns, board.Rows];
            for (int x = 0; x < board.Columns; x++)
                for (int y = 0; y < board.Rows; y++)
                    initialState[x, y] = board.Cells[x, y].IsAlive;

            board.Advance();
            board.Advance();

            for (int x = 0; x < board.Columns; x++)
                for (int y = 0; y < board.Rows; y++)
                    Assert.AreEqual(initialState[x, y], board.Cells[x, y].IsAlive);
        }

        /// <summary>
        /// Тест проверяет определение стабильного состояния системы
        /// </summary>
        [TestMethod]
        public void StabilityDetectedCorrectly()
        {
            var board = new Board(10, 10, 1);

            // Создаем стабильную фигуру (блок)
            board.Cells[1, 1].IsAlive = true;
            board.Cells[2, 1].IsAlive = true;
            board.Cells[1, 2].IsAlive = true;
            board.Cells[2, 2].IsAlive = true;

            int initialAlive = board.AliveCount;
            board.Advance();
            int nextAlive = board.AliveCount;

            Assert.AreEqual(initialAlive, nextAlive, "Количество живых клеток не должно меняться для стабильной фигуры");
        }
    }
}