using System.Collections.Generic;
using System.Dynamic;
using System.Linq;

public sealed class UpdateManager
{
    private static UpdateManager instance = new();
    public static UpdateManager Instance => instance;
    public static HashSet<(int, int)> cellsToUpdate = new ();

    //private Element[,] currentGrid;

    // public void setCurrentGrid(Element[,] grid)
    // {
    //     // dont forget to set the current grid in the main loop each frame, before cell updates
    //     currentGrid = grid;
    // }

    public void ClearUpdateRequests()
    {
        cellsToUpdate.Clear();
    }

    public (int, int)[] GetUpdateRequests()
    {
        return cellsToUpdate.ToArray();
    }

    public HashSet<(int, int)> getHashSet()
    {
        return new HashSet<(int, int)>(cellsToUpdate);
    }

    public void RequestUpdateNextFrame(int x, int y)
    {
        cellsToUpdate.Add((x, y));
    }

    public void UpdateNearbyCellsNextFrame(int x, int y, int maxX, int maxY)
    {
        // if (currentGrid == null)
        // {
        //     throw new InvalidOperationException("Current grid is not set. (set it in the main loop, before cell updates)");
        // }

        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                int nx = x + dx;
                int ny = y + dy;

                if (nx >= 0 && nx < maxX && ny >= 0 && ny < maxY)
                {
                    cellsToUpdate.Add((nx, ny));
                }
            }
        }
    }
}