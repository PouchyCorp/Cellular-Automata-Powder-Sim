using System;
using System.Collections.Generic;
using Godot;
public class Root : Element, ILife, ISolid
{
	public float nutrient { get; set; } = 0f;
	public float maxNutrient => 10f;
	public float wetness { get; set; } = 0f;
	public float maxWetness => 1f;
	
	private (int, int) parentSeed;
	private int lastActivity = 0;
	private int activityInterval = 30;
	private int lastGrowthTick = 0;

	public Root() {} // DO NOT USE EXCEPT IF YOU'RE GONNA SET A STATE RIGHT AFTER

	public Root((int, int) parentSeed)
	{
		this.parentSeed = parentSeed;
		
		density = 21;
		color = Colors.SandyBrown;
		modulateColor();
	}

	/// <summary>
	/// Returns the parent seed if it still exists, otherwise null
	/// </summary>
	public Seed getParentSeed(Element[,] currentGrid, int maxX, int maxY)
	{
		if (currentGrid[parentSeed.Item1, parentSeed.Item2] is Seed seed)
		{
			return seed;
		}
		return null;
	}
	private void absorbNutrientsAndWetness(Element[,] currentGrid, int x, int y, int maxX, int maxY)
	{
		// absorb nutrients and wetness from adjacent soil in cardinal directions
		(int, int)[] directions = [(0, -1), (0, 1), (-1, 0), (1, 0)];
		
		foreach (var dir in directions)
		{
			int targetX = x + dir.Item1;
			int targetY = y + dir.Item2;
			if (targetX >= 0 && targetX < maxX && targetY >= 0 && targetY < maxY)
			{
				if (currentGrid[targetX, targetY] is Soil soil)
				{

					NutrientManager.Instance.AddTakeNutrientRequest(new TakeNutrientRequest(x, y, targetX, targetY, soil.nutrient / 4), maxX, maxY);
					NutrientManager.Instance.AddTakeWetnessRequest(new TakeWetnessRequest(x, y, targetX, targetY, soil.wetness / 4), maxX, maxY);
				}
			}
		}
	}

	private void transferNutrientsUpwards(Element[,] currentGrid, int x, int y, int maxX, int maxY)
	{
		// transfer nutrients to parent seed if it exists
		Seed seed = getParentSeed(currentGrid, maxX, maxY);
		if (seed != null)
		{
			if (seed.nutrient >= seed.maxNutrient && seed.wetness >= 1f) return; // parent seed full

			float transferableNutrients = Math.Max(nutrient - 1, 0); // keep at least 1 nutrient in root
			transferableNutrients = Math.Min(transferableNutrients, seed.maxNutrient - seed.nutrient); // don't overfill seed
			float transferableWetness = Math.Max(wetness - 0.2f, 0); // keep at least 0.2 wetness in root
			transferableWetness = Math.Min(transferableWetness, 1f - seed.wetness); // don't overfill seed
			seed.nutrient += transferableNutrients;
			nutrient -= transferableNutrients;
			seed.wetness += transferableWetness;
			wetness -= transferableWetness;
		}
	}

	private bool isValidRootGrowthPosition(Element[,] currentGrid, int x, int y, int maxX, int maxY)
	{
		if (x < 0 || x >= maxX || y < 0 || y >= maxY) return false;
		if (currentGrid[x, y] is not Soil) return false;
		int adjacentRoots = 0;
		for (int nx = x - 1; nx <= x + 1; nx++)
		{
			for (int ny = y - 1; ny <= y + 1; ny++)
			{
				if ((nx, ny) == (x, y)) continue;
				if (nx >= 0 && nx < maxX && ny >= 0 && ny < maxY)
				{
					if (currentGrid[nx, ny] is Root)
					{
						adjacentRoots++;
					}
				}
			}
		}
		if (adjacentRoots > 1) return false;
		return true;
	}

	public bool growRoot(Element[,] oldGrid, Element[,] currentGrid, int x, int y, int maxX, int maxY)
	{
		if (nutrient < 1) return false;
		if (wetness < 0.2f) return false;
		if (y + 1 >= maxY) return false;

		Seed parent = getParentSeed(currentGrid, maxX, maxY);
		if (parent == null) return false;
		if (parent?.rootCount >= parent?.maxRootCount) return false;

		List<(int, int)> possibleGrowthPositions = [];

		for (int nx = x - 1; nx <= x + 1; nx++)
		{
			for (int ny = y; ny <= y + 1; ny++)
			{
				if (isValidRootGrowthPosition(oldGrid, nx, ny, maxX, maxY))
				{
					possibleGrowthPositions.Add((nx, ny));
				}
			}
		}
		if (possibleGrowthPositions.Count > 0)
		{
			var rand = new Random();
			var chosenPos = possibleGrowthPositions[rand.Next(possibleGrowthPositions.Count)]; // found this online
			currentGrid[chosenPos.Item1, chosenPos.Item2] = new Root(parentSeed);
			parent.rootCount++;
			nutrient -= 1f;
			wetness -= 0.2f;
			return true;
		}
		else
		{
			return false;
		}
	}

	override public void update(Element[,] oldGrid, int x, int y, int maxX, int maxY, int T)
	{
		// if parent seed no longer exists, turn into soil with same nutrient and wetness to not lose resources
		Seed parent = getParentSeed(oldGrid, maxX, maxY);
		if (parent == null || parent.plantState == Seed.PlantState.Dying)
		{
			if (Random.Shared.NextSingle() > 0.01f) return; // 99% chance to delay transformation to biomass
			Biomass biomass = new Biomass(wetness, nutrient);
			GridManager.Instance.RequestDeletion(x, y, maxX, maxY, biomass);
			return;
		}

		// take nutrients from soil adjacent to root in cardinal directions
		if (T - lastActivity >= activityInterval)
		{
			lastActivity = T;
			absorbNutrientsAndWetness(oldGrid, x, y, maxX, maxY);
			transferNutrientsUpwards(oldGrid, x, y, maxX, maxY);
			growRoot(oldGrid, oldGrid, x, y, maxX, maxY);
		}

		updateColor(T, x, y);
	}

	public override string getState()
    {
		return base.getState() + ";"
			+ ";" + parentSeed.Item1
			+ ";" + parentSeed.Item2
			+ ";" + lastActivity
			+ ";" + lastGrowthTick;
    }

	override public int setState(string state)
	{
		int i = base.setState(state);;
		string[] stateArgs = state.Split(";", false);
		parentSeed.Item1 = stateArgs[i++].ToInt();
		parentSeed.Item2 = stateArgs[i++].ToInt();
		lastActivity = stateArgs[i++].ToInt();
		lastGrowthTick = stateArgs[i++].ToInt();
		return i;
	}
	
	override public string inspectInfo()
	{
		return $"  Wetness: {wetness:F3}\n  Nutrient: {nutrient:F3}\n  Parent Seed: ({parentSeed.Item1}, {parentSeed.Item2})\n";
	}
} 