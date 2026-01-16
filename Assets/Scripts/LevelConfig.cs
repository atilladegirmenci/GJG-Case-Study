using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewLevelConfig", menuName = "Game/LevelConfig")]
public class LevelConfig : ScriptableObject
{
    [Range(2, 10)]
    public int rows;
    [Range(2, 10)]
    public int cols;
    public List<ColorPalette> availableColors;
    public int K => availableColors.Count;


}
