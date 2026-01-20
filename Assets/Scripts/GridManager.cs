using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using DG.Tweening;

public class GridManager : MonoBehaviour
{
    [Header("Core References")]
    [SerializeField] private LevelConfig currentLevelConfig;
    [SerializeField] private Transform _boardContainer;

    [Header("Board Dimensions")]
    [SerializeField] private float targetBoardWidth;
    [SerializeField] public float targetBoardHeight;
    [SerializeField] private SpriteRenderer boardBackground;
    [SerializeField] private float boardBackgroundPadding;

    [Header("Spacing & Animation")]
    [SerializeField] private float spaceBetweenCols;
    [SerializeField] private float spaceBetweenRows;
    [SerializeField] private float fallTime;
    [SerializeField] private GameObject blastEffectPrefab;

    [Header("Debug Settings")]
    public bool showGridPreview = true;

    private GridNode[,] _grid;
    private float _cellSize;
    private Vector2 _boardOffset;
    private InputManager _inputManager;

    // public float CellSize => _cellSize;
    //public Vector2 BoardOffset => _boardOffset;



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

        int totalCells = currentLevelConfig.rows * currentLevelConfig.cols;

        // Grid boyutu kadar + %25 yedek (refill animasyonları sırasında lazım olabilir)
        int poolSize = Mathf.CeilToInt(totalCells * 1.25f);

