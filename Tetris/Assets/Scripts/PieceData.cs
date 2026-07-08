using UnityEngine;

public static class PieceData
{
    public static readonly Vector2[][] Shapes = new Vector2[][]
    {
        new Vector2[] { new Vector2(-2, 0), new Vector2(-1, 0), new Vector2(0, 0), new Vector2(1, 0) },
        new Vector2[] { new Vector2(-1, 0), new Vector2(0, 0), new Vector2(-1, 1), new Vector2(0, 1) },
        new Vector2[] { new Vector2(-1, 0), new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1) },
        new Vector2[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(-1, 1), new Vector2(0, 1) },
        new Vector2[] { new Vector2(-1, 0), new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 1) },
        new Vector2[] { new Vector2(-1, 0), new Vector2(0, 0), new Vector2(1, 0), new Vector2(-1, 1) },
        new Vector2[] { new Vector2(-1, 0), new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1) },
    };

    public static readonly Color[] Colors = new Color[]
    {
        Color.cyan,
        Color.yellow,
        new Color(0.6f, 0.2f, 1f),
        new Color(0f, 0.8f, 0.2f),
        Color.red,
        Color.blue,
        new Color(1f, 0.5f, 0f),
    };

    public static Vector2[] Rotate(Vector2[] cells, bool clockwise)
    {
        Vector2[] rotated = new Vector2[cells.Length];
        for (int i = 0; i < cells.Length; i++)
        {
            if (clockwise)
                rotated[i] = new Vector2(cells[i].y, -cells[i].x);
            else
                rotated[i] = new Vector2(-cells[i].y, cells[i].x);
        }
        return rotated;
    }
}