using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

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
        int maxAllowedCount = 7;

#if UNITY_EDITOR
        // Search the entire project for assets of type "ColorPalette".
        // The filter string "t:ColorPalette" tells Unity to look for that specific type.
        string[] guids = AssetDatabase.FindAssets("t:ColorPalette");

        maxAllowedCount = guids.Length;
#endif

        if (availableColors.Count > maxAllowedCount)
        {
            Debug.LogWarning($"[LevelConfig] Limit exceeded! Found {maxAllowedCount} ColorPalette assets in the project. Removing extra items from {name}.");

            while (availableColors.Count > maxAllowedCount)
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
