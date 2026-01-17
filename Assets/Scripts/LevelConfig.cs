using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewLevelConfig", menuName = "Game/LevelConfig")]
public class LevelConfig : ScriptableObject
{
    [Header("Grid Settings")]
    [Range(2, 10)]
    public int rows;
    [Range(2, 10)]
    public int cols;
    public List<ColorPalette> availableColors;

    [Header("Game Rules")]
    public int maxMoves = 30;

    public int K => availableColors.Count;

    private void OnValidate()
    {
        // 1. KURAL: Maksimum 6 Renk
        if (availableColors.Count > 6)
        {
            Debug.LogWarning($"[LevelConfig] You can't add more than 6 colors! Removed extra items from {name}.");

            // Fazlalıkları sondan sil
            while (availableColors.Count > 6)
            {
                availableColors.RemoveAt(availableColors.Count - 1);
            }
        }

        // 2. KURAL: Aynı Renk Tekrarı (Duplicate) Kontrolü
        // Designer yanlışlıkla iki kere "Mavi" eklemiş mi?
        ValidateDuplicates();
    }

    private void ValidateDuplicates()
    {
        if (availableColors == null) return;

        // Benzersizleri bulmak için HashSet kullanıyoruz
        HashSet<ColorPalette> uniqueSet = new HashSet<ColorPalette>();

        foreach (var item in availableColors)
        {
            if (item != null)
            {
                if (uniqueSet.Contains(item))
                {
                    Debug.LogError($"[LevelConfig] DANGER: Duplicate color detected in {name}! Please remove one of them.");
                }
                else
                {
                    uniqueSet.Add(item);
                }
            }
        }
    }


}
