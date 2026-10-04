using System.Collections.Generic;
using System.ComponentModel;
using Godot;


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

    private Dictionary<(int, int), DeletionRequest> DeletionRequests = new();
    private Dictionary<(int, int), SpawnRequest> SpawnRequests =  new();

    private bool EnqueueDeletionRequest(DeletionRequest request)
    {
        if (!DeletionRequests.ContainsKey((request.x, request.y)))
        {
            DeletionRequests.Add((request.x, request.y), request);
            return true;
        }
        return false;
    }

    private bool EnqueueSpawnRequest(SpawnRequest request)
    {
        if (!SpawnRequests.ContainsKey((request.x, request.y)))
        {
            SpawnRequests.Add((request.x, request.y), request);
            return true;
        }
        return false;
    }

    public bool RequestDeletion(int x, int y, int maxX, int maxY, Element replace = null)
    {
        DeletionRequest request = new DeletionRequest(x, y, replace);
        if (request.IsValid(maxX, maxY)){
            return EnqueueDeletionRequest(request);
        }
        return false; 
    }

    public bool RequestSpawn(int x, int y, Element elementToSpawn, int maxX, int maxY)
    {
        // this will fail if the place is not empty, use RequestDeletion first if you want to forcefully spawn an element
        SpawnRequest request = new SpawnRequest(x, y, elementToSpawn);
        if (request.IsValid(maxX, maxY)){
            return EnqueueSpawnRequest(request);
        }
        return false;   
    }

    public void ProcessDeletions(Element[,] elements, int maxX, int maxY)
    {
        foreach (var ((x, y), requests) in DeletionRequests)
        {

            elements[x, y] = requests.replace; // replace with the specified element or null if none specified

            UpdateManager.Instance.UpdateNearbyCellsNextFrame(x, y, maxX, maxY);
        }
        DeletionRequests.Clear();
    }

    public void ProcessSpawns(Element[,] elements, int maxX, int maxY)
    {
        foreach (var ((x, y), requests) in SpawnRequests)
        {
            if (elements[x, y] != null) continue; // Only spawn if the cell is empty (main difference with deletion)

            elements[x, y] = requests.elementToSpawn; // replace with the specified element or null if none specified
            UpdateManager.Instance.UpdateNearbyCellsNextFrame(x, y, maxX, maxY);
        }
        SpawnRequests.Clear();
    }
}
