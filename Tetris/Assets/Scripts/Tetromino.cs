using UnityEngine;

public class Tetromino : MonoBehaviour
{
    [HideInInspector] public int pieceIndex;
    [HideInInspector] public Vector2[] cells;
    [HideInInspector] public Color color;

    private Transform[] blockObjects = new Transform[4];
    private GridManager gridManager;
    private Sprite blockSprite;


    private const int O_PIECE_INDEX = 1;

    private static readonly Vector2[] WallKicks = new Vector2[]
    {
        Vector2.zero,
        Vector2.left,
        Vector2.right,
        new Vector2(-2, 0),
        new Vector2(2, 0),
        Vector2.up,
        Vector2.down,
        new Vector2(0, 2),
        new Vector2(0, -2),
    };

    public void Initialize(int index, Vector2 spawnPos, GridManager gm, Sprite sprite)
    {
        pieceIndex = index;
        cells = PieceData.Shapes[index];
        color = PieceData.Colors[index];
        gridManager = gm;
        blockSprite = sprite;

        transform.position = spawnPos;

        for (int i = 0; i < 4; i++)
        {
            GameObject block = new GameObject("Cell");
            block.transform.SetParent(transform);
            block.transform.localPosition = cells[i];
            block.transform.localScale = Vector3.one;

            SpriteRenderer sr = block.AddComponent<SpriteRenderer>();
            sr.sprite = blockSprite;
            sr.color = color;

            blockObjects[i] = block.transform;
        }
    }

    public bool CanFitAtPosition(Vector2 pos)
    {
        return gridManager.CanFit(cells, pos);
    }

    public bool TryMove(Vector2 direction)
    {
        Vector2 newPos = (Vector2)transform.position + direction;
        if (gridManager.CanFit(cells, newPos))
        {
            transform.position = newPos;
            return true;
        }
        return false;
    }

    public bool TryRotate(bool clockwise = true)
    {
       
        if (pieceIndex == O_PIECE_INDEX)
            return true;

        Vector2[] rotated = PieceData.Rotate(cells, clockwise);

        foreach (Vector2 kick in WallKicks)
        {
            Vector2 newPos = (Vector2)transform.position + kick;
            if (gridManager.CanFit(rotated, newPos))
            {
                transform.position = newPos;
                cells = rotated;
                UpdateBlockPositions();
                return true;
            }
        }
        return false;
    }

    private void UpdateBlockPositions()
    {
        for (int i = 0; i < 4; i++)
            blockObjects[i].localPosition = cells[i];
    }

    public int HardDrop()
    {
        int steps = 0;
        while (TryMove(Vector2.down))
            steps++;
        return steps;
    }

    public void Land()
    {
        gridManager.PlaceBlocks(cells, transform.position, color, blockSprite);
        GameManager.Instance.OnPieceLanded();
        Destroy(gameObject);
    }
}