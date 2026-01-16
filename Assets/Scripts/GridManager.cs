using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class GridManager : MonoBehaviour
{
    [Header("Core References")]
    [SerializeField] private LevelConfig currentLevelConfig;
    [SerializeField] private GameObject blockPrefab;
    [SerializeField] private Transform boardContainer;

    [Header("Board Dimensions")]
    [SerializeField] private float targetBoardWidth;
    [SerializeField] private float targetBoardHeight;

    [Header("Spacing & Animation")]
    [SerializeField] private float spaceBetweenCols;
    [SerializeField] private float spaceBetweenRows;
    [SerializeField] private float moveTime;

    // Private State
    private GridNode[,] _grid;
    private float _cellSize;
    private Vector2 _boardOffset;
    private InputManager _inputManager; // Cached reference

    // Public Properties
    public float CellSize => _cellSize;
    public Vector2 BoardOffset => _boardOffset;

    // Direction array for neighbors (Up, Down, Left, Right)
    private readonly Vector2Int[] _directions = new Vector2Int[]
    {
        new Vector2Int(0, 1),
        new Vector2Int(0, -1),
        new Vector2Int(-1, 0),
        new Vector2Int(1, 0)
    };

    private void Awake()
    {
        _inputManager = FindAnyObjectByType<InputManager>();
    }

    private void Start()
    {
        if (currentLevelConfig == null)
        {
            Debug.LogError("GridManager: Level config is not assigned.");
            return;
        }

        CalculateBoardLayout();
        CreateGrid();
        UpdateAllGroupVisuals();
    }

    #region Grid Generation

    private void CalculateBoardLayout()
    {
        int rows = currentLevelConfig.rows;
        int cols = currentLevelConfig.cols;

        // Calculate total gaps
        float totalGapWidth = (cols - 1) * spaceBetweenCols;
        float totalGapHeight = (rows - 1) * spaceBetweenRows;

        // Calculate cell size ensuring it fits both width and height
        float availableWidth = targetBoardWidth - totalGapWidth;
        float availableHeight = targetBoardHeight - totalGapHeight;
        float widthPerCell = availableWidth / cols;
        float heightPerCell = availableHeight / rows;

        _cellSize = Mathf.Min(widthPerCell, heightPerCell);

        // Calculate centering offset
        float totalBoardWidth = (cols * _cellSize) + ((cols - 1) * spaceBetweenCols);
        float totalBoardHeight = (rows * _cellSize) + ((rows - 1) * spaceBetweenRows);
        float startX = -(totalBoardWidth / 2f) + (_cellSize / 2f);
        float startY = -(totalBoardHeight / 2f) + (_cellSize / 2f);

        _boardOffset = new Vector2(startX, startY);
    }

    private void CreateGrid()
    {
        int rows = currentLevelConfig.rows;
        int cols = currentLevelConfig.cols;

        _grid = new GridNode[cols, rows];

        for (int x = 0; x < cols; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                // Create Logic Node
                int randomColorIndex = Random.Range(0, currentLevelConfig.availableColors.Count);
                GridNode node = new GridNode(x, y, randomColorIndex) { isEmpty = false };
                _grid[x, y] = node;

                Vector3 targetPos = GetWorldPosition(x, y);

                // Create Visual Block
                SpawnBlockView(node, x, y, targetPos);
            }
        }
    }

    private BlockView SpawnBlockView(GridNode node, int x, int y, Vector3 spawnPosition)
    {
        // Instantiate at the specific 'spawnPosition' requested by the caller
        GameObject blockObj = Instantiate(blockPrefab, spawnPosition, Quaternion.identity, boardContainer);
        blockObj.name = $"Block_{x}_{y}";

        // Scale adjustment
        blockObj.transform.localScale = Vector3.one * _cellSize / 2;

        BlockView view = blockObj.GetComponent<BlockView>();

        // Get sprite and initialize
        var palette = currentLevelConfig.availableColors[node.colorIndex];
        view.Init(spawnPosition, palette.defaultIcon);

        // Setup Context for Raycast
        BlockContext context = blockObj.GetComponent<BlockContext>();
        if (context == null) context = blockObj.AddComponent<BlockContext>();
        context.SetCoordinates(x, y);

        // Link Logic and View
        node.assignedView = view;

        // Return the created view so the caller can animate it if needed
        return view;
    }

    #endregion

    #region Input & Logic

    public void OnBlockClicked(int x, int y)
    {
        if (!IsInsideBounds(x, y)) return;

        GridNode startNode = _grid[x, y];
        if (startNode.isEmpty) return;

        // Run Flood Fill
        List<GridNode> matches = GetMatches(startNode);

        //Minimum 2 blocks
        if (matches.Count >= 2)
        {
            BlastBlocks(matches);
        }
    }

    private List<GridNode> GetMatches(GridNode startNode)
    {
        List<GridNode> result = new List<GridNode>();
        int targetColorIndex = startNode.colorIndex;

        Queue<GridNode> queue = new Queue<GridNode>();
        HashSet<GridNode> visited = new HashSet<GridNode>();

        queue.Enqueue(startNode);
        visited.Add(startNode);

        while (queue.Count > 0)
        {
            GridNode current = queue.Dequeue();
            result.Add(current);

            foreach (Vector2Int dir in _directions)
            {
                int nextX = current.x + dir.x;
                int nextY = current.y + dir.y;

                if (IsInsideBounds(nextX, nextY))
                {
                    GridNode neighbor = _grid[nextX, nextY];

                    if (!visited.Contains(neighbor) && !neighbor.isEmpty && neighbor.colorIndex == targetColorIndex)
                    {
                        queue.Enqueue(neighbor);
                        visited.Add(neighbor);
                    }
                }
            }
        }
        return result;
    }

    private void BlastBlocks(List<GridNode> nodesToBlast)
    {
        foreach (GridNode node in nodesToBlast)
        {
            node.isEmpty = true;

            if (node.assignedView != null)
            {
                node.assignedView.OnBlast();
                node.assignedView = null;
            }
        }

        StartCoroutine(ApplyGravityRoutine());
    }

    #endregion

    #region Gravity & Refill

    private IEnumerator ApplyGravityRoutine()
    {
        // Lock Input
        if (_inputManager != null) _inputManager.SetInputActive(false);

        yield return new WaitForSeconds(0.1f); // Wait for blast frame

        int rows = currentLevelConfig.rows;
        int cols = currentLevelConfig.cols;

        // PHASE 1: SHIFT DOWN
        for (int x = 0; x < cols; x++)
        {
            List<GridNode> livingBlocks = new List<GridNode>();

            // Collect surviving blocks
            for (int y = 0; y < rows; y++)
            {
                if (!_grid[x, y].isEmpty)
                {
                    livingBlocks.Add(_grid[x, y]);
                }
            }

            // Rebuild the column
            for (int y = 0; y < rows; y++)
            {
                if (y < livingBlocks.Count)
                {
                    // Existing block moved or stayed
                    GridNode node = livingBlocks[y];
                    _grid[x, y] = node;
                    node.isEmpty = false;

                    // If position changed, update Logic, View, and Context
                    if (node.y != y)
                    {
                        node.x = x;
                        node.y = y;

                        Vector3 targetPos = GetWorldPosition(x, y);
                        node.assignedView.MoveToPosition(targetPos, moveTime);

                        // Update Context for Raycast
                        var context = node.assignedView.GetComponent<BlockContext>();
                        if (context != null) context.SetCoordinates(x, y);

                        node.assignedView.name = $"Block_{x}_{y}";
                    }
                }
                else
                {
                    // Empty slot
                    GridNode emptyNode = new GridNode(x, y, -1) { isEmpty = true };
                    _grid[x, y] = emptyNode;
                }
            }
        }

        yield return new WaitForSeconds(moveTime);

        // Visual Update 1: Show new groups formed by falling blocks
        UpdateAllGroupVisuals();

        RefillBoard();

        yield return new WaitForSeconds(moveTime);

        // Visual Update 2: Show new groups formed by new blocks
        UpdateAllGroupVisuals();

        // Unlock Input
        if (_inputManager != null) _inputManager.SetInputActive(true);
    }

    private void RefillBoard()
    {
        int rows = currentLevelConfig.rows;
        int cols = currentLevelConfig.cols;

        for (int x = 0; x < cols; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                if (_grid[x, y].isEmpty)
                {
                    // Logic Creation
                    int randomColor = Random.Range(0, currentLevelConfig.availableColors.Count);
                    GridNode newNode = new GridNode(x, y, randomColor) { isEmpty = false };
                    _grid[x, y] = newNode;

                    // Calculate Positions
                    Vector3 targetPos = GetWorldPosition(x, y);
                    Vector3 startPos = targetPos + Vector3.up * 5f; // Offset for falling effect

                    // Spawn above the board and GET the view back
                    BlockView view = SpawnBlockView(newNode, x, y, startPos);

                    // Now command the view to move (Animation Logic belongs here)
                    view.MoveToPosition(targetPos, moveTime);
                }
            }
        }
    }

    #endregion

    #region Visualization & Helpers

    private void UpdateAllGroupVisuals()
    {
        int rows = currentLevelConfig.rows;
        int cols = currentLevelConfig.cols;
        bool[,] globalVisited = new bool[cols, rows];

        for (int x = 0; x < cols; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                if (!globalVisited[x, y] && !_grid[x, y].isEmpty)
                {
                    GridNode startNode = _grid[x, y];
                    List<GridNode> group = GetMatches(startNode);

                    foreach (GridNode node in group)
                    {
                        globalVisited[node.x, node.y] = true;
                    }

                    UpdateGroupSprites(group);
                }
            }
        }
    }

    private void UpdateGroupSprites(List<GridNode> group)
    {
        int count = group.Count;
        if (count == 0) return;

        int colorIndex = group[0].colorIndex;
        var palette = currentLevelConfig.availableColors[colorIndex];
        Sprite targetSprite = palette.defaultIcon;

        // Dynamic Visual Logic
        if (count >= 10) targetSprite = palette.IconC;
        else if (count >= 8) targetSprite = palette.IconB;
        else if (count > 4) targetSprite = palette.IconA;

        foreach (GridNode node in group)
        {
            if (node.assignedView != null)
            {
                node.assignedView.SetSprite(targetSprite);
            }
        }
    }

    public Vector3 GetWorldPosition(int x, int y)
    {
        float xPos = x * (_cellSize + spaceBetweenCols) + _boardOffset.x;
        float yPos = y * (_cellSize + spaceBetweenRows) + _boardOffset.y;
        float zDepth = -y * 0.1f;

        Vector3 localPos = new Vector3(xPos, yPos, zDepth);

        // Convert to container's world space if parent exists
        if (boardContainer != null)
        {
            return boardContainer.TransformPoint(localPos);
        }

        return localPos; // if not use world
    }

    public bool IsInsideBounds(int x, int y)
    {
        return x >= 0 && x < currentLevelConfig.cols &&
                 y >= 0 && y < currentLevelConfig.rows;
    }

    #endregion

#if UNITY_EDITOR
    public void GenerateGridForEditor()
    {
        ClearGrid();
        if (currentLevelConfig != null) CalculateBoardLayout();
        CreateGrid();
    }

    public void ClearGrid()
    {
        if (boardContainer != null)
        {
            while (boardContainer.childCount > 0)
            {
                DestroyImmediate(boardContainer.GetChild(0).gameObject);
            }
        }
    }
#endif
}