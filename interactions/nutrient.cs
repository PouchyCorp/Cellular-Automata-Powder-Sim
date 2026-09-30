using System.Collections.Generic;
using System.Linq;
using Godot;
using System;
using System.ComponentModel.DataAnnotations;

// ---------------------------------------
// TODO : Make the NutrientManager account for the maxNutrient and maxWetness of the target element when processing requests
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

            float totalNutrientGiven = hasGiveRequests
                ? giveRequests.Sum(r => r.NutrientAmount)
                : 0;

            float totalNutrientTaken = hasTakeRequests
                ? takeRequests.Sum(r => r.NutrientAmount)
                : 0;

            float nutrientBalance =
                targetElement.nutrient
                + totalNutrientGiven
                - totalNutrientTaken;


            // ------------------------------------------------------------
            // The target would overflow.
            // ------------------------------------------------------------
            if (nutrientBalance > targetElement.maxNutrient)
            {
                // Process takes first
                if (hasTakeRequests)
                {
                    foreach (var request in takeRequests)
                    {
                        if (oldElementArray[request.x, request.y] is ILife nutrientElement)
                        {
                            float nutrientToTake = Mathf.Min(
                                request.NutrientAmount,
                                Mathf.Min(targetElement.nutrient,
                                nutrientElement.maxNutrient - nutrientElement.nutrient)
                            );

                            if (nutrientToTake <= 0)
                                continue;

                            targetElement.nutrient -= nutrientToTake;
                            nutrientElement.nutrient += nutrientToTake;
                        }
                    }
                }

                // Then process gives.
                //
                // There may still be more GIVE than the target can hold,
                // so the last requests may only be partially fulfilled.
                if (hasGiveRequests)
                {
                    foreach (var request in giveRequests)
                    {
                        if (oldElementArray[request.x, request.y] is ILife nutrientElement)
                        {
                            float nutrientToGive = Mathf.Min(
                                request.NutrientAmount,
                                Mathf.Min(nutrientElement.nutrient,
                                targetElement.maxNutrient - targetElement.nutrient)
                            );

                            if (nutrientToGive <= 0)
                                continue;

                            nutrientElement.nutrient -= nutrientToGive;
                            targetElement.nutrient += nutrientToGive;
                        }
                    }
                }
            }


            // ------------------------------------------------------------
            // The target would underflow.
            // ------------------------------------------------------------
            else if (nutrientBalance < 0)
            {
                // Process gives first
                if (hasGiveRequests)
                {
                    foreach (var request in giveRequests)
                    {
                        if (oldElementArray[request.x, request.y] is ILife nutrientElement)
                        {
                            float nutrientToGive = Mathf.Min(
                                request.NutrientAmount,
                                Mathf.Min(nutrientElement.nutrient, targetElement.maxNutrient - targetElement.nutrient)
                            );

                            if (nutrientToGive <= 0)
                                continue;

                            nutrientElement.nutrient -= nutrientToGive;
                            targetElement.nutrient += nutrientToGive;
                        }
                    }
                }

                // Then process takes
                if (hasTakeRequests)
                {
                    foreach (var request in takeRequests)
                    {
                        if (oldElementArray[request.x, request.y] is ILife nutrientElement)
                        {
                            float nutrientToTake = Mathf.Min(
                                request.NutrientAmount,
                                Mathf.Min(targetElement.nutrient, nutrientElement.maxNutrient - nutrientElement.nutrient)
                            );

                            if (nutrientToTake <= 0)
                                continue;

                            targetElement.nutrient -= nutrientToTake;
                            nutrientElement.nutrient += nutrientToTake;
                        }
                    }
                }
            }


            // ------------------------------------------------------------
            // The requested net change fits within the target.
            //
            // Either order is safe.
            // ------------------------------------------------------------
            else
            {
                // Give
                if (hasGiveRequests)
                {
                    foreach (var request in giveRequests)
                    {
                        if (oldElementArray[request.x, request.y] is ILife nutrientElement)
                        {
                            float nutrientToGive = Mathf.Min(
                                request.NutrientAmount,
                                Mathf.Min(nutrientElement.nutrient, targetElement.maxNutrient - targetElement.nutrient)
                            );

                            if (nutrientToGive <= 0)
                                continue;

                            nutrientElement.nutrient -= nutrientToGive;
                            targetElement.nutrient += nutrientToGive;
                        }
                    }
                }

                // Take
                if (hasTakeRequests)
                {
                    foreach (var request in takeRequests)
                    {
                        if (oldElementArray[request.x, request.y] is ILife nutrientElement)
                        {
                            float nutrientToTake = Mathf.Min(
                                request.NutrientAmount,
                                Mathf.Min(targetElement.nutrient,
                                nutrientElement.maxNutrient - nutrientElement.nutrient)
                            );

                            if (nutrientToTake <= 0)
                                continue;

                            targetElement.nutrient -= nutrientToTake;
                            nutrientElement.nutrient += nutrientToTake;
                        }
                    }
                }
            }
        }

        takeNutrientRequests.Clear();
        giveNutrientRequests.Clear();
        uniqueNutrientTargets.Clear();
    }

    public void ProcessWetnessRequests(Element[,] oldElementArray, Element[,] currentElementArray, int maxX, int maxY) // almost the same as ProcessWetnessRequests, but for wetness
                                                                                                                       // IMPORTANT : This was made by just copying and pasting the above method and it may be bugged
    {

        // give wetness requests are puposely processed before take wetness requests to save of conflicts
        foreach (var position in uniqueWetnessTargets)
        {
            // sum all the in and out wetness requests for this position
            float totalWetnessGiven = 0;
            float totalWetnessTaken = 0;

            bool hasGiveRequests = giveWetnessRequests.ContainsKey(position);
            bool hasTakeRequests = takeWetnessRequests.ContainsKey(position);

            totalWetnessGiven = hasGiveRequests ? giveWetnessRequests[position].Sum(r => r.WetnessAmount) : 0;
            totalWetnessTaken = hasTakeRequests ? takeWetnessRequests[position].Sum(r => r.WetnessAmount) : 0;

            if (currentElementArray[position.Item1, position.Item2] is ILife targetElement)
            {

                var wetnessBalance = targetElement.wetness + totalWetnessGiven - totalWetnessTaken;
                if (targetElement.maxWetness < wetnessBalance)
                {
                    // if the target element cannot hold all the wetness, we need to adjust the totalWetnessToTake and totalWetnessToGive
                    float excessWetness = wetnessBalance - targetElement.maxWetness;
                    if (excessWetness > 0)
                    {
                        // we need to reduce the totalWetnessToTake by the excess wetness
                        totalWetnessGiven -= excessWetness;
                    }

                    if (!hasGiveRequests) continue;
                    foreach (var request in giveWetnessRequests[position])
                    {
                        if (oldElementArray[request.x, request.y] is ILife wetnessElement)
                        {
                            float wetnessToGive = Mathf.Min(request.WetnessAmount, totalWetnessGiven);
                            wetnessElement.wetness -= wetnessToGive;
                            targetElement.wetness += wetnessToGive;
                            totalWetnessGiven -= wetnessToGive;
                            if (totalWetnessGiven <= 0)
                            {
                                break;
                            }
                        }
                    }

                }
                else if (wetnessBalance < 0)
                {
                    // more wetness is being taken than given, so we can only take as much as is available
                    float totalWetnessAvailable = targetElement.wetness;
                    float totalWetnessToTakeAdjusted = Mathf.Min(totalWetnessAvailable, totalWetnessTaken);

                    if (!hasTakeRequests) continue;
                    foreach (var request in takeWetnessRequests[position])
                    {
                        if (oldElementArray[request.x, request.y] is ILife wetnessElement)
                        {
                            float wetnessToTake = Mathf.Min(request.WetnessAmount, totalWetnessToTakeAdjusted);
                            targetElement.wetness -= wetnessToTake;
                            wetnessElement.wetness += wetnessToTake;
                            totalWetnessToTakeAdjusted -= wetnessToTake;
                            if (totalWetnessToTakeAdjusted <= 0)
                            {
                                break;
                            }
                        }
                    }
                }
                else // if balance is 0 or positive, we can give all the wetness
                {
                    // more wetness is being given than taken, so we can give all the wetness
                    if (!hasGiveRequests) continue;
                    foreach (var request in giveWetnessRequests[position])
                    {
                        if (oldElementArray[request.x, request.y] is ILife wetnessElement)
                        {
                            wetnessElement.wetness -= request.WetnessAmount;
                        }
                    }
                    if (!hasTakeRequests) continue;
                    foreach (var request in takeWetnessRequests[position])
                    {
                        if (oldElementArray[request.x, request.y] is ILife wetnessElement)
                        {
                            wetnessElement.wetness += request.WetnessAmount;
                        }
                    }

                    targetElement.wetness = wetnessBalance; // set the target element's wetness to the new value
                }
            }
        }

        takeWetnessRequests.Clear();
        giveWetnessRequests.Clear();
        uniqueWetnessTargets.Clear();
    }
}
