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
        if (availableColors.Count > 6)
        {
            Debug.LogWarning($"[LevelConfig] You can't add more than 6 colors! Removed extra items from {name}.");

            while (availableColors.Count > 6)
            {
                availableColors.RemoveAt(availableColors.Count - 1);
            }
        }

        ValidateDuplicates();
    }

    private void ValidateDuplicates()
    {
        if (availableColors == null) return;

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
