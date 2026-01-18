using UnityEngine;
using TMPro;
using DG.Tweening;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [Header("Text References")]
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI movesText;
    [SerializeField] private TextMeshProUGUI multiplierText;

    [Header("Animation Settings")]
    [SerializeField] private float punchScaleAmount = 0.3f;
    [SerializeField] private float punchDuration = 0.2f;

    [Header("Game Over Panel")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TextMeshProUGUI finalScoreText;
    [SerializeField] private TextMeshProUGUI highScoreText;
    [SerializeField] private GameObject newRecordObject;

    [Header("Sound Button Settings")]
    [SerializeField] private Image soundButtonImage;
    [SerializeField] private Sprite soundOnSprite;
    [SerializeField] private Sprite soundOffSprite;

    private bool _isMuted = false;

    // Store initial scales to preserve scene setup
    private Vector3 _baseScoreScale;
    private Vector3 _baseMovesScale;
    private Vector3 _baseMultiScale;

    private void Awake()
    {
        if (scoreText) _baseScoreScale = scoreText.transform.localScale;
        if (movesText) _baseMovesScale = movesText.transform.localScale;
        if (multiplierText) _baseMultiScale = multiplierText.transform.localScale;

        if (gameOverPanel) gameOverPanel.SetActive(false);
    }

    private void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnScoreChanged += UpdateScoreUI;
            GameManager.Instance.OnMovesChanged += UpdateMovesUI;
            GameManager.Instance.OnMultiplierChanged += UpdateMultiplierUI;
            GameManager.Instance.OnGameOver += ShowGameOverUI;

            // Sync initial state
            UpdateScoreUI(GameManager.Instance.Score);
            UpdateMovesUI(GameManager.Instance.MovesLeft);
            UpdateMultiplierUI(GameManager.Instance.CurrentMultiplier);

            UpdateSoundButtonVisual();
        }
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnScoreChanged -= UpdateScoreUI;
            GameManager.Instance.OnMovesChanged -= UpdateMovesUI;
            GameManager.Instance.OnMultiplierChanged -= UpdateMultiplierUI;
            GameManager.Instance.OnGameOver -= ShowGameOverUI;
        }

        // Kill Tweens to prevent errors on scene reload
        if (scoreText) scoreText.transform.DOKill();
        if (movesText) movesText.transform.DOKill();
        if (multiplierText) multiplierText.transform.DOKill();

        if (gameOverPanel) gameOverPanel.transform.DOKill();
        if (newRecordObject) newRecordObject.transform.DOKill();
    }

    private void UpdateScoreUI(int newScore)
    {
        if (scoreText == null) return;

        scoreText.text = newScore.ToString();
        AnimateText(scoreText.transform, _baseScoreScale);
    }

    private void UpdateMovesUI(int remainingMoves)
    {
        if (movesText == null) return;

        movesText.text = remainingMoves.ToString();

        // Turn red if moves are low
        movesText.color = remainingMoves <= 5 ? Color.red : Color.white;

        AnimateText(movesText.transform, _baseMovesScale);
    }

    private void UpdateMultiplierUI(float multiplier)
    {
        if (multiplierText == null) return;

        multiplierText.text = $"x{multiplier:F1}";

        if (multiplier <= 1.0f)
        {
            multiplierText.alpha = 0.5f;
            multiplierText.transform.localScale = _baseMultiScale;
        }
        else
        {
            multiplierText.alpha = 1f;
            AnimateText(multiplierText.transform, _baseMultiScale);
        }
    }

    private void AnimateText(Transform target, Vector3 baseScale)
    {
        target.DOKill();
        target.localScale = baseScale;
        target.DOPunchScale(Vector3.one * punchScaleAmount, punchDuration, 1, 0.5f);
    }

    private void ShowGameOverUI(bool isNewRecord)
    {
        if (gameOverPanel == null) return;

        gameOverPanel.SetActive(true);

        // Pop-in Animation
        gameOverPanel.transform.localScale = Vector3.zero;
        gameOverPanel.transform.DOScale(1f, 0.5f).SetEase(Ease.OutBack);

        if (finalScoreText) finalScoreText.text = $"SCORE: {GameManager.Instance.Score}";
        if (highScoreText) highScoreText.text = $"HIGH SCORE: {GameManager.Instance.HighScore}";

        if (newRecordObject)
        {
            newRecordObject.SetActive(isNewRecord);
            if (isNewRecord)
            {
                // Pulse animation
                newRecordObject.transform.DOScale(1.2f, 0.5f).SetLoops(-1, LoopType.Yoyo);
            }
        }
    }

    #region Button Actions

    public void OnSoundButtonClicked()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.ToggleMute();
            _isMuted = !_isMuted;
            UpdateSoundButtonVisual();
        }
    }

    private void UpdateSoundButtonVisual()
    {
        if (soundButtonImage == null) return;

        // Toggle sprite based on mute state
        soundButtonImage.sprite = _isMuted ? soundOffSprite : soundOnSprite;
    }

    public void OnExitButtonClicked()
    {
        Debug.Log("Exiting Game...");
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    #endregion
}