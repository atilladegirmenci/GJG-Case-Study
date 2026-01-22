
[System.Serializable]
public class GridNode
{
    public int x, y;
    public int colorIndex;
    public bool isEmpty = true;
    public BlockView assignedView;

    public GridNode(int x, int y, int colorIndex)
    {
        this.colorIndex = colorIndex;
        this.x = x;
        this.y = y;
    }

}
