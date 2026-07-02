using UnityEngine;

[CreateAssetMenu(fileName = "NewTileConfig", menuName = "Match3/TileConfig")]
public class TileTypeConfig : ScriptableObject
{
    [System.Serializable]
    public struct TileData
    {
        public TileType type;
        public Sprite sprite;
        public Color color;
        public int scoreValue;
    }

    public TileData[] tiles;

    public TileData GetData(TileType type)
    {
        foreach (TileData tile in tiles)
        {
            if (tile.type == type) return tile;
        }
        return tiles[0];
    }

    public TileType GetRandomType()
    {
        return tiles[Random.Range(0, tiles.Length)].type;
    }

    public Sprite GetSprite(TileType type)
    {
        return GetData(type).sprite;
    }

    public Color GetColor(TileType type)
    {
        return GetData(type).color;
    }
}

public enum TileType
{
    Red,
    Blue,
    Green,
    Yellow,
    Purple,
    Orange
}
