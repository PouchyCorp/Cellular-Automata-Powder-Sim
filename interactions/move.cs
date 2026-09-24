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

    public bool IsValid()
    {
        return movementX != 0 || movementY != 0;
    }
    public bool CanMove(Element[,] oldElementArray, int maxX, int maxY)
    {
        int newX = x + movementX;
        int newY = y + movementY;

        if (newX >= maxX || newX < 0 ||
            newY >= maxY || newY < 0) return false;

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


public class MoveManager
{
    private List<MoveRequest> moveRequests = [];

    public void AddMoveRequest(MoveRequest request)
    {
        if (request.IsValid())
            moveRequests.Add(request);
    }

    public void ProcessMoveRequests(Element[,] oldElementArray, Element[,] currentElementArray, int maxX, int maxY)
    {
        foreach (var request in moveRequests)
        {
            if (request.CanMove(oldElementArray, maxX, maxY))
            {
                int newX = request.x + request.movementX;
                int newY = request.y + request.movementY;

                // Swap the elements in the currentElementArray
                currentElementArray[request.x, request.y] = currentElementArray[newX, newY];
                currentElementArray[newX, newY] = oldElementArray[request.x, request.y];
            }
        }

        // Clear the requests after processing
        moveRequests.Clear();
    }
}




