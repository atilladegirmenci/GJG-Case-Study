using UnityEngine;
using System;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private LevelConfig levelConfig;
    [SerializeField] private float comboTimeout = 2.0f;

    private readonly float[] _multiplierLevels = { 1.0f, 1.1f, 1.2f, 1.5f, 2.0f, 3.0f, 5.0f };

    // State
    public int Score { get; private set; }
    public int MovesLeft { get; private set; }

    private int _currentMultiplierIndex = 0;
    public float CurrentMultiplier => _multiplierLevels[_currentMultiplierIndex];

    public event Action<int> OnScoreChanged;
    public event Action<int> OnMovesChanged;
    public event Action<float> OnMultiplierChanged;

    private float _lastMoveTime;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        StartGame();
    }

    private void Update()
    {
        HandleMultiplierDecay();
    }

    public void StartGame()
    {
        MovesLeft = (levelConfig != null) ? levelConfig.maxMoves : 20;
        Score = 0;
        _currentMultiplierIndex = 0;
        _lastMoveTime = Time.time;

        OnMovesChanged?.Invoke(MovesLeft);
        OnScoreChanged?.Invoke(Score);
        OnMultiplierChanged?.Invoke(CurrentMultiplier);
    }

    private void HandleMultiplierDecay()
    {
        if (_currentMultiplierIndex == 0) return;

        // Check for timeout
        if (Time.time - _lastMoveTime > comboTimeout)
        {
            _currentMultiplierIndex--;
            _lastMoveTime = Time.time;

            OnMultiplierChanged?.Invoke(CurrentMultiplier);
        }
    }

    public bool TryUseMove()
    {
        if (MovesLeft <= 0)
        {
            Debug.Log("Game Over! No moves left."); // TODO: Trigger Game Over UI
            return false;
        }

        MovesLeft--;
        OnMovesChanged?.Invoke(MovesLeft);

        // Check if the move was fast enough to keep/increase the combo
        if (Time.time - _lastMoveTime <= comboTimeout)
        {
            if (_currentMultiplierIndex < _multiplierLevels.Length - 1)
            {
                _currentMultiplierIndex++;
            }
        }
        else
        {
            // Timeout: Reset combo to base level
            _currentMultiplierIndex = 0;
        }

        _lastMoveTime = Time.time;

        OnMultiplierChanged?.Invoke(CurrentMultiplier);
        return true;
    }

    public void AddScore(int blockCount)
    {
        int basePoints = blockCount * 10;

        // Group Size Bonus
        float sizeBonus = 1.0f;
        if (blockCount >= 5) sizeBonus = 1.5f;
        if (blockCount >= 8) sizeBonus = 2.0f;
        if (blockCount >= 10) sizeBonus = 3.0f;

        int totalPoints = Mathf.FloorToInt(basePoints * sizeBonus * CurrentMultiplier);

        Score += totalPoints;
        OnScoreChanged?.Invoke(Score);
    }
}