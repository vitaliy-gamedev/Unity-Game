using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class CreateTwoGrid : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private GameObject _cellPrefab;
    [SerializeField] private GameObject explosionPrefab;

    [Header("Grid Parents")]
    [SerializeField] private Transform _leftGrid;
    [SerializeField] private Transform _rightGrid;

    [Header("End UI")]
    [SerializeField] private GameObject endPanel;
    [SerializeField] private GameObject winText;
    [SerializeField] private GameObject loseText;
    [SerializeField] private GameObject restartButton;
    [SerializeField] private GameObject mainMenuButton;

    [Header("Кнопки складності")]
    [SerializeField] private Button easyButton;
    [SerializeField] private Button mediumButton;
    [SerializeField] private Button hardButton;

    [Header("Кнопки керування")]
    [SerializeField] private Button reshuffleButton;
    [SerializeField] private Button startBattleButton;

    [Header("Grid Settings")]
    [SerializeField] private int _rows = 10;
    [SerializeField] private int _columns = 10;
    [SerializeField] private float _cellSize = 50f;

    private int[,] playerGrid;
    private int[,] enemyGrid;
    private int[,] playerShipId;
    private int[,] enemyShipId;

    private Button[,] playerButtons;
    private Button[,] enemyButtons;
    private Image[,] playerImages;
    private Image[,] enemyImages;

    private bool isPlayerTurn = false;
    private bool gameOver = false;
    private bool battleStarted = false;

    public enum BotDifficulty { Easy, Medium, Hard }
    private BotDifficulty difficulty = BotDifficulty.Medium;

    private Color selectedColor = Color.yellow;
    private Color normalColor = Color.white;

    void HighlightButton(Button selected)
    {
        easyButton.image.color = normalColor;
        mediumButton.image.color = normalColor;
        hardButton.image.color = normalColor;
        selected.image.color = selectedColor;
    }

    public void SetEasy() { difficulty = BotDifficulty.Easy; HighlightButton(easyButton); }
    public void SetMedium() { difficulty = BotDifficulty.Medium; HighlightButton(mediumButton); }
    public void SetHard() { difficulty = BotDifficulty.Hard; HighlightButton(hardButton); }

    private bool huntMode = false;
    private Vector2Int lastHit;
    private Vector2Int firstHit;
    private Vector2Int huntDirection;
    private bool directionLocked;
    private int shipCounter = 1;

    void Start()
    {
        // Оптимізація для мобільних: фіксуємо frame rate, щоб не перегрівати батарею
        Application.targetFrameRate = 60;

        playerGrid = new int[_rows, _columns];
        enemyGrid = new int[_rows, _columns];
        playerShipId = new int[_rows, _columns];
        enemyShipId = new int[_rows, _columns];

        playerButtons = new Button[_rows, _columns];
        enemyButtons = new Button[_rows, _columns];
        playerImages = new Image[_rows, _columns];
        enemyImages = new Image[_rows, _columns];

        GenerateShips(playerGrid, playerShipId);
        GenerateShips(enemyGrid, enemyShipId);

        CreateGrid(_leftGrid, false);
        CreateGrid(_rightGrid, true);

        RefreshGrid(false);
        RefreshGrid(true);

        endPanel.SetActive(false);
        winText.SetActive(false);
        loseText.SetActive(false);
        restartButton.SetActive(false);
        if (mainMenuButton != null) mainMenuButton.SetActive(false);

        HighlightButton(mediumButton);

        if (startBattleButton != null)
            startBattleButton.gameObject.SetActive(true);
    }

    public void StartBattle()
    {
        if (battleStarted) return;
        battleStarted = true;
        isPlayerTurn = true;

        if (startBattleButton != null) startBattleButton.gameObject.SetActive(false);
        if (reshuffleButton != null) reshuffleButton.gameObject.SetActive(false);

        easyButton.gameObject.SetActive(false);
        mediumButton.gameObject.SetActive(false);
        hardButton.gameObject.SetActive(false);
    }

    public void ReshufflePlayers()
    {
        if (battleStarted) return;
        shipCounter = 1;
        playerGrid = new int[_rows, _columns];
        playerShipId = new int[_rows, _columns];
        enemyGrid = new int[_rows, _columns];
        enemyShipId = new int[_rows, _columns];

        GenerateShips(playerGrid, playerShipId);
        GenerateShips(enemyGrid, enemyShipId);

        RefreshGrid(false);
        RefreshGrid(true);
    }

    void CreateGrid(Transform parent, bool isEnemy)
    {
        for (int x = 0; x < _rows; x++)
        {
            for (int y = 0; y < _columns; y++)
            {
                GameObject cell = Instantiate(_cellPrefab, parent);
                RectTransform rt = cell.GetComponent<RectTransform>();
                rt.anchoredPosition = new Vector2(x * _cellSize, -y * _cellSize);

                Button btn = cell.GetComponent<Button>();
                Image img = cell.GetComponent<Image>();

                if (isEnemy)
                {
                    enemyButtons[x, y] = btn;
                    enemyImages[x, y] = img;
                }
                else
                {
                    playerButtons[x, y] = btn;
                    playerImages[x, y] = img;
                }

                int cx = x;
                int cy = y;

                if (isEnemy)
                    btn.onClick.AddListener(() => OnCellClicked(cx, cy));
            }
        }
    }

    void OnCellClicked(int x, int y)
    {
        if (gameOver || !isPlayerTurn || !battleStarted) return;
        if (enemyGrid[x, y] >= 2) return;

        if (enemyGrid[x, y] == 1)
        {
            enemyGrid[x, y] = 2;
            SpawnExplosion(x, y, _rightGrid);
            int id = enemyShipId[x, y];
            if (IsShipDestroyed(enemyGrid, enemyShipId, id))
                SinkShip(enemyGrid, id);
        }
        else
        {
            enemyGrid[x, y] = 3;
        }

        RefreshGrid(true);

        if (AllShipsDestroyed(enemyGrid))
        {
            EndGame(true);
            return;
        }

        if (enemyGrid[x, y] == 3)
        {
            isPlayerTurn = false;
            Invoke(nameof(BotTurn), 0.5f);
        }
    }

    void BotTurn()
    {
        if (gameOver) return;
        Vector2Int shot;

        if (difficulty == BotDifficulty.Easy) shot = GetRandomShot();
        else if (difficulty == BotDifficulty.Medium) shot = huntMode ? GetHuntShot() : GetRandomShot();
        else shot = huntMode ? GetHardShot() : GetRandomShot();

        int x = shot.x; int y = shot.y;

        if (playerGrid[x, y] == 1)
        {
            playerGrid[x, y] = 2;
            SpawnExplosion(x, y, _leftGrid);

            if (!huntMode)
            {
                firstHit = shot;
                huntDirection = Vector2Int.zero;
                directionLocked = false;
            }
            lastHit = shot;
            huntMode = true;

            int id = playerShipId[x, y];
            if (IsShipDestroyed(playerGrid, playerShipId, id))
            {
                SinkShip(playerGrid, id);
                huntMode = false;
                directionLocked = false;
            }

            RefreshGrid(false);

            if (AllShipsDestroyed(playerGrid))
            {
                EndGame(false);
                return;
            }
            Invoke(nameof(BotTurn), 0.5f);
            return;
        }
        else
        {
            playerGrid[x, y] = 3;
            if (difficulty == BotDifficulty.Hard && huntMode && directionLocked)
            {
                huntDirection = -huntDirection;
                lastHit = firstHit;
            }
            isPlayerTurn = true;
        }
        RefreshGrid(false);
    }

    Vector2Int GetRandomShot()
    {
        List<Vector2Int> freeCells = new List<Vector2Int>();
        for (int x = 0; x < _rows; x++)
            for (int y = 0; y < _columns; y++)
                if (playerGrid[x, y] < 2) freeCells.Add(new Vector2Int(x, y));
        return freeCells[Random.Range(0, freeCells.Count)];
    }

    Vector2Int GetHuntShot()
    {
        Vector2Int[] dirs = { new(1, 0), new(-1, 0), new(0, 1), new(0, -1) };
        foreach (var d in dirs)
        {
            int nx = lastHit.x + d.x; int ny = lastHit.y + d.y;
            if (nx >= 0 && ny >= 0 && nx < _rows && ny < _columns)
                if (playerGrid[nx, ny] < 2) return new Vector2Int(nx, ny);
        }
        return GetRandomShot();
    }

    Vector2Int GetHardShot()
    {
        Vector2Int[] directions = { new(1, 0), new(-1, 0), new(0, 1), new(0, -1) };
        if (directionLocked)
        {
            int nx = lastHit.x + huntDirection.x; int ny = lastHit.y + huntDirection.y;
            if (nx >= 0 && ny >= 0 && nx < _rows && ny < _columns && playerGrid[nx, ny] < 2) return new Vector2Int(nx, ny);

            huntDirection = -huntDirection; lastHit = firstHit;
            nx = lastHit.x + huntDirection.x; ny = lastHit.y + huntDirection.y;
            if (nx >= 0 && ny >= 0 && nx < _rows && ny < _columns && playerGrid[nx, ny] < 2) return new Vector2Int(nx, ny);
        }
        foreach (var v in directions)
        {
            int nx = lastHit.x + v.x; int ny = lastHit.y + v.y;
            if (nx >= 0 && ny >= 0 && nx < _rows && ny < _columns && playerGrid[nx, ny] < 2)
            {
                huntDirection = v; directionLocked = true;
                return new Vector2Int(nx, ny);
            }
        }
        return GetRandomShot();
    }

    void SpawnExplosion(int x, int y, Transform parent)
    {
        if (explosionPrefab == null) return;
        GameObject fx = Instantiate(explosionPrefab, parent);
        fx.transform.localPosition = new Vector2(x * _cellSize, -y * _cellSize);
        Destroy(fx, 1f);
    }

    void EndGame(bool playerWon)
    {
        gameOver = true; isPlayerTurn = false;
        CancelInvoke();

        if (_leftGrid != null) _leftGrid.gameObject.SetActive(false);
        if (_rightGrid != null) _rightGrid.gameObject.SetActive(false);

        endPanel.SetActive(true);
        winText.SetActive(playerWon);
        loseText.SetActive(!playerWon);
        restartButton.SetActive(true);
        if (mainMenuButton != null) mainMenuButton.SetActive(true);
    }

    public void RestartGame() { SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); }
    public void GoToMainMenu() { SceneManager.LoadScene(0); }

    void RefreshGrid(bool isEnemy)
    {
        int[,] grid = isEnemy ? enemyGrid : playerGrid;
        Image[,] images = isEnemy ? enemyImages : playerImages;

        for (int x = 0; x < _rows; x++)
        {
            for (int y = 0; y < _columns; y++)
            {
                switch (grid[x, y])
                {
                    case 4: images[x, y].color = new Color(1f, 0.5f, 0f); break;
                    case 2: images[x, y].color = Color.red; break;
                    case 3: images[x, y].color = Color.blue; break;
                    case 1: images[x, y].color = isEnemy ? Color.white : Color.gray; break;
                    default: images[x, y].color = Color.white; break;
                }
            }
        }
    }

    void SinkShip(int[,] grid, int id)
    {
        int[,] shipIdMatrix = (grid == enemyGrid) ? enemyShipId : playerShipId;
        for (int x = 0; x < _rows; x++)
        {
            for (int y = 0; y < _columns; y++)
            {
                if (shipIdMatrix[x, y] == id)
                {
                    grid[x, y] = 4;
                    for (int ox = -1; ox <= 1; ox++)
                    {
                        for (int oy = -1; oy <= 1; oy++)
                        {
                            int sx = x + ox; int sy = y + oy;
                            if (sx >= 0 && sy >= 0 && sx < _rows && sy < _columns)
                                if (grid[sx, sy] == 0) grid[sx, sy] = 3;
                        }
                    }
                }
            }
        }
    }

    void GenerateShips(int[,] grid, int[,] shipId)
    {
        int[] ships = { 4, 3, 3, 2, 2, 2, 1, 1, 1, 1 };
        foreach (int size in ships) PlaceShip(grid, shipId, size);
    }

    bool PlaceShip(int[,] grid, int[,] shipId, int size)
    {
        int tries = 200;
        while (tries-- > 0)
        {
            bool horizontal = Random.value > 0.5f;
            int x = Random.Range(0, _rows); int y = Random.Range(0, _columns);

            if (CanPlace(grid, x, y, size, horizontal))
            {
                int id = shipCounter++;
                for (int i = 0; i < size; i++)
                {
                    int nx = x + (horizontal ? i : 0); int ny = y + (horizontal ? 0 : i);
                    grid[nx, ny] = 1; shipId[nx, ny] = id;
                }
                return true;
            }
        }
        return false;
    }

    bool CanPlace(int[,] grid, int x, int y, int size, bool horizontal)
    {
        for (int i = 0; i < size; i++)
        {
            int nx = x + (horizontal ? i : 0); int ny = y + (horizontal ? 0 : i);
            if (nx < 0 || ny < 0 || nx >= _rows || ny >= _columns) return false;

            for (int ox = -1; ox <= 1; ox++)
            {
                for (int oy = -1; oy <= 1; oy++)
                {
                    int sx = nx + ox; int sy = ny + oy;
                    if (sx >= 0 && sy >= 0 && sx < _rows && sy < _columns)
                        if (grid[sx, sy] == 1) return false;
                }
            }
        }
        return true;
    }

    bool AllShipsDestroyed(int[,] grid)
    {
        for (int x = 0; x < _rows; x++)
            for (int y = 0; y < _columns; y++)
                if (grid[x, y] == 1) return false;
        return true;
    }

    bool IsShipDestroyed(int[,] grid, int[,] shipId, int id)
    {
        for (int x = 0; x < _rows; x++)
            for (int y = 0; y < _columns; y++)
                if (shipId[x, y] == id && grid[x, y] == 1) return false;
        return true;
    }
}