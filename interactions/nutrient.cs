using System.Collections.Generic;
using System.Linq;
using Godot;
using System;
using System.ComponentModel.DataAnnotations;

// ---------------------------------------
// TODO : Potential bug when an element is deleted before being processed
// ---------------------------------------
public interface ILife
{
    public float nutrient
    {
        get { return nutrient; }   // get method
        set
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThan(value, maxNutrient);
            nutrient = value;
        }  // set method
    }
    public float maxNutrient => 10.0f;

    public float wetness
    {
        get { return wetness; }   // get method
        set
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThan(value, maxWetness);
            wetness = value;
        }  // set method
    }

    public float maxWetness => 1.0f;
}

public class GiveNutrientRequest
{
    public int x { get; set; }
    public int y { get; set; }

    public int targetX { get; set; }
    public int targetY { get; set; }
    public float NutrientAmount { get; set; }

    public GiveNutrientRequest(int x, int y, int targetX, int targetY, float nutrientAmount)
    {
        this.x = x;
        this.y = y;
        this.targetX = targetX;
        this.targetY = targetY;
        this.NutrientAmount = nutrientAmount;
    }

    public bool IsValid(int maxX, int maxY)
    {
        return NutrientAmount > 0 && !(x < 0 || x >= maxX || y < 0 || y >= maxY) && !(targetX < 0 || targetX >= maxX || targetY < 0 || targetY >= maxY);
    }
}

public class TakeNutrientRequest
{
    public int x { get; set; }
    public int y { get; set; }

    public int targetX { get; set; }
    public int targetY { get; set; }
    public float NutrientAmount { get; set; }

    public TakeNutrientRequest(int x, int y, int targetX, int targetY, float nutrientAmount)

    {
        this.x = x;
        this.y = y;
        this.targetX = targetX;
        this.targetY = targetY;
        this.NutrientAmount = nutrientAmount;
    }