        if (BlockPool.Instance != null)
        {
            BlockPool.Instance.InitializePool(poolSize);
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

        if (boardBackground != null)
        {
            float bgWidth = totalBoardWidth + (boardBackgroundPadding * 2);
            float bgHeight = totalBoardHeight + (boardBackgroundPadding * 2);

            boardBackground.size = new Vector2(bgWidth, bgHeight);

            boardBackground.transform.localPosition = new Vector3(0, 0, 2);
        }
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
        BlockView view = BlockPool.Instance.GetBlock();

        // Setup Transform
        view.transform.SetParent(_boardContainer);
        view.transform.position = spawnPosition;
        view.transform.rotation = Quaternion.identity;
        view.name = $"Block_{x}_{y}";
        view.transform.localScale = Vector3.one * _cellSize / 2;

        // Get sprite and initialize
        var palette = currentLevelConfig.availableColors[node.colorIndex];
        view.Init(spawnPosition, palette.defaultIcon);

        // Setup Context for Raycast
        BlockContext context = view.GetComponent<BlockContext>();
        if (context == null) context = view.gameObject.AddComponent<BlockContext>();
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

        List<GridNode> matches = GetMatches(startNode);
        if (matches.Count < 2) return;

        if (GameManager.Instance != null)
        {
            bool canMove = GameManager.Instance.TryUseMove();
            if (!canMove) return; // No moves left!
        }

        BlastBlocks(matches);
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
        int colorIndex = nodesToBlast[0].colorIndex;
        ColorPalette sharedPalette = currentLevelConfig.availableColors[colorIndex];

        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddScore(nodesToBlast.Count);
        }
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayBlastSound(nodesToBlast.Count);
        }

        foreach (GridNode node in nodesToBlast)
        {
            node.isEmpty = true;

            if (blastEffectPrefab != null)
            {
                // Instantiate at block position
                Vector3 pos = GetWorldPosition(node.x, node.y);
                GameObject particleObj = Instantiate(blastEffectPrefab, pos + new Vector3(0, 0, -5), Quaternion.identity);

                ParticleSystem ps = particleObj.GetComponent<ParticleSystem>();
                if (ps != null)
                {
                    var main = ps.main;
                    main.startColor = sharedPalette.particleColor; // Set the color dynamically
                }
            }

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

        int fallingBlockCount = 0;

        // SHIFT DOWN
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
                        node.assignedView.MoveToPosition(targetPos, fallTime);

                        // Update Context for Raycast
                        var context = node.assignedView.GetComponent<BlockContext>();
                        if (context != null) context.SetCoordinates(x, y);

                        node.assignedView.name = $"Block_{x}_{y}";

                        fallingBlockCount++;
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

        if (fallingBlockCount > 0)
        {
            // Play sound when they land (after fallTime)
            DOVirtual.DelayedCall(fallTime, () =>
            {
                if (AudioManager.Instance) AudioManager.Instance.PlayDropSound(fallingBlockCount);
            });
        }

        yield return new WaitForSeconds(fallTime + 0.1f);

        // Visual Update 1: Show new groups formed by falling blocks
        UpdateAllGroupVisuals();

        RefillBoard();

        yield return new WaitForSeconds(fallTime + 0.1f);

        // Visual Update 2: Show new groups formed by new blocks
        bool hasMoves = UpdateAllGroupVisuals();

        // if deadlock => shuffle
        if (!hasMoves)
        {
            yield return new WaitForSeconds(0.5f);
            ShuffleBoard();
            yield return new WaitForSeconds(0.5f);
        }


        if (GameManager.Instance.MovesLeft <= 0)
        {
            yield return new WaitForSeconds(1f);

            GameManager.Instance.CheckGameEnd();
        }
        else
        {
            if (_inputManager != null) _inputManager.SetInputActive(true);
        }
    }

    private void RefillBoard()
    {
        int rows = currentLevelConfig.rows;
        int cols = currentLevelConfig.cols;

        int newBlockCount = 0;

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
                    view.MoveToPosition(targetPos, fallTime);

                    newBlockCount++;
                }
            }
        }
        if (newBlockCount > 0)
        {
            // Play sound when they land (after fallTime)
            DOVirtual.DelayedCall(fallTime, () =>
            {
                if (AudioManager.Instance) AudioManager.Instance.PlayDropSound(newBlockCount);
            });
        }
    }
    private void ShuffleBoard()
    {
        Debug.Log("Deadlock detected! Shuffling board...");

        int rows = currentLevelConfig.rows;
        int cols = currentLevelConfig.cols;

        List<GridNode> activeNodes = new List<GridNode>();
        List<int> colors = new List<int>();

        // Collect active nodes and colors
        for (int x = 0; x < cols; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                if (!_grid[x, y].isEmpty)
                {
                    activeNodes.Add(_grid[x, y]);
                    colors.Add(_grid[x, y].colorIndex);
                }
            }
        }

        // Fisher-Yates Shuffle
        for (int i = 0; i < colors.Count; i++)
        {
            int temp = colors[i];
            int randomIndex = Random.Range(i, colors.Count);
            colors[i] = colors[randomIndex];
            colors[randomIndex] = temp;
        }

        // Apply new colors and animate
        for (int i = 0; i < activeNodes.Count; i++)
        {
            GridNode node = activeNodes[i];
            node.colorIndex = colors[i];

            if (node.assignedView != null)
            {
                var palette = currentLevelConfig.availableColors[node.colorIndex];

                // Reset to default icon before calculating groups
                node.assignedView.SetSprite(palette.defaultIcon);

                // Shake feedback
                node.assignedView.transform.DOPunchRotation(Vector3.forward * 20, 0.5f);
            }
        }

        // Re-calculate groups and check for valid moves
        bool hasValidMoves = UpdateAllGroupVisuals();

        // Retry if still deadlocked
        if (!hasValidMoves) ShuffleBoard();
    }

    #endregion

    #region Visualization & Helpers

    private bool UpdateAllGroupVisuals()
    {
        int rows = currentLevelConfig.rows;
        int cols = currentLevelConfig.cols;
        bool[,] globalVisited = new bool[cols, rows];

        bool hasAnyValidMove = false;

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

                    if (group.Count >= 2)
                    {
                        hasAnyValidMove = true; // no deadlock
                    }
                }
            }
        }
        return hasAnyValidMove;
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
        if (_boardContainer != null)
        {
            return _boardContainer.TransformPoint(localPos);
        }

        return localPos; // if not use world
    }

    public bool IsInsideBounds(int x, int y)
    {
        return x >= 0 && x < currentLevelConfig.cols &&
                 y >= 0 && y < currentLevelConfig.rows;
    }

    private void OnDrawGizmos()
    {
        if (!showGridPreview || currentLevelConfig == null) return;

        int rows = currentLevelConfig.rows;
        int cols = currentLevelConfig.cols;

        float totalGapW = (cols - 1) * spaceBetweenCols;
        float totalGapH = (rows - 1) * spaceBetweenRows;
        float availW = targetBoardWidth - totalGapW;
        float availH = targetBoardHeight - totalGapH;

        if (cols == 0 || rows == 0) return;

        float size = Mathf.Min(availW / cols, availH / rows);

        float totalW = (cols * size) + totalGapW;
        float totalH = (rows * size) + totalGapH;
        float startX = -(totalW / 2f) + (size / 2f);
        float startY = -(totalH / 2f) + (size / 2f);
        Vector2 offset = new Vector2(startX, startY);

        Gizmos.color = Color.green;

        for (int x = 0; x < cols; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                float xPos = x * (size + spaceBetweenCols) + offset.x;
                float yPos = y * (size + spaceBetweenRows) + offset.y;

                Vector3 center = new Vector3(xPos, yPos, 0);

                if (_boardContainer != null) center = _boardContainer.TransformPoint(center);
                else center += transform.position;

                Gizmos.DrawWireCube(center, Vector3.one * size);
            }
        }

        Gizmos.color = new Color(0, 1, 1, 0.3f); // Cyan
        Vector3 boardCenter = (_boardContainer != null) ? _boardContainer.position : transform.position;
        Gizmos.DrawCube(boardCenter, new Vector3(targetBoardWidth, targetBoardHeight, 1));
    }

    #endregion

#if UNITY_EDITOR
    public void ForceDeadlockPattern()
    {
        // make sure there is only 2 colors
        if (currentLevelConfig.availableColors.Count < 2)
        {
            Debug.LogError("only 2 colors needed for activating the deadlock cheat");
            return;
        }

        int rows = currentLevelConfig.rows;
        int cols = currentLevelConfig.cols;

        // make grid checkered
        for (int x = 0; x < cols; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                if (_grid[x, y].isEmpty) continue;

                int cheatColorIndex = (x + y) % 2;

                _grid[x, y].colorIndex = cheatColorIndex;

                if (_grid[x, y].assignedView != null)
                {
                    var palette = currentLevelConfig.availableColors[cheatColorIndex];
                    _grid[x, y].assignedView.SetSprite(palette.defaultIcon);

                    _grid[x, y].assignedView.transform.localScale = Vector3.one * _cellSize / 2;
                }
            }
        }

        Debug.Log("CHEAT ACTIVE: board has deadlocked");

        bool hasMoves = UpdateAllGroupVisuals();
        if (!hasMoves)
        {
            Debug.Log("system validated its a deadlock");

            ShuffleBoard();
        }
    }
#endif
}