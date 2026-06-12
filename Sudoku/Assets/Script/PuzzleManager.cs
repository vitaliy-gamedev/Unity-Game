using UnityEngine;

public class PuzzleManager : MonoBehaviour
{
    public Tile tilePrefab;
    public GameObject winText;
    public GameObject boardObject;

    private const int SIZE = 4;

    public float cellSize = 100f;
    public float cellSpacing = 5f;

    private Tile[,] board = new Tile[SIZE, SIZE];

    private int emptyX;
    private int emptyY;

    void Start()
    {
        GenerateBoard();
        Shuffle(200);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.UpArrow))
            EmptyMove(0, 1);

        if (Input.GetKeyDown(KeyCode.DownArrow))
            EmptyMove(0, -1);

        if (Input.GetKeyDown(KeyCode.LeftArrow))
            EmptyMove(1, 0);

        if (Input.GetKeyDown(KeyCode.RightArrow))
            EmptyMove(-1, 0);
    }

    private void GenerateBoard()
    {
        int number = 1;

        for (int y = 0; y < SIZE; y++)
        {
            for (int x = 0; x < SIZE; x++)
            {
                if (x == SIZE - 1 && y == SIZE - 1)
                {
                    emptyX = x;
                    emptyY = y;
                    continue;
                }

                Tile tile = Instantiate(tilePrefab, transform);
                tile.SetNumber(number);
                board[x, y] = tile;
                number++;
            }
        }

        UpdateVisuals();
    }

    private void EmptyMove(int dx, int dy, bool updateVisuals = true)
    {
        int targetX = emptyX + dx;
        int targetY = emptyY + dy;

        if (targetX < 0 || targetX >= SIZE) return;
        if (targetY < 0 || targetY >= SIZE) return;

        board[emptyX, emptyY] = board[targetX, targetY];
        board[targetX, targetY] = null;

        emptyX = targetX;
        emptyY = targetY;

        if (updateVisuals)
        {
            UpdateVisuals();
            if (CheckWin()) OnWin();
        }
    }

    private void UpdateVisuals()
    {
        float startX = -((SIZE - 1) * (cellSize + cellSpacing)) / 2f;
        float startY = ((SIZE - 1) * (cellSize + cellSpacing)) / 2f;

        for (int y = 0; y < SIZE; y++)
        {
            for (int x = 0; x < SIZE; x++)
            {
                if (board[x, y] == null) continue;

                RectTransform rect = board[x, y].GetComponent<RectTransform>();
                rect.anchoredPosition = new Vector2(
                    startX + x * (cellSize + cellSpacing),
                    startY - y * (cellSize + cellSpacing)
                );
            }
        }
    }

    private void Shuffle(int moves)
    {
        Vector2Int[] dirs =
        {
            new Vector2Int(0, -1),
            new Vector2Int(0, 1),
            new Vector2Int(-1, 0),
            new Vector2Int(1, 0),
        };

        for (int i = 0; i < moves; i++)
        {
            Vector2Int dir = dirs[Random.Range(0, dirs.Length)];
            EmptyMove(dir.x, dir.y, false);
        }

        UpdateVisuals();
    }

    private bool CheckWin()
    {
        int expected = 1;

        for (int y = 0; y < SIZE; y++)
        {
            for (int x = 0; x < SIZE; x++)
            {
                if (x == SIZE - 1 && y == SIZE - 1)
                    return board[x, y] == null;

                if (board[x, y] == null) return false;
                if (board[x, y].Number != expected) return false;

                expected++;
            }
        }

        return true;
    }

    private void OnWin()
    {
        boardObject.SetActive(false);
        winText.SetActive(true);
    }
}