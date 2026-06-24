using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;


public class HangmanGame : MonoBehaviour
{
    [Header("Список слів для вгадування")]
    public List<string> wordList = new List<string>
    {
         "ПРОГРАМА", "КОМП'ЮТЕР", "ЮНІТІ", "СКРИПТ", "ГРАВЕЦЬ",
         "МОНІТОР", "КЛАВІАТУРА", "ІНТЕРНЕТ", "ТЕЛЕФОН", "ВІКНО",
         "СЕРВЕР", "ПАРОЛЬ", "ДРАЙВЕР", "ПРОЦЕСОР", "ПРОЕКТ",
         "МИШКА", "НОУТБУК", "ПІКСЕЛЬ", "КАНВАС", "ІНСПЕКТОР",
         "СПРАЙТ", "ЛОГІКА", "ДАНІ", "КОНСОЛЬ", "МАТРИЦЯ",
         "ПАМ'ЯТЬ", "КЛИЕНТ", "СЕНСОР", "КАМЕРА", "ДОДАТОК"

    };

    [Header("UI елементи")]
    public TextMeshProUGUI wordDisplayText;      // Відображення слова з прогалинами "_ _ _"
    public TextMeshProUGUI attemptsLeftText;     // Текст "Залишилось спроб: X"
    public TextMeshProUGUI resultText;           // Текст переможної/проигральної надписи
    public Transform lettersPanel;               // Панель, де будуть кнопки літер
    public Button letterButtonPrefab;            // Префаб кнопки літери
    public Button restartButton;                 // Кнопка "Заново"

    [Header("3D частини шибениці (Renderer/GameObject для кожної частини)")]
    public GameObject[] hangmanParts; // 0=стовп, 1=поперечка, 2=верьовка, 3=голова, 4=тіло, 5=рука1, 6=рука2, 7=нога1, 8=нога2

    [Header("Налаштування")]
    public int maxAttempts = 9; 

    private string currentWord;
    private HashSet<char> guessedLetters = new HashSet<char>();
    private int wrongAttempts = 0;
    private bool gameOver = false;

    private static readonly string ukrainianAlphabet = "АБВГДЕЄЖЗИІЇЙКЛМНОПРСТУФХЦЧШЩЬЮЯ'";

    void Start()
    {
        if (restartButton != null)
            restartButton.onClick.AddListener(StartNewGame);

        StartNewGame();
    }

    public void StartNewGame()
    {
        gameOver = false;
        wrongAttempts = 0;
        guessedLetters.Clear();

        currentWord = wordList[Random.Range(0, wordList.Count)].ToUpper();

        
        foreach (var part in hangmanParts)
        {
            if (part != null) part.SetActive(false);
        }

        resultText.text = "";
        UpdateWordDisplay();
        UpdateAttemptsText();
        GenerateLetterButtons();
    }

    void GenerateLetterButtons()
    {
        
        foreach (Transform child in lettersPanel)
        {
            Destroy(child.gameObject);
        }

        foreach (char letter in ukrainianAlphabet)
        {
            Button btn = Instantiate(letterButtonPrefab, lettersPanel);
            TextMeshProUGUI btnText = btn.GetComponentInChildren<TextMeshProUGUI>();
            btnText.text = letter.ToString();

            char capturedLetter = letter; 
            btn.onClick.AddListener(() => OnLetterClicked(capturedLetter, btn));
        }
    }

    void OnLetterClicked(char letter, Button btn)
    {
        if (gameOver) return;

        btn.interactable = false; 
        guessedLetters.Add(letter);

        if (currentWord.Contains(letter))
        {
            UpdateWordDisplay();
            CheckWinCondition();
        }
        else
        {
            wrongAttempts++;
            ShowNextHangmanPart();
            UpdateAttemptsText();
            CheckLoseCondition();
        }
    }

    void UpdateWordDisplay()
    {
        string display = "";
        foreach (char c in currentWord)
        {
            if (guessedLetters.Contains(c))
                display += c + " ";
            else
                display += "_ ";
        }
        wordDisplayText.text = display.Trim();
    }

    void UpdateAttemptsText()
    {
        attemptsLeftText.text = "Залишилось спроб: " + (maxAttempts - wrongAttempts);
    }

    void ShowNextHangmanPart()
    {
        if (wrongAttempts - 1 < hangmanParts.Length && wrongAttempts - 1 >= 0)
        {
            hangmanParts[wrongAttempts - 1].SetActive(true);
        }
    }

    void CheckWinCondition()
    {
        foreach (char c in currentWord)
        {
            if (!guessedLetters.Contains(c))
                return; 
        }

        gameOver = true;
        resultText.text = "Вітаю! Ви перемогли! 🎉";
        resultText.color = Color.green;
    }

    void CheckLoseCondition()
    {
        if (wrongAttempts >= maxAttempts)
        {
            gameOver = true;
            resultText.text = "Гра закінчена! Слово було: " + currentWord;
            resultText.color = Color.red;
        }
    }
}