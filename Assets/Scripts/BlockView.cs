using UnityEngine;
using System.Collections;
using DG.Tweening;

public class BlockView : MonoBehaviour
{

    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("VISUAL EFFECETS")]
    [SerializeField] private float jellyEffectMagnitude = 0.05f;
    [SerializeField] private float jellyEffectDuration = 0.1f;
    [SerializeField] private float disappearDuration = 0.2f;


    private void Awake()
    {
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void Init(Vector3 startPos, Sprite sprite)
    {
        transform.position = startPos;
        SetSprite(sprite);
        gameObject.SetActive(true);
        transform.DOKill();
    }

    // Visual update for icon changes
    public void SetSprite(Sprite newSprite)
    {
        if (spriteRenderer.sprite == newSprite) return;

        spriteRenderer.sprite = newSprite;
        transform.DOPunchScale(Vector3.one * 0.05f, 0.15f, 10, 1);
    }

    // Gravity animation
    public void MoveToPosition(Vector3 targetPos, float duration)
    {
        transform.DOKill();
        transform.DOMove(targetPos, duration)
            .SetEase(Ease.InQuad)
            .OnComplete(() =>
            {
                transform.DOPunchScale(new Vector3(jellyEffectMagnitude, -jellyEffectMagnitude, 0), jellyEffectDuration, 10, 1);
            });
    }
    // Called on explosion
    public void OnBlast()
    {
        transform.DOKill();

        var col = GetComponent<Collider2D>();
        if (col) col.enabled = false;

        //particle effects wil be added here 
        transform.DOScale(Vector3.zero, disappearDuration)
            .SetEase(Ease.InBack)
            .OnComplete(() =>
            {
                if (BlockPool.Instance != null)
                {
                    // Re-enable collider for next use
                    if (col) col.enabled = true;
                    BlockPool.Instance.ReturnBlock(this);
                }
                else
                {
                    Destroy(gameObject); // Fallback
                }
            });

    }
    private void OnDestroy()
    {
        transform.DOKill();
    }

}