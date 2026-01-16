using UnityEngine;
using System.Collections;
using DG.Tweening;

public class BlockView : MonoBehaviour
{

    [SerializeField] private SpriteRenderer spriteRenderer;

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
        transform.DOMove(targetPos, duration).SetEase(Ease.OutBounce);
    }
    // Called on explosion
    public void OnBlast()
    {
        transform.DOKill();

        //particle effects wil be added here 
        transform.DOScale(Vector3.zero, 0.1f)
            .SetEase(Ease.InBack)
            .OnComplete(() =>
            {
                Destroy(gameObject);
            });

    }
    private void OnDestroy()
    {
        transform.DOKill();
    }

}