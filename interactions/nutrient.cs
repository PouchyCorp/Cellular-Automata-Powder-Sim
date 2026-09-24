using System.Collections.Generic;
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

public class NutrientManager
{
    private List<TakeNutrientRequest> takeNutrientRequests = new List<TakeNutrientRequest>();
    private List<GiveNutrientRequest> giveNutrientRequests = new List<GiveNutrientRequest>();

    public void AddTakeNutrientRequest(TakeNutrientRequest request, int maxX, int maxY)
    {
        if (request.IsValid(maxX, maxY))
        {
            takeNutrientRequests.Add(request);
        }
    }

    public void AddGiveNutrientRequest(GiveNutrientRequest request, int maxX, int maxY)
    {
        if (request.IsValid(maxX, maxY))
        {
            giveNutrientRequests.Add(request);
        }
    }

    public void ProcessNutrientRequests(Element[,] oldElementArray, Element[,] currentElementArray, int maxX, int maxY)
    {

        // give nutrient requests are puposely processed before take nutrient requests to save of conflicts
        foreach (var request in giveNutrientRequests)
        {
            if (request.x >= 0 && request.x < maxX && request.y >= 0 && request.y < maxY &&
                request.targetX >= 0 && request.targetX < maxX && request.targetY >= 0 && request.targetY < maxY)
            {
                // TODO Implement nutrient addition/removal logic here, as well as request conflicts (resolve using 50/50 or a priority system)
            }
        }
        foreach (var request in takeNutrientRequests)
        {
            if (request.x >= 0 && request.x < maxX && request.y >= 0 && request.y < maxY)
            {
                // TODO Implement nutrient addition/removal logic here, as well as request conflics (resolve using 50/50 or a priority system)
            }
        }
        takeNutrientRequests.Clear();
        giveNutrientRequests.Clear();
    }
}