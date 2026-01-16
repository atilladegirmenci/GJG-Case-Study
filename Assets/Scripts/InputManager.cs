using UnityEngine;

public class InputManager : MonoBehaviour
{
    [SerializeField] private GridManager gridManager;

    private Camera _mainCamera;
    private bool _isInputActive = true;

    private void Awake()
    {
        _mainCamera = Camera.main;

        if (gridManager == null)
            Debug.LogError("InputManager: GridManager reference is missing!");
    }

    private void Update()
    {
        if (!_isInputActive) return;

        if (Input.GetMouseButtonDown(0))
        {
            HandleRaycastInput();
        }
    }

    private void HandleRaycastInput()
    {
        Vector3 mouseWorldPos = _mainCamera.ScreenToWorldPoint(Input.mousePosition);
        Vector2 mousePos2D = new Vector2(mouseWorldPos.x, mouseWorldPos.y);

        RaycastHit2D hit = Physics2D.Raycast(mousePos2D, Vector2.zero);

        if (!hit.collider) return;

        if (hit.collider.TryGetComponent(out BlockContext context))
        {
            gridManager.OnBlockClicked(context.X, context.Y);
        }
    }

    /// <summary>
    /// Locks/Unlocks input. Called by GridManager during animations.
    /// </summary>
    public void SetInputActive(bool active)
    {
        _isInputActive = active;
    }
}