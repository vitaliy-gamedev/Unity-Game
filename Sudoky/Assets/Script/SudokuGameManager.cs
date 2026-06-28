using System;
using System.Collections.Generic;

namespace SudokuGame
{
    
    public enum Difficulty
    {
        Easy,   // легкий   ~ 38-45 відкритих клітинок
        Medium, // середній ~ 30-37 відкритих клітинок
        Hard    // важкий   ~ 22-29 відкритих клітинок
    }

    
    public class SudokuPuzzle
    {
        public int[,] Solution; // повне правильне рішення 9x9
        public int[,] Puzzle;   // головоломка з порожніми клітинками 9x9
        public bool[,] IsFixed; // true = клітинка задана від початку (не редагується)
        public Difficulty Difficulty;
    }

    public static class SudokuGenerator
    {
        private const int Size = 9;
        private const int BoxSize = 3;

        private static readonly System.Random Rng = new System.Random();

       
        public static SudokuPuzzle Generate(Difficulty difficulty)
        {
            int[,] solution = GenerateFullSolution();
            int[,] puzzle = (int[,])solution.Clone();
            bool[,] isFixed = new bool[Size, Size];

            int cellsToRemove = GetCellsToRemove(difficulty);
            RemoveCells(puzzle, cellsToRemove);

            for (int r = 0; r < Size; r++)
                for (int c = 0; c < Size; c++)
                    isFixed[r, c] = puzzle[r, c] != 0;

            return new SudokuPuzzle
            {
                Solution = solution,
                Puzzle = puzzle,
                IsFixed = isFixed,
                Difficulty = difficulty
            };
        }

       
        private static int GetCellsToRemove(Difficulty difficulty)
        {
            switch (difficulty)
            {
                case Difficulty.Easy:
                    return Rng.Next(81 - 45, 81 - 38 + 1); // лишає 38-45 заповнених
                case Difficulty.Medium:
                    return Rng.Next(81 - 37, 81 - 30 + 1); // лишає 30-37 заповнених
                case Difficulty.Hard:
                    return Rng.Next(81 - 29, 81 - 22 + 1); // лишає 22-29 заповнених
                default:
                    return 40;
            }
        }

        // ---------------------------------------------------------------
        // Генерація повного валідного поля 9x9 методом backtracking
        // ---------------------------------------------------------------

        private static int[,] GenerateFullSolution()
        {
            int[,] grid = new int[Size, Size];
            FillGrid(grid, 0, 0);
            return grid;
        }

        private static bool FillGrid(int[,] grid, int row, int col)
        {
            if (row == Size)
                return true; // дійшли до кінця - поле заповнене повністю

            int nextRow = (col == Size - 1) ? row + 1 : row;
            int nextCol = (col == Size - 1) ? 0 : col + 1;

            // Рандомізований порядок цифр 1-9 - саме це дає різні поля щоразу
            List<int> numbers = ShuffledRange(1, 9);

            foreach (int num in numbers)
            {
                if (IsValidPlacement(grid, row, col, num))
                {
                    grid[row, col] = num;

                    if (FillGrid(grid, nextRow, nextCol))
                        return true;

                    grid[row, col] = 0; // повертаємось назад (backtrack)
                }
            }

            return false;
        }

        // Видалення клітинок зі збереженням єдиності рішення
      

        private static void RemoveCells(int[,] puzzle, int cellsToRemove)
        {
            List<(int row, int col)> positions = new List<(int row, int col)>();
            for (int r = 0; r < Size; r++)
                for (int c = 0; c < Size; c++)
                    positions.Add((r, c));

            Shuffle(positions);

            int removed = 0;
            foreach (var pos in positions)
            {
                if (removed >= cellsToRemove)
                    break;

                int backup = puzzle[pos.row, pos.col];
                if (backup == 0)
                    continue;

                puzzle[pos.row, pos.col] = 0;

                // Перевіряємо, що головоломка досі має РІВНО ОДНЕ рішення.
                // Якщо ні - повертаємо цифру назад.
                int solutionsFound = CountSolutions((int[,])puzzle.Clone(), 2);

                if (solutionsFound != 1)
                {
                    puzzle[pos.row, pos.col] = backup;
                }
                else
                {
                    removed++;
                }
            }
        }

       
        private static int CountSolutions(int[,] grid, int maxCount)
        {
            int count = 0;
            SolveCount(grid, 0, 0, ref count, maxCount);
            return count;
        }

        private static void SolveCount(int[,] grid, int row, int col, ref int count, int maxCount)
        {
            if (count >= maxCount)
                return;

            if (row == Size)
            {
                count++;
                return;
            }

            int nextRow = (col == Size - 1) ? row + 1 : row;
            int nextCol = (col == Size - 1) ? 0 : col + 1;

            if (grid[row, col] != 0)
            {
                SolveCount(grid, nextRow, nextCol, ref count, maxCount);
                return;
            }

            for (int num = 1; num <= 9; num++)
            {
                if (count >= maxCount)
                    return;

                if (IsValidPlacement(grid, row, col, num))
                {
                    grid[row, col] = num;
                    SolveCount(grid, nextRow, nextCol, ref count, maxCount);
                    grid[row, col] = 0;
                }
            }
        }

        
        public static bool IsValidPlacement(int[,] grid, int row, int col, int num)
        {
            for (int c = 0; c < Size; c++)
                if (grid[row, c] == num)
                    return false;

            for (int r = 0; r < Size; r++)
                if (grid[r, col] == num)
                    return false;

            int boxRow = (row / BoxSize) * BoxSize;
            int boxCol = (col / BoxSize) * BoxSize;
            for (int r = boxRow; r < boxRow + BoxSize; r++)
                for (int c = boxCol; c < boxCol + BoxSize; c++)
                    if (grid[r, c] == num)
                        return false;

            return true;
        }

        private static List<int> ShuffledRange(int min, int max)
        {
            List<int> list = new List<int>();
            for (int i = min; i <= max; i++)
                list.Add(i);
            Shuffle(list);
            return list;
        }

        private static void Shuffle<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}