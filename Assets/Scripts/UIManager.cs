using UnityEngine;
using TMPro;
using DG.Tweening;

public class UIManager : MonoBehaviour
{
    [Header("Text References")]
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI movesText;
    [SerializeField] private TextMeshProUGUI multiplierText;

    [Header("Animation Settings")]
    [SerializeField] private float punchScaleAmount = 0.3f;
    [SerializeField] private float punchDuration = 0.2f;

    // Store initial scales to preserve scene setup
    private Vector3 _baseScoreScale;
    private Vector3 _baseMovesScale;
    private Vector3 _baseMultiScale;

    private void Awake()
    {
        if (scoreText) _baseScoreScale = scoreText.transform.localScale;
        if (movesText) _baseMovesScale = movesText.transform.localScale;
        if (multiplierText) _baseMultiScale = multiplierText.transform.localScale;
    }

    private void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnScoreChanged += UpdateScoreUI;
            GameManager.Instance.OnMovesChanged += UpdateMovesUI;
            GameManager.Instance.OnMultiplierChanged += UpdateMultiplierUI;

            // Sync initial state
            UpdateScoreUI(GameManager.Instance.Score);
            UpdateMovesUI(GameManager.Instance.MovesLeft);
            UpdateMultiplierUI(GameManager.Instance.CurrentMultiplier);
        }
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnScoreChanged -= UpdateScoreUI;
            GameManager.Instance.OnMovesChanged -= UpdateMovesUI;
            GameManager.Instance.OnMultiplierChanged -= UpdateMultiplierUI;
        }
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

        // Turn red if moves are low (<= 5)
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
}