using System.Collections.Generic;
using System.Text.RegularExpressions;
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
		return (movementX != 0 || movementY != 0) && !(x+movementX < 0 || x+movementX >= maxX || y+movementY < 0 || y+movementY >= maxY);
	}

	public static bool canMoveOnElement(Element element, Element target)
	{
		if (target == null)
		{
			return true;
		}
		if (target is IGas){
			if (element is IGas && element.density < target.density){ // we assume that the gas density is always less than any other element density
				return false;
			}
			return true;
		}
		if (target is ILiquid){
			if (element is IGas){
				return false;
			}
			if (element is ILiquid && element.density < target.density){
				return false;
			}
			return true;
		}
		return false;
	}
	public static bool CanMove(Element[,] oldElementArray, int x, int y, int movementX, int movementY, int maxX, int maxY)
	{
		int newX = x + movementX;
		int newY = y + movementY;

		if (newX < 0 || newX >= maxX || newY < 0 || newY >= maxY)
		{
			return false;
		}

		return canMoveOnElement(oldElementArray[x, y], oldElementArray[newX, newY]);
	}
}


public sealed class MoveManager
{
	
	private static MoveManager instance = null;

	private MoveManager()
	{
	}

	public static MoveManager Instance
	{
		get
		{
			if (instance == null)
			{
				instance = new MoveManager();
			}
			return instance;
		}
	}

	private Dictionary<(int, int), List<MoveRequest>> moveRequests = [];
	private List<(int, int)> uniqueMoveTargets = [];

	private HashSet<(int, int)> uniqueMoveSources = [];

	public bool AttemptMove(Element[,] oldElementArray, int x, int y, int movementX, int movementY, int maxX, int maxY)
	{
		if (MoveRequest.CanMove(oldElementArray, x, y, movementX, movementY, maxX, maxY) && !uniqueMoveSources.Contains((x, y)))
		{
			if (AddMoveRequest(new MoveRequest(x, y, movementX, movementY), maxX, maxY))
			{
				uniqueMoveSources.Add((x, y));
				return true;
			}
		}
		return false;
	}

	public bool AddMoveRequest(MoveRequest request, int maxX, int maxY)
	{
		if (request.IsValid(maxX, maxY))
		{
			if (!moveRequests.ContainsKey((request.x, request.y)))
			{
				moveRequests.Add((request.x, request.y), new List<MoveRequest>());
				uniqueMoveTargets.Add((request.x, request.y));
			}
			moveRequests[(request.x, request.y)].Add(request);
			return true;
		}
		return false;
	}

	public void ProcessMoveRequests(Element[,] oldElementArray, Element[,] currentElementArray, int maxX, int maxY)
	{

		// for each unique target position, process the move requests in the order they were added
		foreach (var position in uniqueMoveTargets)
		{
			var requests = moveRequests[position];
			var validRequests = new List<MoveRequest>();
			// Sort out every cell that cannot make the move for whatever reason
			foreach (var request in requests)
			{
				if (MoveRequest.CanMove(oldElementArray, request.x, request.y, request.movementX, request.movementY, maxX, maxY))
				{
					validRequests.Add(request);
				}
			}

			if (validRequests.Count == 0)
			{
				continue; // No valid requests for this position, skip to the next
			}

			// Sort the valid requests by density between them
			validRequests.Sort((a, b) =>
			{
				var elementA = oldElementArray[a.x, a.y];
				var elementB = oldElementArray[b.x, b.y];

				return elementB.density.CompareTo(elementA.density);
			});

			// Pick the first one
			var pickedRequest = validRequests[0];
			int newX = pickedRequest.x + pickedRequest.movementX;
			int newY = pickedRequest.y + pickedRequest.movementY;

			// Swap the elements in the currentElementArray
			currentElementArray[pickedRequest.x, pickedRequest.y] = currentElementArray[newX, newY];
			currentElementArray[newX, newY] = oldElementArray[pickedRequest.x, pickedRequest.y];
		}

		// Clear the requests after processing
		moveRequests.Clear();
		uniqueMoveTargets.Clear();
	}
}
