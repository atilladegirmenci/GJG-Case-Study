using UnityEngine;

public class BlockContext : MonoBehaviour
{
    public int X { get; private set; }
    public int Y { get; private set; }

    public void SetCoordinates(int x, int y)
    {
        X = x;
        Y = y;
    }
}