    public bool IsValid(int maxX, int maxY)
    {
        return NutrientAmount > 0 && !(x < 0 || x >= maxX || y < 0 || y >= maxY) && !(targetX < 0 || targetX >= maxX || targetY < 0 || targetY >= maxY);
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

    public void ProcessNutrientRequests(
    Element[,] oldElementArray,
    Element[,] currentElementArray)
    {
        foreach (var position in uniqueNutrientTargets)
        {
            if (currentElementArray[position.Item1, position.Item2] is not ILife targetElement)
                continue;

            bool hasGiveRequests = giveNutrientRequests.TryGetValue(
                position, out var giveRequests);

            bool hasTakeRequests = takeNutrientRequests.TryGetValue(
                position, out var takeRequests);

            float totalNutrientGiven = 0;
            for (int i = 0; i < giveRequests?.Count; i++)
            {
                var request = giveRequests[i];
                if (oldElementArray[request.x, request.y] is ILife nutrientElement)
                {
                    totalNutrientGiven += Mathf.Min(
                        request.NutrientAmount,
                        nutrientElement.nutrient
                    );
                }
            }

            float totalNutrientTaken = 0;
            for (int i = 0; i < takeRequests?.Count; i++)
            {
                var request = takeRequests[i];
                if (oldElementArray[request.x, request.y] is ILife nutrientElement)
                {
                    totalNutrientTaken += Mathf.Min(
                        request.NutrientAmount,
                        nutrientElement.maxNutrient - nutrientElement.nutrient
                    );
                }
            }

            float nutrientBalance =
                targetElement.nutrient
                + totalNutrientGiven
                - totalNutrientTaken;


            // ------------------------------------------------------------
            // The target would overflow.
            // the nutrient balance need to be trusted (every take and give request must be able to be truthfully fulfilled) or else, this procedure will delete nutrients from the world
            // ------------------------------------------------------------
            if (nutrientBalance > targetElement.maxNutrient)
            {
                // Process takes first (it can be entirely fulfilled, since the target is overflowing)
                if (hasTakeRequests)
                {
                    foreach (var request in takeRequests)
                    {
                        if (oldElementArray[request.x, request.y] is ILife takingElements)
                        {
                            // purposefully not accounting the available nutrient in the target element (we know it can be fulfilled and it improves nutrient flow)
                            float nutrientToTake = Mathf.Min(
                                request.NutrientAmount,
                                takingElements.maxNutrient - takingElements.nutrient
                            );

                            if (nutrientToTake <= 0)
                                continue;

                            takingElements.nutrient += nutrientToTake;
                        }
                    }
                }

                // Then process gives.
                //
                // There may still be more GIVE than the target can hold,
                // so the last requests may only be partially fulfilled.
                if (hasGiveRequests)
                {
                    float givenNutrient = 0;
                    float nutrientToBeGiven = targetElement.maxNutrient - targetElement.nutrient + totalNutrientTaken;
                    foreach (var request in giveRequests)
                    {
                        if (oldElementArray[request.x, request.y] is ILife givingElement)
                        {
                            if (givenNutrient >= nutrientToBeGiven)
                                break;
                            // purposefully not accounting the available nutrient in the target element (we know it can be fulfilled and it improves nutrient flow)
                            float nutrientToGive = Mathf.Min(
                                request.NutrientAmount,
                                Math.Min(nutrientToBeGiven - givenNutrient, givingElement.nutrient)
                            );

                            if (nutrientToGive <= 0)
                                continue;

                            givenNutrient += nutrientToGive;
                            givingElement.nutrient -= nutrientToGive;
                        }
                    }
                }

                targetElement.nutrient = targetElement.maxNutrient; // the target element is overflowing, so we set it to its max
            }


            // ------------------------------------------------------------
            // The target would underflow.
            // ------------------------------------------------------------
            else if (nutrientBalance < 0)
            {
                // Process takes first (it can be entirely fulfilled, since the target is overflowing)
                if (hasGiveRequests)
                {
                    foreach (var request in giveRequests)
                    {
                        if (oldElementArray[request.x, request.y] is ILife givingElement)
                        {
                            // purposefully not accounting the available nutrient in the target element (we know every GIVE can be fulfilled and it improves nutrient flow)
                            // the Min is redundant, but just to be sure ...
                            float nutrientToTake = Mathf.Min(
                                request.NutrientAmount,
                                givingElement.nutrient
                            );

                            if (nutrientToTake <= 0)
                                continue;

                            givingElement.nutrient -= nutrientToTake;
                        }
                    }
                }

                // Then process gives.
                //
                // There may still be more GIVE than the target can hold,
                // so the last requests may only be partially fulfilled.
                if (hasGiveRequests)
                {
                    float takenNutrient = 0;
                    float nutrientToBeTaken = Mathf.Min(targetElement.nutrient + totalNutrientGiven, totalNutrientTaken);
                    foreach (var request in giveRequests)
                    {
                        if (oldElementArray[request.x, request.y] is ILife takingElement)
                        {
                            if (takenNutrient >= nutrientToBeTaken)
                                break;

                            // purposefully not accounting the available nutrient in the target element (we know it will be 0 at the end)
                            float nutrientToTake = Mathf.Min(
                                request.NutrientAmount,
                                Math.Min(nutrientToBeTaken - takenNutrient, takingElement.maxNutrient - takingElement.nutrient)
                            );

                            if (nutrientToTake <= 0)
                                continue;

                            takenNutrient += nutrientToTake;
                            takingElement.nutrient += nutrientToTake;
                        }
                    }
                }

                // The requested outflow exceeds the available nutrient.
                targetElement.nutrient = 0;
            }


            // ------------------------------------------------------------
            // The requested net change fits within the target. (every GIVE and TAKE can be fulfilled)
            // 
            // Either order is safe.
            // ------------------------------------------------------------
            else
            {
                if (hasTakeRequests)
                {
                    foreach (var request in takeRequests)
                    {
                        if (oldElementArray[request.x, request.y] is ILife takingElements)
                        {
                            // purposefully not accounting the available nutrient in the target element (we know it can be fulfilled and it improves nutrient flow)
                            float nutrientToTake = Mathf.Min(
                                request.NutrientAmount,
                                takingElements.maxNutrient - takingElements.nutrient
                            );

                            if (nutrientToTake <= 0)
                                continue;

                            takingElements.nutrient += nutrientToTake;
                        }
                    }
                }

                if (hasGiveRequests)
                {
                    foreach (var request in giveRequests)
                    {
                        if (oldElementArray[request.x, request.y] is ILife givingElement)
                        {
                            // purposefully not accounting the available nutrient in the target element (we know it can be fulfilled and it improves nutrient flow)
                            float nutrientToGive = Mathf.Min(
                                request.NutrientAmount,
                                givingElement.nutrient
                            );

                            if (nutrientToGive <= 0)
                                continue;

                            givingElement.nutrient -= nutrientToGive;
                        }
                    }
                }

                targetElement.nutrient = nutrientBalance;
            }
        }

        takeNutrientRequests.Clear();
        giveNutrientRequests.Clear();
        uniqueNutrientTargets.Clear();
    }
}