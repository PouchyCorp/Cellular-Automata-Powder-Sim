using System.Collections.Generic;
using System.Linq;
using Godot;
using System;

public interface ILife
{
	public float nutrient { get; set; }
	public float maxNutrient { get; set; }
	public float wetness
	{
		get { return wetness; }   // get method
		set {
			ArgumentOutOfRangeException.ThrowIfGreaterThan(value, 1);
			wetness = value; }  // set method
	}
}

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

public class TakeWetnessRequest
{
    public int x { get; set; }
    public int y { get; set; }
        
    public int targetX { get; set; }
    public int targetY { get; set; }
    public float WetnessAmount { get; set; }

    public TakeWetnessRequest(int x, int y, int targetX, int targetY, float wetnessAmount)
   
    {
        this.x = x;
        this.y = y;
        this.targetX = targetX;
        this.targetY = targetY;
        this.WetnessAmount = wetnessAmount;
    }

    public bool IsValid(int maxX, int maxY)
    {
        return WetnessAmount > 0 && !(x < 0 || x >= maxX || y < 0 || y >= maxY) && !(targetX < 0 || targetX >= maxX || targetY < 0 || targetY >= maxY);
    }
}

public class GiveWetnessRequest
{
    public int x { get; set; }
    public int y { get; set; }
        
    public int targetX { get; set; }
    public int targetY { get; set; }
    public float WetnessAmount { get; set; }

    public GiveWetnessRequest(int x, int y, int targetX, int targetY, float wetnessAmount)
    {
        this.x = x;
        this.y = y;
        this.targetX = targetX;
        this.targetY = targetY;
        this.WetnessAmount = wetnessAmount;
    }

    public bool IsValid(int maxX, int maxY)
    {
        return WetnessAmount > 0 && !(x < 0 || x >= maxX || y < 0 || y >= maxY) && !(targetX < 0 || targetX >= maxX || targetY < 0 || targetY >= maxY);
    }
}

// Not a thread safe implementation of the singleton
public class NutrientManager
{
    private HashSet<(int, int)> uniqueNutrientTargets = new HashSet<(int, int)>();
    private HashSet<(int, int)> uniqueWetnessTargets = new HashSet<(int, int)>();
    private Dictionary<(int, int), List<TakeNutrientRequest>> takeNutrientRequests = new Dictionary<(int, int), List<TakeNutrientRequest>>();
    private Dictionary<(int, int), List<TakeWetnessRequest>> takeWetnessRequests = new Dictionary<(int, int), List<TakeWetnessRequest>>();
    private Dictionary<(int, int), List<GiveWetnessRequest>> giveWetnessRequests = new Dictionary<(int, int), List<GiveWetnessRequest>>();
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

    public void AddGiveWetnessRequest(GiveWetnessRequest request, int maxX, int maxY)
    {
        if (request.IsValid(maxX, maxY))
        {
            if (!giveWetnessRequests.ContainsKey((request.targetX, request.targetY)))
            {
                giveWetnessRequests.Add((request.targetX, request.targetY), new List<GiveWetnessRequest>());
                uniqueWetnessTargets.Add((request.targetX, request.targetY));
            }
            giveWetnessRequests[(request.targetX, request.targetY)].Add(request);
        }
    }

