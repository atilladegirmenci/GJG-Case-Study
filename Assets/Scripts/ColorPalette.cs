using UnityEngine;

[CreateAssetMenu(fileName = "NewColor", menuName = "Game/Color")]
public class ColorPalette : ScriptableObject
{
    public string colorName;
    [Header("SPRITES")]
    public Sprite defaultIcon;
    public Sprite IconA;
    public Sprite IconB;
    public Sprite IconC;

    //[Header("VISUAL EFFECTS")]
    //for spawning correct colored effect. this will be addded!!

}
