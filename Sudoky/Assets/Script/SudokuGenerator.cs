using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SudokuGame
{
    /// <summary>
    /// Головний контролер гри судоку. Будує сітку 9x9 з префабів клітинок,
    /// керує вибором клітинки, вводом цифр, перевіркою помилок,
    /// таймером, рівнями складності та станом "гра завершена".
    /// </summary>
    public class SudokuGameManager : MonoBehaviour
    {
        [Header("Префаби та контейнери")]
        public SudokuCell CellPrefab;
        public RectTransform GridContainer; // GridLayoutGroup 9x9 або 3x3 субсітки
        public RectTransform[] BoxContainers; // опціонально: 9 контейнерів 3x3

        [Header("UI елементи")]
        public TextMeshProUGUI TimerText;
        public TextMeshProUGUI DifficultyText;
        public TextMeshProUGUI MistakesText;
        public GameObject WinPanel;
        public TextMeshProUGUI WinTimeText;
        public Button[] NumberButtons; // кнопки 1-9
        public Button EraseButton;
        public Button HintButton;
        public Button NewGameButton;
        public Button[] DifficultyButtons; // 0=Easy,1=Medium,2=Hard

        [Header("Налаштування")]
        public int MaxMistakes = 3;
        public int HintsAllowed = 3;

        private SudokuCell[,] _cells = new SudokuCell[9, 9];
        private SudokuPuzzle _puzzle;
        private SudokuCell _selectedCell;
        private Difficulty _currentDifficulty = Difficulty.Easy;

        private float _elapsedTime;
        private bool _isRunning;
        private bool _isGameOver;
        private int _mistakes;
        private int _hintsUsed;

        private void Awake()
        {
            BuildGrid();
            WireUpButtons();
        }

        private void Start()
        {
            StartNewGame(Difficulty.Easy);
        }

        private void Update()
        {
            if (!_isRunning || _isGameOver) return;

            _elapsedTime += Time.deltaTime;
            if (TimerText != null)
                TimerText.text = FormatTime(_elapsedTime);
        }

        private void BuildGrid()
        {
            for (int row = 0; row < 9; row++)
            {
                for (int col = 0; col < 9; col++)
                {
                    SudokuCell cell = Instantiate(CellPrefab, GridContainer);
                    cell.Init(row, col, this);
                    cell.name = $"Cell_{row}_{col}";
                    _cells[row, col] = cell;
                }
            }
        }

        private void WireUpButtons()
        {
            for (int i = 0; i < NumberButtons.Length; i++)
            {
                int number = i + 1; // замикання по значенню, не по змінній циклу
                NumberButtons[i].onClick.AddListener(() => OnNumberPressed(number));
            }

            if (EraseButton != null)
                EraseButton.onClick.AddListener(OnErasePressed);

            if (HintButton != null)
                HintButton.onClick.AddListener(OnHintPressed);

            if (NewGameButton != null)
                NewGameButton.onClick.AddListener(() => StartNewGame(_currentDifficulty));

            if (DifficultyButtons != null)
            {
                for (int i = 0; i < DifficultyButtons.Length; i++)
                {
                    Difficulty diff = (Difficulty)i;
                    DifficultyButtons[i].onClick.AddListener(() => StartNewGame(diff));
                }
            }
        }

        // -----------------------------------------------------------
        // Старт нової гри / рівень складності
        // -----------------------------------------------------------

        public void StartNewGame(Difficulty difficulty)
        {
            _currentDifficulty = difficulty;
            _puzzle = SudokuGenerator.Generate(difficulty);

            _elapsedTime = 0f;
            _isRunning = true;
            _isGameOver = false;
            _mistakes = 0;
            _hintsUsed = 0;
            _selectedCell = null;

            for (int row = 0; row < 9; row++)
            {
                for (int col = 0; col < 9; col++)
                {
                    int value = _puzzle.Puzzle[row, col];
                    bool isFixed = _puzzle.IsFixed[row, col];
                    _cells[row, col].SetValue(value, isFixed);
                }
            }

            RefreshHighlights();
            UpdateMistakesLabel();

            if (DifficultyText != null)
                DifficultyText.text = GetDifficultyLabel(difficulty);

            if (WinPanel != null)
                WinPanel.SetActive(false);
        }

        private string GetDifficultyLabel(Difficulty difficulty)
        {
            switch (difficulty)
            {
                case Difficulty.Easy: return "Легкий";
                case Difficulty.Medium: return "Середній";
                case Difficulty.Hard: return "Важкий";
                default: return "";
            }
        }

        // -----------------------------------------------------------
        // Вибір клітинки та ввід цифр
        // -----------------------------------------------------------

        public void SelectCell(SudokuCell cell)
        {
            if (_isGameOver) return;

            _selectedCell = cell;
            RefreshHighlights();
        }

        private void OnNumberPressed(int number)
        {
            if (_isGameOver || _selectedCell == null) return;
            if (_selectedCell.IsFixedCell) return; // задані цифри не редагуються

            int row = _selectedCell.Row;
            int col = _selectedCell.Col;

            _selectedCell.SetValue(number, false);

            bool correct = number == _puzzle.Solution[row, col];
            _selectedCell.SetError(!correct);

            if (!correct)
            {
                _mistakes++;
                UpdateMistakesLabel();

                if (_mistakes >= MaxMistakes)
                {
                    HandleGameOver(false);
                    RefreshHighlights();
                    return;
                }
            }

            RefreshHighlights();
            CheckForWin();
        }

        private void OnErasePressed()
        {
            if (_isGameOver || _selectedCell == null) return;
            if (_selectedCell.IsFixedCell) return;

            _selectedCell.SetValue(0, false);
            RefreshHighlights();
        }

        private void OnHintPressed()
        {
            if (_isGameOver || _selectedCell == null) return;
            if (_selectedCell.IsFixedCell || !_selectedCell.IsEmpty) return;
            if (_hintsUsed >= HintsAllowed) return;

            int row = _selectedCell.Row;
            int col = _selectedCell.Col;
            int correctValue = _puzzle.Solution[row, col];

            _selectedCell.SetValue(correctValue, false);
            _selectedCell.ShowHintFlash();
            _hintsUsed++;

            RefreshHighlights();
            CheckForWin();
        }

        // -----------------------------------------------------------
        // Підсвітка (вибрана клітинка, однакові цифри, рядок/стовпець/блок)
        // -----------------------------------------------------------

        private void RefreshHighlights()
        {
            int selectedValue = _selectedCell != null ? _selectedCell.Value : 0;

            for (int row = 0; row < 9; row++)
            {
                for (int col = 0; col < 9; col++)
                {
                    SudokuCell cell = _cells[row, col];
                    bool isSelected = cell == _selectedCell;
                    bool sameNumber = selectedValue != 0 && cell.Value == selectedValue && !isSelected;

                    cell.UpdateBackground(isSelected, sameNumber);
                }
            }
        }

        private void UpdateMistakesLabel()
        {
            if (MistakesText != null)
                MistakesText.text = $"Помилки: {_mistakes}/{MaxMistakes}";
        }

        // -----------------------------------------------------------
        // Перевірка перемоги / завершення гри
        // -----------------------------------------------------------

        private void CheckForWin()
        {
            for (int row = 0; row < 9; row++)
                for (int col = 0; col < 9; col++)
                    if (_cells[row, col].Value != _puzzle.Solution[row, col])
                        return; // ще не все правильно заповнено

            HandleGameOver(true);
        }

        private void HandleGameOver(bool won)
        {
            _isGameOver = true;
            _isRunning = false;

            if (WinPanel != null)
                WinPanel.SetActive(true);

            if (WinTimeText != null)
            {
                WinTimeText.text = won
                    ? $"Вітаємо! Розв'язано за {FormatTime(_elapsedTime)}"
                    : $"Гру завершено: перевищено ліміт помилок ({MaxMistakes})";
            }
        }

        

        private string FormatTime(float seconds)
        {
            int totalSeconds = Mathf.FloorToInt(seconds);
            int minutes = totalSeconds / 60;
            int secs = totalSeconds % 60;
            return $"{minutes:00}:{secs:00}";
        }
    }
}