using UnityEngine;

[CreateAssetMenu(fileName = "NewColorPalette", menuName = "Game/BlockColor")]
public class ColorPalette : ScriptableObject
{
    public string colorName;
    [Header("SPRITES")]
    public Sprite defaultIcon;
    public Sprite IconA;
    public Sprite IconB;
    public Sprite IconC;

    [Header("VISUAL EFFECTS")]
    public Color particleColor;

}
