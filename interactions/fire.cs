using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

public interface IFlammable
{
    public bool burning { get; set; }
    public double flammability { get; set; } // the chance that the element will catch fire when in contact with fire
	public int burningLifetime { get; set; } // how long the element has been burning, in ticks
}

public static class FlammableBehavior
{
    public static void burn(IFlammable self, Element[,] oldElementArray, int x, int y, int maxX, int maxY, int T)
	{
		if (!self.burning) return;

		// Ignite neighbors in cardinal directions
		foreach ((int dx, int dy) in new (int, int)[] { (0, 1), (1, 0), (0, -1), (-1, 0) }) // big ugly loop but I don't care
		{
			int nx = x + dx;
			int ny = y + dy;

			if (nx >= 0 && nx < maxX && ny >= 0 && ny < maxY)
			{
				if (oldElementArray[nx, ny] != null)
				{
					Element neighbor = oldElementArray[nx, ny];
					if (neighbor is IFlammable flammableNeighbor && flammableNeighbor.flammability > 0 && !flammableNeighbor.burning)
					{
						// Chance to ignite based on flammability
						if (Random.Shared.NextSingle() < flammableNeighbor.flammability * 0.01f) // Adjust ignition chance factor as needed
						{
							ignite(nx, ny, maxX, maxY);
						}
					}
				}
			}
		}

        if (Random.Shared.NextSingle() < 0.01f){
            GridManager.Instance.RequestSpawn(x, y+1, new Smoke(), maxX, maxY); // spawn smoke above the burning element
        }

		self.burningLifetime--;
		if (self.burningLifetime <= 0)
		{
            if (self is ILife nutrientElement)
            {
                GridManager.Instance.RequestDeletion(x, y, maxX, maxY ,new Ash(nutrientElement.nutrient)); // element is consumed by fire and turned to ash
            }
            else
            {
                GridManager.Instance.RequestDeletion(x, y, maxX, maxY); // element is fully destroyed
            }
		}
	}

	public static void ignite(int x, int y, int maxX, int maxY)
	{
        FireManager.Instance.RequestIgnition(x, y, maxX, maxY);
	}
}

public class FireManager
{
    private static FireManager instance = null;

    private HashSet<(int, int)> ignitionRequests = new HashSet<(int, int)>();
    private FireManager()
    {
    }
    public static FireManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new FireManager();
            }
            return instance;
        }
    }

    public static bool IsValid(int x, int y, int maxX, int maxY)
    {
        return !(x < 0 || x >= maxX || y < 0 || y >= maxY);
    }
    

    public void RequestIgnition(int x, int y, int maxX, int maxY)
    {
        if (IsValid(x, y, maxX, maxY))
        {
            ignitionRequests.Add((x, y));
        }
    }

    public void ProcessIgnitionRequests(Element[,] elements, int maxX, int maxY)
    {
        foreach (var (x, y) in ignitionRequests)
        {
            if (IsValid(x, y, maxX, maxY) && elements[x, y] is IFlammable flammableElement)
            {
                if (!flammableElement.burning && flammableElement.flammability > 0)
                {
                    flammableElement.burning = true;
                    flammableElement.burningLifetime = (int)(5000 / flammableElement.flammability); // start counting burning lifetime
                }
            }
        }
        ignitionRequests.Clear();
    }
}