    public void AddTakeWetnessRequest(TakeWetnessRequest request, int maxX, int maxY)
    {
        if (request.IsValid(maxX, maxY))
        {
            if (!takeWetnessRequests.ContainsKey((request.targetX, request.targetY)))
            {
                takeWetnessRequests.Add((request.targetX, request.targetY), new List<TakeWetnessRequest>());
                uniqueWetnessTargets.Add((request.targetX, request.targetY));
            }
            takeWetnessRequests[(request.targetX, request.targetY)].Add(request);
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

            totalNutrientToGive = giveNutrientRequests.ContainsKey(position) ? giveNutrientRequests[position].Sum(r => r.nutrientAmount) : 0;
            totalNutrientToTake = takeNutrientRequests.ContainsKey(position) ? takeNutrientRequests[position].Sum(r => r.nutrientAmount) : 0;

            if (currentElementArray[position.Item1, position.Item2] is ILife nutrientElement && currentElementArray[position.Item1, position.Item2] is ILife targetElement)
            {

                var diff = targetElement.nutrient + totalNutrientToGive - totalNutrientToTake;

                if (diff > 0)
                {
                    // more nutrient is being given than taken, so we can give all the nutrient
                    foreach (var request in giveNutrientRequests[position])
                    {
                        nutrientElement.nutrient -= request.nutrientAmount;
                        targetElement.nutrient += request.nutrientAmount;
                    }

                    foreach (var request in takeNutrientRequests[position])
                    {
                        targetElement.nutrient -= request.nutrientAmount;
                    }
                }
                else if (diff < 0)
                {
                    // more nutrient is being taken than given, so we can only take as much as is available
                    float totalNutrientAvailable = targetElement.nutrient;
                    float totalNutrientToTakeAdjusted = Mathf.Min(totalNutrientAvailable, totalNutrientToTake);

                    foreach (var request in takeNutrientRequests[position])
                    {
                        float nutrientToTake = Mathf.Min(request.nutrientAmount, totalNutrientToTakeAdjusted);
                        targetElement.nutrient -= nutrientToTake;
                        nutrientElement.nutrient -= nutrientToTake;
                        totalNutrientToTakeAdjusted -= nutrientToTake;
                        if (totalNutrientToTakeAdjusted <= 0)
                        {
                            break;
                        }
                    }
                }
            }
        }
        
        takeNutrientRequests.Clear();
        giveNutrientRequests.Clear();
    }

    public void ProcessWetnessRequests(Element[,] oldElementArray, Element[,] currentElementArray, int maxX, int maxY) // almost the same as ProcessNutrientRequests, but for wetness
    // IMPORTANT : This was made by just copying and pasting the above method and it may be bugged
    {
        
        // give nutrient requests are puposely processed before take nutrient requests to save of conflicts
        foreach (var position in uniqueWetnessTargets)
        {
            // sum all the in and out Wetness requests for this position
            float totalWetnessToGive = 0;
            float totalWetnessToTake = 0;

            totalWetnessToGive = giveWetnessRequests.ContainsKey(position) ? giveWetnessRequests[position].Sum(r => r.WetnessAmount) : 0;
            totalWetnessToTake = takeWetnessRequests.ContainsKey(position) ? takeWetnessRequests[position].Sum(r => r.WetnessAmount) : 0;

            if (currentElementArray[position.Item1, position.Item2] is ILife WetnessElement && currentElementArray[position.Item1, position.Item2] is ILife targetElement)
            {

                var diff = targetElement.wetness + totalWetnessToGive - totalWetnessToTake;

                if (diff > 0)
                {
                    // more Wetness is being given than taken, so we can give all the Wetness
                    foreach (var request in giveWetnessRequests[position])
                    {
                        WetnessElement.wetness -= request.WetnessAmount;
                        targetElement.wetness += request.WetnessAmount;
                    }

                    foreach (var request in takeWetnessRequests[position])
                    {
                        targetElement.wetness -= request.WetnessAmount;
                    }
                }
                else if (diff < 0)
                {
                    // more Wetness is being taken than given, so we can only take as much as is available
                    float totalWetnessAvailable = targetElement.wetness;
                    float totalWetnessToTakeAdjusted = Mathf.Min(totalWetnessAvailable, totalWetnessToTake);

                    foreach (var request in takeWetnessRequests[position])
                    {
                        float WetnessToTake = Mathf.Min(request.WetnessAmount, totalWetnessToTakeAdjusted);
                        targetElement.wetness -= WetnessToTake;
                        WetnessElement.wetness -= WetnessToTake;
                        totalWetnessToTakeAdjusted -= WetnessToTake;
                        if (totalWetnessToTakeAdjusted <= 0)
                        {
                            break;
                        }
                    }
                }
            }
        }
        
        takeNutrientRequests.Clear();
        giveNutrientRequests.Clear();
    }
}