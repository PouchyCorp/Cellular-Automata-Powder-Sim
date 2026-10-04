using System.Collections.Generic;
using System;
using System.Linq;
using Godot;


public class MoveRequest
{
	public int x { get; set; }
	public int y { get; set; }
	public int movementX { get; set; }
	public int movementY { get; set; }

	public MoveRequest(int x, int y, int movementX, int movementY)
	{
		this.x = x;
		this.y = y;
		this.movementX = movementX;
		this.movementY = movementY;
	}

	// does not check if the element can move, only checks if the move is within bounds and if the movement is not zero
	// also the bounds of the x and y of the calling element are not checked (should cause problems only if there is an implementation error in the calling code) 
	public bool IsValid(int maxX, int maxY)
	{
		return (movementX != 0 || movementY != 0) && !(x + movementX < 0 || x + movementX >= maxX || y + movementY < 0 || y + movementY >= maxY);
	}

	public static bool canMoveOnElement(Element element, Element target)
	{
		if (target == null)
		{
			return true;
		}
		if (target is IGas)
		{
			if (element is IGas && element.density <= target.density)
			{ // we assume that the gas density is always less than any other element density
				return false;
			}
			return true;
		}
		if (target is ILiquid)
		{
			if (element is IGas)
			{
				return false;
			}
			if (element is ILiquid && element.density <= target.density)
			{
				return false;
			}
			return true;
		}
		if (element is Worm && target is Soil)
		{
			return true;
		}
		return false;
	}
	public static bool CanMove(Element[,] oldGrid, int x, int y, int movementX, int movementY, int maxX, int maxY)
	{
		int newX = x + movementX;
		int newY = y + movementY;

		if (newX < 0 || newX >= maxX || newY < 0 || newY >= maxY)
		{
			return false;
		}

		return canMoveOnElement(oldGrid[x, y], oldGrid[newX, newY]);
	}
}
public sealed class MoveManager
{
    private static MoveManager instance = new();
    public static MoveManager Instance => instance;

    private List<MoveRequest> requests = new();
    private HashSet<(int, int)> sources = new();
    private HashSet<(int, int)> destinations = new();

	private HashSet<(int, int)> blacklist = new(); // this blacklist is the initial position of moved elements (to prevent another element from moving into the same position in the same update cycle)

    private MoveManager() { }

    public bool AttemptMove(
        Element[,] oldGrid,
        int x, int y,
        int dx, int dy,
        int maxX, int maxY)
    {

		if (!MoveRequest.CanMove(oldGrid, x, y, dx, dy, maxX, maxY))
            return false; // purposefully not requesting an update for the element that failed to move
		
        if (sources.Contains((x, y)) || destinations.Contains((x + dx, y + dy)))
		{
			UpdateManager.Instance.RequestUpdateNextFrame(x, y); // so it can update again next frame if it was not able to move
			return false;
		}
        	
		int tx = x + dx;
        int ty = y + dy;

        requests.Add(new MoveRequest(x, y, dx, dy));
        sources.Add((x, y));
        destinations.Add((tx, ty));

        return true;
    }

    public void ProcessMoveRequests(
        Element[,] oldGrid,
        Element[,] currentGrid,
        int maxX, int maxY)
    {
        // Randomize the processing order to reduce directional bias.
        for (int i = requests.Count - 1; i > 0; i--)
        {
            int j = Random.Shared.Next(i + 1);
            (requests[i], requests[j]) = (requests[j], requests[i]);
        }

        foreach (var request in requests)
        {
            int tx = request.x + request.movementX;
            int ty = request.y + request.movementY;

			// if (blacklist.Contains((tx, ty)))
			// {
			// 	UpdateManager.Instance.RequestUpdateNextFrame(request.x, request.y); // so it can update again next frame if it was not able to move
			// 	continue;
			// }

            // The source must still contain the expected element.
            if (currentGrid[request.x, request.y] == null)
                continue;

			Element temp = currentGrid[tx, ty];
            currentGrid[tx, ty] = currentGrid[request.x, request.y];
            currentGrid[request.x, request.y] = temp;

			UpdateManager.Instance.UpdateNearbyCellsNextFrame(request.x, request.y, maxX, maxY);
			UpdateManager.Instance.UpdateNearbyCellsNextFrame(tx, ty, maxX, maxY);

			//blacklist.Add((request.x, request.y));
        }

        requests.Clear();
        sources.Clear();
        destinations.Clear();
		//blacklist.Clear();
    }
}