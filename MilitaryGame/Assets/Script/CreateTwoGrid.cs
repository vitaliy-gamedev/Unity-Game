using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class CreateTooGrid : MonoBehaviour
{
    [Header("Префаби")]
    [SerializeField] private GameObject _cellPrefab;
    [SerializeField] private GameObject explosionPrefab;

    [Header("Батьківські об'єкти (Сітки)")]
    [SerializeField] private Transform _leftGrid;
    [SerializeField] private Transform _rightGrid;

    [Header("UI Елементи")]
    [SerializeField] private GameObject endPanel;
    [SerializeField] private GameObject winText;
    [SerializeField] private GameObject loseText;
    [SerializeField] private GameObject restartButton;
    [SerializeField] private GameObject mainMenuButton;
    [SerializeField] private GameObject startBattleButton;
    [SerializeField] private Text rotationText;
    [SerializeField] private Button rotateMobileButton;
    [SerializeField] private Button autoPlaceButton; // <--- ПОВЕРНУТО: Кнопка для авторозстановки

    [Header("Налаштування")]
    [SerializeField] private int _rows = 10;
    [SerializeField] private int _columns = 10;
    [SerializeField] private float _cellSize = 50f;

    [Header("Кнопки складності")]
    [SerializeField] private Button easyButton;
    [SerializeField] private Button mediumButton;
    [SerializeField] private Button hardButton;

    private Color selectedColor = Color.yellow;
    private Color normalColor = Color.white;

    public enum BotDifficulty { Easy, Medium, Hard }
    private BotDifficulty difficulty = BotDifficulty.Medium;

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

    private int[,] playerGrid;
    private int[,] enemyGrid;
    private int[,] playerShipId;
    private int[,] enemyShipId;
    private Button[,] playerButtons;
    private Button[,] enemyButtons;
    private Image[,] playerImages;
    private Image[,] enemyImages;

    private bool battleStarted;
    private bool gameOver;

    private bool placementPhase = true;
    private int[] ships = { 4, 3, 3, 2, 2, 2, 1, 1, 1, 1 };
    private int shipIndex;
    private int shipCounter = 1;
    private bool horizontal = true;

    private bool hunt;
    private Vector2Int lastHit;
    private Vector2Int firstHit;
    private Vector2Int huntDirection;
    private bool directionLocked;

    private Vector2Int previewOrigin = new Vector2Int(-1, -1);
    private List<Vector2Int> previewCells = new List<Vector2Int>();

    void Start()
    {
        Application.targetFrameRate = 60;

        playerGrid = new int[_rows, _columns];
        enemyGrid = new int[_rows, _columns];
        playerShipId = new int[_rows, _columns];
        enemyShipId = new int[_rows, _columns];

        playerButtons = new Button[_rows, _columns];
        enemyButtons = new Button[_rows, _columns];
        playerImages = new Image[_rows, _columns];
        enemyImages = new Image[_rows, _columns];

        GenerateShips(enemyGrid, enemyShipId); // Ворог завжди авто

        CreateGrid(_leftGrid, false);
        CreateGrid(_rightGrid, true);

        Refresh(false);
        Refresh(true);

        endPanel.SetActive(false);
        if (mainMenuButton != null) mainMenuButton.SetActive(false);
        startBattleButton.SetActive(false);

        if (rotateMobileButton != null) rotateMobileButton.gameObject.SetActive(true);
        if (autoPlaceButton != null) autoPlaceButton.gameObject.SetActive(true);

        HighlightButton(mediumButton);
    }

    void Update()
    {
        if (!placementPhase) return;

        if (rotationText != null)
            rotationText.text = horizontal ? "Horizontal" : "Vertical";

        HandleMobilePreview();
    }

    public void ToggleRotationMobile()
    {
        if (!placementPhase) return;
        horizontal = !horizontal;
        ClearPreview();
    }

    // ================= ПОВЕРНУТО: МЕТОД АВТОРОЗСТАНОВКИ ДЛЯ ГРАВЦЯ =================
    public void AutoPlacePlayerShips()
    {
        if (!placementPhase) return;

        // Скидаємо стару розстановку, якщо вона була почата вручну
        playerGrid = new int[_rows, _columns];
        playerShipId = new int[_rows, _columns];
        shipCounter = 1;

        // Генеруємо випадкові кораблі для гравця
        foreach (int size in ships)
        {
            PlaceShipRandomly(playerGrid, playerShipId, size);
        }

        shipIndex = ships.Length; // Пропускаємо індекс до кінця, бо все виставлено
        placementPhase = false;

        ClearPreview();
        Refresh(false);

        // Ховаємо кнопки розстановки та показуємо кнопку старту бою
        if (rotateMobileButton != null) rotateMobileButton.gameObject.SetActive(false);
        if (autoPlaceButton != null) autoPlaceButton.gameObject.SetActive(false);
        startBattleButton.SetActive(true);
    }

    void CreateGrid(Transform parent, bool enemy)
    {
        for (int x = 0; x < _rows; x++)
        {
            for (int y = 0; y < _columns; y++)
            {
                GameObject c = Instantiate(_cellPrefab, parent);
                RectTransform rt = c.GetComponent<RectTransform>();
                rt.anchoredPosition = new Vector2(x * _cellSize, -y * _cellSize);

                Button b = c.GetComponent<Button>();
                Image img = c.GetComponent<Image>();

                int cx = x, cy = y;
                if (enemy)
                {
                    enemyButtons[x, y] = b;
                    enemyImages[x, y] = img;
                    b.onClick.AddListener(() => Shoot(cx, cy));
                }
                else
                {
                    playerButtons[x, y] = b;
                    playerImages[x, y] = img;
                    b.onClick.AddListener(() => Place(cx, cy));
                }
            }
        }
    }

    void HandleMobilePreview()
    {
        if (Input.touchCount == 0)
        {
            ClearPreview();
            return;
        }

        Touch touch = Input.GetTouch(0);
        Vector2 local;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _leftGrid as RectTransform,
            touch.position,
            null,
            out local
        );

        int x = Mathf.FloorToInt(local.x / _cellSize);
        int y = Mathf.FloorToInt(-local.y / _cellSize);

        if (x < 0 || y < 0 || x >= _rows || y >= _columns)
        {
            ClearPreview();
            return;
        }

        if (previewOrigin.x == x && previewOrigin.y == y)
            return;

        previewOrigin = new Vector2Int(x, y);
        DrawPreview(x, y);
    }

    void Place(int x, int y)
    {
        if (!placementPhase) return;
        int size = ships[shipIndex];

        if (!CanPlace(playerGrid, x, y, size, horizontal)) return;

        int id = shipCounter++;
        for (int i = 0; i < size; i++)
        {
            int nx = x + (horizontal ? i : 0); int ny = y + (horizontal ? 0 : i);
            playerGrid[nx, ny] = 1; playerShipId[nx, ny] = id;
        }

        shipIndex++;
        Refresh(false);
        ClearPreview();

        if (shipIndex >= ships.Length)
        {
            placementPhase = false;
            startBattleButton.SetActive(true);
            if (rotateMobileButton != null) rotateMobileButton.gameObject.SetActive(false);
            if (autoPlaceButton != null) autoPlaceButton.gameObject.SetActive(false);
        }
    }

    public void StartBattle()
    {
        if (placementPhase) return;
        battleStarted = true;
        startBattleButton.SetActive(false);

        if (easyButton != null) easyButton.gameObject.SetActive(false);
        if (mediumButton != null) mediumButton.gameObject.SetActive(false);
        if (hardButton != null) hardButton.gameObject.SetActive(false);
        if (rotationText != null) rotationText.gameObject.SetActive(false);
    }

    void DrawPreview(int x, int y)
    {
        ClearPreview();
        if (!placementPhase) return;

        int size = ships[shipIndex];
        bool ok = CanPlace(playerGrid, x, y, size, horizontal);
        Color c = ok ? new Color(0, 1, 0, 0.35f) : new Color(1, 0, 0, 0.35f);

        for (int i = 0; i < size; i++)
        {
            int nx = x + (horizontal ? i : 0); int ny = y + (horizontal ? 0 : i);
            if (nx < 0 || ny < 0 || nx >= _rows || ny >= _columns) continue;

            previewCells.Add(new Vector2Int(nx, ny));
            playerImages[nx, ny].color = c;
        }
    }

    void ClearPreview()
    {
        foreach (var p in previewCells)
        {
            if (playerGrid[p.x, p.y] == 0) playerImages[p.x, p.y].color = Color.white;
            else if (playerGrid[p.x, p.y] == 1) playerImages[p.x, p.y].color = Color.gray;
        }
        previewCells.Clear();
        previewOrigin = new Vector2Int(-1, -1);
    }

    void Shoot(int x, int y)
    {
        if (!battleStarted || gameOver) return;
        if (enemyGrid[x, y] >= 2) return;

        if (enemyGrid[x, y] == 1)
        {
            enemyGrid[x, y] = 2;
            Spawn(x, y, _rightGrid);
            int id = enemyShipId[x, y];
            if (IsDead(enemyGrid, enemyShipId, id)) SinkShip(enemyGrid, enemyShipId, id);
        }
        else
        {
            enemyGrid[x, y] = 3;
        }

        Refresh(true);

        if (Check(enemyGrid)) { End(true); return; }
        if (enemyGrid[x, y] == 3) Invoke(nameof(Bot), 0.5f);
    }

    void Bot()
    {
        if (gameOver) return;
        Vector2Int s;

        if (difficulty == BotDifficulty.Easy) s = RandomShot();
        else if (difficulty == BotDifficulty.Medium) s = hunt ? SmartShot() : RandomShot();
        else s = hunt ? HardShot() : RandomShot();

        int x = s.x; int y = s.y;

        if (playerGrid[x, y] == 1)
        {
            playerGrid[x, y] = 2; Spawn(x, y, _leftGrid);

            if (!hunt)
            {
                firstHit = s; huntDirection = Vector2Int.zero; directionLocked = false;
            }
            lastHit = s; hunt = true;

            int id = playerShipId[x, y];
            if (IsDead(playerGrid, playerShipId, id))
            {
                SinkShip(playerGrid, playerShipId, id);
                hunt = false; directionLocked = false;
            }

            Refresh(false);
            if (Check(playerGrid)) { End(false); return; }
            Invoke(nameof(Bot), 0.5f);
        }
        else
        {
            playerGrid[x, y] = 3;
            if (difficulty == BotDifficulty.Hard && hunt && directionLocked)
            {
                huntDirection = -huntDirection; lastHit = firstHit;
            }
        }
        Refresh(false);
    }

    Vector2Int RandomShot()
    {
        List<Vector2Int> free = new List<Vector2Int>();
        for (int x = 0; x < _rows; x++)
            for (int y = 0; y < _columns; y++)
                if (playerGrid[x, y] < 2) free.Add(new Vector2Int(x, y));
        return free[Random.Range(0, free.Count)];
    }

    Vector2Int SmartShot()
    {
        Vector2Int[] d = { new(1, 0), new(-1, 0), new(0, 1), new(0, -1) };
        foreach (var v in d)
        {
            int nx = lastHit.x + v.x; int ny = lastHit.y + v.y;
            if (nx >= 0 && ny >= 0 && nx < _rows && ny < _columns)
                if (playerGrid[nx, ny] < 2) return new Vector2Int(nx, ny);
        }
        return RandomShot();
    }

    Vector2Int HardShot()
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
        return RandomShot();
    }

    void Refresh(bool enemy)
    {
        int[,] g = enemy ? enemyGrid : playerGrid;
        Image[,] imgs = enemy ? enemyImages : playerImages;

        for (int x = 0; x < _rows; x++)
        {
            for (int y = 0; y < _columns; y++)
            {
                switch (g[x, y])
                {
                    case 4: imgs[x, y].color = new Color(1f, 0.5f, 0f); break;
                    case 2: imgs[x, y].color = Color.red; break;
                    case 3: imgs[x, y].color = Color.blue; break;
                    case 1: imgs[x, y].color = enemy ? Color.white : Color.gray; break;
                    default: imgs[x, y].color = Color.white; break;
                }
            }
        }
    }

    void GenerateShips(int[,] grid, int[,] id)
    {
        int[] shipsLocal = { 4, 3, 3, 2, 2, 2, 1, 1, 1, 1 };
        foreach (int size in shipsLocal) PlaceShipRandomly(grid, id, size);
    }

    bool PlaceShipRandomly(int[,] grid, int[,] id, int size)
    {
        int tries = 200;
        while (tries-- > 0)
        {
            bool h = Random.value > 0.5f;
            int x = Random.Range(0, _rows); int y = Random.Range(0, _columns);

            if (!CanPlace(grid, x, y, size, h)) continue;
            int shipId = shipCounter++;

            for (int i = 0; i < size; i++)
            {
                int nx = x + (h ? i : 0); int ny = y + (h ? 0 : i);
                grid[nx, ny] = 1; id[nx, ny] = shipId;
            }
            return true;
        }
        return false;
    }

    bool CanPlace(int[,] g, int x, int y, int size, bool h)
    {
        for (int i = 0; i < size; i++)
        {
            int nx = x + (h ? i : 0); int ny = y + (h ? 0 : i);
            if (nx < 0 || ny < 0 || nx >= _rows || ny >= _columns) return false;

            for (int ox = -1; ox <= 1; ox++)
            {
                for (int oy = -1; oy <= 1; oy++)
                {
                    int sx = nx + ox; int sy = ny + oy;
                    if (sx >= 0 && sy >= 0 && sx < _rows && sy < _columns)
                        if (g[sx, sy] == 1) return false;
                }
            }
        }
        return true;
    }

    bool Check(int[,] g)
    {
        for (int x = 0; x < _rows; x++)
            for (int y = 0; y < _columns; y++)
                if (g[x, y] == 1) return false;
        return true;
    }

    bool IsDead(int[,] g, int[,] id, int ship)
    {
        for (int x = 0; x < _rows; x++)
            for (int y = 0; y < _columns; y++)
                if (id[x, y] == ship && g[x, y] == 1) return false;
        return true;
    }

    void SinkShip(int[,] grid, int[,] id, int ship)
    {
        for (int x = 0; x < _rows; x++)
        {
            for (int y = 0; y < _columns; y++)
            {
                if (id[x, y] == ship)
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

    void Spawn(int x, int y, Transform p)
    {
        if (!explosionPrefab) return;
        var fx = Instantiate(explosionPrefab, p);
        fx.transform.localPosition = new Vector2(x * _cellSize, -y * _cellSize);
        Destroy(fx, 1f);
    }

    void End(bool win)
    {
        gameOver = true;
        if (_leftGrid != null) _leftGrid.gameObject.SetActive(false);
        if (_rightGrid != null) _rightGrid.gameObject.SetActive(false);
        if (rotationText != null) rotationText.gameObject.SetActive(false);
        if (startBattleButton != null) startBattleButton.SetActive(false);
        if (rotateMobileButton != null) rotateMobileButton.gameObject.SetActive(false);
        if (autoPlaceButton != null) autoPlaceButton.gameObject.SetActive(false);

        endPanel.SetActive(true);
        winText.SetActive(win);
        loseText.SetActive(!win);
        restartButton.SetActive(true);
        if (mainMenuButton != null) mainMenuButton.SetActive(true);
    }

    public void Restart() { SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); }
    public void GoToMainMenu() { SceneManager.LoadScene(0); }
}