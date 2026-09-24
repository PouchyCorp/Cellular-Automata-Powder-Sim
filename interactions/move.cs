using System.Collections.Generic;
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
    public bool CanMove(Element[,] oldElementArray)
    {
        int newX = x + movementX;
        int newY = y + movementY;

        Element elementWhereMovement = oldElementArray[newX, newY];

        if (
                (movementY > 0 &&
                oldElementArray[x, y].canMoveDownOnElement(elementWhereMovement))
            ||
                (movementY < 0 &&
                oldElementArray[x, y].canMoveUpOnElement(elementWhereMovement))
            ||
                (movementY == 0 &&
                oldElementArray[x, y].canMoveSideOnElement(elementWhereMovement))
            )
        {
            return true;
        }

        return false;
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

    public void AddMoveRequest(MoveRequest request, int maxX, int maxY)
    {
        if (request.IsValid(maxX, maxY))
        {
            if (!moveRequests.ContainsKey((request.x, request.y)))
            {
                moveRequests.Add((request.x, request.y), new List<MoveRequest>());
                uniqueMoveTargets.Add((request.x, request.y));
            }
            moveRequests[(request.x, request.y)].Add(request);
        }
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
                if (request.CanMove(oldElementArray))
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




