using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class PuzzleGame : MonoBehaviour
{
    [Header("Image")]
    [SerializeField] private Texture2D sourceImage;

    [Header("Grid")]
    [Min(1)][SerializeField] private int columns = 4;
    [Min(1)][SerializeField] private int rows = 4;

    [Header("UI")]
    [SerializeField] private RectTransform board;
    [SerializeField] private RectTransform tray;
    [SerializeField] private RectTransform pieceLayer;

    [Header("Gameplay")]
    [SerializeField] private float snapDistance = 40f;
    [SerializeField] private bool buildOnStart = true;
    [SerializeField] private bool shuffleOnBuild = true;
    [SerializeField] private bool lockPlacedPieces = true;
    [SerializeField] private bool showBorders = true;
    [SerializeField] private Vector2 trayPadding = new Vector2(50f, 50f);

    [Header("Events")]
    public UnityEvent onPuzzleStarted;
    public UnityEvent onPuzzleCompleted;
    public UnityEvent<int, int> onProgressChanged;
    public UnityEvent<float> onTimerChanged;

    public bool IsPlaying { get; private set; }
    public bool IsCompleted { get; private set; }
    public float ElapsedTime { get; private set; }
    public int PlacedCount { get; private set; }
    public int TotalPieces { get; private set; }

    private readonly List<PuzzlePieceDrag> pieces = new List<PuzzlePieceDrag>();
    private Canvas canvas;

    private void Awake()
    {
        canvas = GetComponentInParent<Canvas>();
        EnsurePieceLayer();
    }

    private System.Collections.IEnumerator Start()
    {
        if (buildOnStart)
        {
            // Чекаємо до кінця кадру, поки UI розтягнеться під екран
            yield return new WaitForEndOfFrame();
            StartNewGame();
        }
    }

    private void Update()
    {
        if (!IsPlaying || IsCompleted)
            return;

        ElapsedTime += Time.deltaTime;
        onTimerChanged?.Invoke(ElapsedTime);
    }

    public void StartNewGame()
    {
        if (!ValidateSettings())
            return;

        ClearPieces();

        IsPlaying = true;
        IsCompleted = false;
        ElapsedTime = 0f;
        PlacedCount = 0;
        TotalPieces = columns * rows;

        BuildPieces();

        if (shuffleOnBuild)
            ShufflePieces();

        onPuzzleStarted?.Invoke();
        onProgressChanged?.Invoke(PlacedCount, TotalPieces);
        onTimerChanged?.Invoke(ElapsedTime);
    }

    public void RestartGame()
    {
        StartNewGame();
    }

    public void PauseGame()
    {
        if (IsCompleted)
            return;

        IsPlaying = false;
        SetPiecesInteractable(false);
    }

    public void ResumeGame()
    {
        if (IsCompleted || pieces.Count == 0)
            return;

        IsPlaying = true;
        SetPiecesInteractable(true);
    }

    public void SetSourceImage(Texture2D image, bool rebuild = true)
    {
        sourceImage = image;

        if (rebuild)
            StartNewGame();
    }

    public void SetGrid(int newColumns, int newRows, bool rebuild = true)
    {
        columns = Mathf.Max(1, newColumns);
        rows = Mathf.Max(1, newRows);

        if (rebuild)
            StartNewGame();
    }

    public void ShufflePieces()
    {
        foreach (PuzzlePieceDrag piece in pieces)
        {
            if (piece.IsPlaced)
                continue;

            piece.SetStartPosition(RandomPointInTray());
            piece.SetInteractable(IsPlaying);
        }
    }

    public void ClearPieces()
    {
        if (pieceLayer == null)
            return;

        for (int i = pieceLayer.childCount - 1; i >= 0; i--)
            Destroy(pieceLayer.GetChild(i).gameObject);

        pieces.Clear();
    }

    private void BuildPieces()
    {
        float boardW = board.rect.width;
        float boardH = board.rect.height;
        float cellW = boardW / columns;
        float cellH = boardH / rows;

        for (int row = 0; row < rows; row++)
        {
            for (int column = 0; column < columns; column++)
            {
                PuzzlePieceDrag piece = CreatePiece(column, row, cellW, cellH);
                pieces.Add(piece);
            }
        }
    }

    private PuzzlePieceDrag CreatePiece(int column, int row, float cellW, float cellH)
    {
        GameObject go = new GameObject($"Piece_{column}_{row}", typeof(RectTransform));
        RectTransform pieceRect = go.GetComponent<RectTransform>();
        pieceRect.SetParent(pieceLayer, false);
        pieceRect.anchorMin = new Vector2(0.5f, 0.5f);
        pieceRect.anchorMax = new Vector2(0.5f, 0.5f);
        pieceRect.pivot = new Vector2(0.5f, 0.5f);
        pieceRect.sizeDelta = new Vector2(cellW, cellH);

        RawImage image = go.AddComponent<RawImage>();
        image.texture = sourceImage;
        image.uvRect = GetUvRect(column, row);

        if (showBorders)
        {
            Outline outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.6f);
            outline.effectDistance = new Vector2(2f, 2f);
        }

        CanvasGroup canvasGroup = go.AddComponent<CanvasGroup>();
        PuzzlePieceDrag drag = go.AddComponent<PuzzlePieceDrag>();
        drag.Initialize(
            canvas,
            pieceLayer,
            canvasGroup,
            GetTargetPosition(column, row, cellW, cellH),
            snapDistance,
            lockPlacedPieces,
            HandlePiecePlaced
        );

        pieceRect.anchoredPosition = RandomPointInTray();
        return drag;
    }

    private Rect GetUvRect(int column, int row)
    {
        return new Rect(
            (float)column / columns,
            (float)(rows - 1 - row) / rows,
            1f / columns,
            1f / rows
        );
    }

    private Vector2 GetTargetPosition(int column, int row, float cellW, float cellH)
    {
        Vector2 cellLocal = new Vector2(
            board.rect.xMin + cellW * (column + 0.5f),
            board.rect.yMax - cellH * (row + 0.5f)
        );

        Vector2 world = board.TransformPoint(cellLocal);
        return WorldToLayer(world);
    }

    private void HandlePiecePlaced(PuzzlePieceDrag piece)
    {
        if (IsCompleted)
            return;

        PlacedCount++;
        onProgressChanged?.Invoke(PlacedCount, TotalPieces);

        if (PlacedCount >= TotalPieces)
            CompletePuzzle();
    }

    private void CompletePuzzle()
    {
        IsCompleted = true;
        IsPlaying = false;
        SetPiecesInteractable(false);
        onPuzzleCompleted?.Invoke();
        Debug.Log("Puzzle completed!");
    }

    private void SetPiecesInteractable(bool interactable)
    {
        foreach (PuzzlePieceDrag piece in pieces)
        {
            if (piece.IsPlaced && lockPlacedPieces)
                piece.SetInteractable(false);
            else
                piece.SetInteractable(interactable);
        }
    }

    private Vector2 RandomPointInTray()
    {
        float minX = tray.rect.xMin + trayPadding.x;
        float maxX = tray.rect.xMax - trayPadding.x;
        float minY = tray.rect.yMin + trayPadding.y; // -50 + 50 = 0
        float maxY = tray.rect.yMax - trayPadding.y;

        if (minX > maxX)
        {
            float center = tray.rect.center.x;
            minX = center;
            maxX = center;
        }

        if (minY > maxY)
        {
            float center = tray.rect.center.y;
            minY = center;
            maxY = center;
        }

        Vector2 world = tray.TransformPoint(new Vector2(
            UnityEngine.Random.Range(minX, maxX),
            UnityEngine.Random.Range(minY, maxY)
        ));

        return WorldToLayer(world);
    }

    private Vector2 WorldToLayer(Vector2 worldPos)
    {
        Camera cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? canvas.worldCamera
            : null;

        Vector2 screen = RectTransformUtility.WorldToScreenPoint(cam, worldPos);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(pieceLayer, screen, cam, out Vector2 local);
        return local;
    }

    private void EnsurePieceLayer()
    {
        if (pieceLayer != null)
            return;

        pieceLayer = (RectTransform)transform;
        pieceLayer.anchorMin = Vector2.zero;
        pieceLayer.anchorMax = Vector2.one;
        pieceLayer.pivot = new Vector2(0.5f, 0.5f);
        pieceLayer.offsetMin = Vector2.zero;
        pieceLayer.offsetMax = Vector2.zero;
    }

    private bool ValidateSettings()
    {
        if (sourceImage == null)
        {
            Debug.LogError("PuzzleGame: sourceImage is not assigned.", this);
            return false;
        }

        if (board == null || tray == null)
        {
            Debug.LogError("PuzzleGame: board and tray must be assigned.", this);
            return false;
        }

        if (pieceLayer == null)
            EnsurePieceLayer();

        columns = Mathf.Max(1, columns);
        rows = Mathf.Max(1, rows);
        snapDistance = Mathf.Max(1f, snapDistance);
        return true;
    }
}
