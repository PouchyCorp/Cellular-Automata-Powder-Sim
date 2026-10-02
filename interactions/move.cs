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
			if (element is IGas && element.density < target.density)
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
			if (element is ILiquid && element.density < target.density)
			{
				return false;
			}
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

    private MoveManager() { }

    public bool AttemptMove(
        Element[,] oldGrid,
        int x, int y,
        int dx, int dy,
        int maxX, int maxY)
    {
        if (sources.Contains((x, y)))
        	return false;

        if (!MoveRequest.CanMove(oldGrid, x, y, dx, dy, maxX, maxY))
            return false;

        int tx = x + dx;
        int ty = y + dy;

        if (destinations.Contains((tx, ty)))
            return false;

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

            // Don't move into an occupied cell, even if it is
            // expected to move away later in this update.
            if (currentGrid[tx, ty] != null)
                continue;

            // The source must still contain the expected element.
            if (currentGrid[request.x, request.y] == null)
                continue;

            currentGrid[tx, ty] = currentGrid[request.x, request.y];
            currentGrid[request.x, request.y] = null;
        }

        requests.Clear();
        sources.Clear();
        destinations.Clear();
    }
}