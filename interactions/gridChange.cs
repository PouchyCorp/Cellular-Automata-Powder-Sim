using System.Collections.Generic;
using System.ComponentModel;


public class DeletionRequest
{
    public int x { get; set; }
    public int y { get; set; }

    public Element replace { get; set; } // Optional: specify an element to replace the deleted element with

    public DeletionRequest(int x, int y, Element replace = null)
    {
        this.x = x;
        this.y = y;
        this.replace = replace;
    }

    public bool IsValid(int maxX, int maxY)
    {
        return !(x < 0 || x >= maxX || y < 0 || y >= maxY);
    }
}

public class SpawnRequest
{
    public int x { get; set; }
    public int y { get; set; }

    public Element elementToSpawn { get; set; }

    public SpawnRequest(int x, int y, Element elementToSpawn)
    {
        this.x = x;
        this.y = y;
        this.elementToSpawn = elementToSpawn;
    }

    public bool IsValid(int maxX, int maxY)
    {
        return !(x < 0 || x >= maxX || y < 0 || y >= maxY);
    }
}

public class GridManager
{
    private static GridManager instance = null;
    private GridManager()
    {
    }
    public static GridManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new GridManager();
            }
            return instance;
        }
    }

    private Dictionary<(int, int), List<DeletionRequest>> DeletionRequests = new Dictionary<(int, int), List<DeletionRequest>>();
    private Dictionary<(int, int), List<SpawnRequest>> SpawnRequests = new Dictionary<(int, int), List<SpawnRequest>>();

    public bool RequestDeletion(int x, int y, int maxX, int maxY, Element replace = null)
    {
        DeletionRequest request = new DeletionRequest(x, y, replace);
        if (request.IsValid(maxX, maxY)){
                DeletionRequests.Add((x, y), [request]);
            return true; 
        }
        return false; 
    }

    public bool RequestSpawn(int x, int y, Element elementToSpawn, int maxX, int maxY)
    {
        // this will fail if the place is not empty, use RequestDeletion first if you want to forcefully spawn an element
        SpawnRequest request = new SpawnRequest(x, y, elementToSpawn);
        if (request.IsValid(maxX, maxY)){
            SpawnRequests.Add((x, y), [request]);
            return true; 
        }
        return false;   
    }

    public double getPriority(Element element)
    {
        if (element == null) return 0;
        if (element is ILife) return 100; // Life has the highest priority
        return element.density; // Higher density means higher priority
    }

    public void ProcessDeletions(Element[,] elements, int maxX, int maxY)
    {
        foreach (var ((x, y), requests) in DeletionRequests)
        {
            // find highest priority request
            DeletionRequest highestPriorityRequest = requests[0];
            foreach (var request in requests)
            {
                if (getPriority(request.replace) > getPriority(highestPriorityRequest.replace))
                {
                    highestPriorityRequest = request;
                }
            }

            elements[x, y] = highestPriorityRequest.replace; // replace with the specified element or null if none specified

        }
        DeletionRequests.Clear();
    }

    public void ProcessSpawns(Element[,] elements, int maxX, int maxY)
    {
        foreach (var ((x, y), requests) in SpawnRequests)
        {
            if (elements[x, y] != null) continue; // Only spawn if the cell is empty (main difference with deletion)

            SpawnRequest highestPriorityRequest = requests[0];
            foreach (var request in requests)
            {
                if (getPriority(request.elementToSpawn) > getPriority(highestPriorityRequest.elementToSpawn))
                {
                    highestPriorityRequest = request;
                }
            }

            elements[x, y] = highestPriorityRequest.elementToSpawn; // replace with the specified element or null if none specified
        }
        SpawnRequests.Clear();
    }
}
