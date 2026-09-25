using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Godot;

public class GiveNutrientRequest
{
    public int x { get; set; }
    public int y { get; set; }

    public int targetX { get; set; }
    public int targetY { get; set; }
    public float nutrientAmount { get; set; }

    public GiveNutrientRequest(int x, int y, int targetX, int targetY, float nutrientAmount)
    {
        this.x = x;
        this.y = y;
        this.targetX = targetX;
        this.targetY = targetY;
        this.nutrientAmount = nutrientAmount;
    }

    public bool IsValid(int maxX, int maxY)
    {
        return nutrientAmount > 0 && !(x < 0 || x >= maxX || y < 0 || y >= maxY) && !(targetX < 0 || targetX >= maxX || targetY < 0 || targetY >= maxY);
    }
}

public class TakeNutrientRequest
{
    public int x { get; set; }
    public int y { get; set; }

    public int targetX { get; set; }
    public int targetY { get; set; }
    public float nutrientAmount { get; set; }

    public TakeNutrientRequest(int x, int y, int targetX, int targetY, float nutrientAmount)
   
    {

        this.x = x;
        this.y = y;
        this.targetX = targetX;
        this.targetY = targetY;
        this.nutrientAmount = nutrientAmount;
    }

    public bool IsValid(int maxX, int maxY)
    {
        return nutrientAmount > 0 && !(x < 0 || x >= maxX || y < 0 || y >= maxY) && !(targetX < 0 || targetX >= maxX || targetY < 0 || targetY >= maxY);
    }
}

// Not a thread safe implementation of the singleton
public class NutrientManager
{
    private Dictionary<(int, int), List<TakeNutrientRequest>> takeNutrientRequests = new Dictionary<(int, int), List<TakeNutrientRequest>>();
    private HashSet<(int, int)> uniqueNutrientTargets = new HashSet<(int, int)>();
    private Dictionary<(int, int), List<GiveNutrientRequest>> giveNutrientRequests = new Dictionary<(int, int), List<GiveNutrientRequest>>();

    private static NutrientManager instance = null;



    private NutrientManager()
    {
    }

    public static NutrientManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new NutrientManager();
            }
            return instance;
        }
    }

    public void AddTakeNutrientRequest(TakeNutrientRequest request, int maxX, int maxY)
    {
        if (request.IsValid(maxX, maxY))
        {
            if (!takeNutrientRequests.ContainsKey((request.targetX, request.targetY)))
            {
                takeNutrientRequests.Add((request.targetX, request.targetY), new List<TakeNutrientRequest>());
                uniqueNutrientTargets.Add((request.targetX, request.targetY));
            }
            takeNutrientRequests[(request.targetX, request.targetY)].Add(request);
        }
    }

    public void AddGiveNutrientRequest(GiveNutrientRequest request, int maxX, int maxY)
    {
        if (request.IsValid(maxX, maxY))
        {
            if (!giveNutrientRequests.ContainsKey((request.targetX, request.targetY)))
            {
                giveNutrientRequests.Add((request.targetX, request.targetY), new List<GiveNutrientRequest>());
                uniqueNutrientTargets.Add((request.targetX, request.targetY));
            }
            giveNutrientRequests[(request.targetX, request.targetY)].Add(request);
        }
    }

    public void ProcessNutrientRequests(Element[,] oldElementArray, Element[,] currentElementArray, int maxX, int maxY)
    {

        // give nutrient requests are puposely processed before take nutrient requests to save of conflicts
        foreach (var position in uniqueNutrientTargets)
        {
            // sum all the in and out nutrient requests for this position
            float totalNutrientToGive = 0;
            float totalNutrientToTake = 0;

            Element targetElement = currentElementArray[position.Item1, position.Item2];

            totalNutrientToGive = giveNutrientRequests.ContainsKey(position) ? giveNutrientRequests[position].Sum(r => r.nutrientAmount) : 0;
            totalNutrientToTake = takeNutrientRequests.ContainsKey(position) ? takeNutrientRequests[position].Sum(r => r.nutrientAmount) : 0;

            var diff = targetElement.nutrient + totalNutrientToGive - totalNutrientToTake;

            if (diff > 0)
            {
                // more nutrient is being given than taken, so we can give all the nutrient
                foreach (var request in giveNutrientRequests[position])
                {
                    currentElementArray[request.x, request.y].nutrient -= request.nutrientAmount;
                }

                foreach (var request in takeNutrientRequests[position])
                {
                    currentElementArray[request.x, request.y].nutrient += request.nutrientAmount;
                }
            }
            else if (diff < 0)
            {
                // more nutrient is being taken than given, so we can only take as much as is available
                float totalNutrientAvailable = currentElementArray[position.Item1, position.Item2].nutrient;
                float totalNutrientToTakeAdjusted = Mathf.Min(totalNutrientAvailable, totalNutrientToTake);

                foreach (var request in takeNutrientRequests[position])
                {
                    float nutrientToTake = Mathf.Min(request.nutrientAmount, totalNutrientToTakeAdjusted);
                    currentElementArray[request.x, request.y].nutrient -= nutrientToTake;
                    totalNutrientToTakeAdjusted -= nutrientToTake;
                    if (totalNutrientToTakeAdjusted <= 0)
                    {
                        break;
                    }
                }
            }
        }
        
        takeNutrientRequests.Clear();
        giveNutrientRequests.Clear();
    }
}