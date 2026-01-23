using UnityEngine;
using System;
using UnityEngine.SceneManagement;
using System.Collections;
using Unity.Cinemachine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private LevelConfig levelConfig;
    [SerializeField] private float comboTimeout = 2.0f;
    [SerializeField] private float autoRestartDelay = 3.0f;
    [Header("Cameras")]
    [SerializeField] private CinemachineCamera farCamera;
    [SerializeField] private CinemachineCamera normalCamera;

    private readonly float[] _multiplierLevels = { 1.0f, 1.1f, 1.2f, 1.5f, 2.0f, 3.0f, 5.0f };

    public int Score { get; private set; }
    public int HighScore { get; private set; }
    public int MovesLeft { get; private set; }

    private bool _isGameOver = false;

    private int _currentMultiplierIndex = 0;
    public float CurrentMultiplier => _multiplierLevels[_currentMultiplierIndex];
    private InputManager _inputManager;

    public event Action<int> OnScoreChanged;
    public event Action<int> OnMovesChanged;
    public event Action<float> OnMultiplierChanged;
    public event Action<bool> OnGameOver;

    private float _lastMoveTime;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        _inputManager = FindAnyObjectByType<InputManager>();
        normalCamera.Priority = 10;
        farCamera.Priority = 11;
    }

    private void Start()
    {
        HighScore = PlayerPrefs.GetInt("HighScore", 0);

        StartGame();
    }

    private void Update()
    {
        if (!_isGameOver)
        {
            HandleMultiplierDecay();
        }
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
        normalCamera.Priority = 12;
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

        if (MovesLeft <= 0) return false;

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
    public void CheckGameEnd()
    {
        if (MovesLeft <= 0)
        {
            Debug.Log("Game Over!");
            StartCoroutine(EndGameRoutine());
        }
    }

    public void AddScore(int blockCount)
    {
        int basePoints = blockCount * 10;

        // Group Size Bonus
        float sizeBonus = 1.0f;
        if (blockCount >= 4) sizeBonus = 1.5f;
        if (blockCount >= 6) sizeBonus = 2.0f;
        if (blockCount >= 8) sizeBonus = 3.0f;

        int totalPoints = Mathf.FloorToInt(basePoints * sizeBonus * CurrentMultiplier);

        Score += totalPoints;
        OnScoreChanged?.Invoke(Score);
    }
    private IEnumerator EndGameRoutine()
    {
        _isGameOver = true;

        _inputManager.SetInputActive(false);
        yield return new WaitForSeconds(1.0f);

        // Check for High Score
        bool isNewRecord = false;
        if (Score > HighScore)
        {
            isNewRecord = true;
            HighScore = Score;
            PlayerPrefs.SetInt("HighScore", HighScore);
            PlayerPrefs.Save();
        }

        // Trigger UI Event
        OnGameOver?.Invoke(isNewRecord);

        // Wait and Auto Restart
        yield return new WaitForSeconds(autoRestartDelay);

        DG.Tweening.DOTween.KillAll();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}