using UnityEngine;

public class GridManager : MonoBehaviour
{
    public int width = 10;
    public int height = 20;

    [HideInInspector] public Transform[,] grid;

    void Awake()
    {
        grid = new Transform[width, height];
    }

    public bool IsInside(int x, int y)
    {
        return x >= 0 && x < width && y >= 0 && y < height;
    }

    public bool IsOccupied(int x, int y)
    {
        return IsInside(x, y) && grid[x, y] != null;
    }

    public bool CanFit(Vector2[] cells, Vector2 pos)
    {
        foreach (Vector2 offset in cells)
        {
            Vector2 world = pos + offset;
            int x = Mathf.FloorToInt(world.x + 0.5f);
            int y = Mathf.FloorToInt(world.y + 0.5f);
            if (!IsInside(x, y) || grid[x, y] != null)
                return false;
        }
        return true;
    }

    public Transform CreateBlock(int x, int y, Color color, Sprite sprite)
    {
        if (!IsInside(x, y)) return null;

        GameObject block = new GameObject("Block");
        block.transform.position = new Vector3(x, y, 0);
        block.transform.localScale = Vector3.one;

        SpriteRenderer sr = block.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = color;
        sr.sortingLayerName = "Default";

        grid[x, y] = block.transform;
        return block.transform;
    }

    public void PlaceBlocks(Vector2[] cells, Vector2 piecePos, Color color, Sprite sprite)
    {
        foreach (Vector2 offset in cells)
        {
            Vector2 world = piecePos + offset;
            int x = Mathf.FloorToInt(world.x + 0.5f);
            int y = Mathf.FloorToInt(world.y + 0.5f);
            CreateBlock(x, y, color, sprite);
        }
    }

    private bool IsRowFull(int y)
    {
        for (int x = 0; x < width; x++)
            if (grid[x, y] == null) return false;
        return true;
    }

    private void ClearRow(int y)
    {
        for (int x = 0; x < width; x++)
        {
            if (grid[x, y] != null)
            {
                Destroy(grid[x, y].gameObject);
                grid[x, y] = null;
            }
        }
    }

    private void ShiftRowDown(int y)
    {
        for (int x = 0; x < width; x++)
        {
            if (grid[x, y] != null)
            {
                grid[x, y].position += Vector3.down;
                grid[x, y - 1] = grid[x, y];
                grid[x, y] = null;
            }
        }
    }

    public int ClearFullRows()
    {
        int cleared = 0;
        for (int y = 0; y < height; y++)
        {
            if (IsRowFull(y))
            {
                ClearRow(y);
                cleared++;
                for (int yy = y + 1; yy < height; yy++)
                    ShiftRowDown(yy);
                y--;
            }
        }
        return cleared;
    }

    public bool SpawnGarbageLine(Sprite sprite)
    {
        for (int x = 0; x < width; x++)
        {
            if (grid[x, height - 1] != null)
                return false;
        }

        for (int y = height - 1; y > 0; y--)
        {
            for (int x = 0; x < width; x++)
            {
                grid[x, y] = grid[x, y - 1];
                if (grid[x, y] != null)
                    grid[x, y].position += Vector3.up;
            }
        }

        for (int x = 0; x < width; x++)
            grid[x, 0] = null;

        int holes = Random.Range(1, 4);
        bool[] isHole = new bool[width];
        for (int i = 0; i < holes; i++)
            isHole[Random.Range(0, width)] = true;

        for (int x = 0; x < width; x++)
        {
            if (!isHole[x])
                CreateBlock(x, 0, Color.gray, sprite);
        }

        return true;
    }
}