using System;
using System.Collections.Generic;
using Godot;
public class Root : Element, ILife, ISolid, IFlammable
{
	public float nutrient { get; set; } = 0f;
	public float maxNutrient { get; set; } = 10f;
	public float maxWetness { get; set; } = 1f;

	public float wetness { get; set; } = 0f;

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
		flammability = 10;
		ashCreationPercentage = 0.8f;
		wetness = 0f;
		modulateColor();
	}

	/// <summary>
	/// Returns the parent seed if it still exists, otherwise null
	/// </summary>
	public Seed getParentSeed(Element[,] currentElementArray, int maxX, int maxY)
	{
		if (currentElementArray[parentSeed.Item1, parentSeed.Item2] is Seed seed)
		{
			return seed;
		}
		return null;
	}
	private bool absorbNutrientsAndWetness(Element[,] currentElementArray, int x, int y, int maxX, int maxY)
	{
		// absorb nutrients and wetness from adjacent soil in cardinal directions
		float absorbedNutrients = 0f;
		float absorbedWetness = 0f;
		(int, int)[] directions = [(0, -1), (0, 1), (-1, 0), (1, 0)];
		
		foreach (var dir in directions)
		{
			int newX = x + dir.Item1;
			int newY = y + dir.Item2;
			if (newX >= 0 && newX < maxX && newY >= 0 && newY < maxY)
			{
				if (currentElementArray[newX, newY] is Soil soil)
				{
					float availableNutrients = Math.Min(this.nutrient, maxNutrient - nutrient - absorbedNutrients);
					availableNutrients = Math.Max(availableNutrients, 0);
					float availableWetness = Math.Min(soil.wetness, 1f - wetness - absorbedWetness); // max wetness is 1
					availableWetness = Math.Max(availableWetness, 0);

					soil.ChangeNutrient(-availableNutrients / 4, currentElementArray, newX, newY, maxX, maxY);
					soil.ChangeWetness(-availableWetness / 4, currentElementArray, newX, newY, maxX, maxY);

					absorbedNutrients += availableNutrients / 4;
					absorbedWetness += availableWetness / 4;
				}
			}
		}

		nutrient += absorbedNutrients;
		wetness += absorbedWetness;
		if (absorbedNutrients > 0 || absorbedWetness > 0) return true;

		return false;
	}

	private void transferNutrientsUpwards(Element[,] currentElementArray, int x, int y, int maxX, int maxY)
	{
		// transfer nutrients to parent seed if it exists
		Seed seed = getParentSeed(currentElementArray, maxX, maxY);
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

	private bool isValidRootGrowthPosition(Element[,] currentElementArray, int x, int y, int maxX, int maxY)
	{
		if (x < 0 || x >= maxX || y < 0 || y >= maxY) return false;
		if (currentElementArray[x, y] is not Soil) return false;
		int adjacentRoots = 0;
		for (int nx = x - 1; nx <= x + 1; nx++)
		{
			for (int ny = y - 1; ny <= y + 1; ny++)
			{
				if ((nx, ny) == (x, y)) continue;
				if (nx >= 0 && nx < maxX && ny >= 0 && ny < maxY)
				{
					if (currentElementArray[nx, ny] is Root)
					{
						adjacentRoots++;
					}
				}
			}
		}
		if (adjacentRoots > 1) return false;
		return true;
	}

	public bool growRoot(Element[,] oldElementArray, Element[,] currentElementArray, int x, int y, int maxX, int maxY)
	{
		if (nutrient < 1) return false;
		if (wetness < 0.2f) return false;
		if (y + 1 >= maxY) return false;

		Seed parent = getParentSeed(currentElementArray, maxX, maxY);
		if (parent == null) return false;
		if (parent?.rootCount >= parent?.maxRootCount) return false;

		List<(int, int)> possibleGrowthPositions = [];

		for (int nx = x - 1; nx <= x + 1; nx++)
		{
			for (int ny = y; ny <= y + 1; ny++)
			{
				if (isValidRootGrowthPosition(oldElementArray, nx, ny, maxX, maxY))
				{
					possibleGrowthPositions.Add((nx, ny));
				}
			}
		}
		if (possibleGrowthPositions.Count > 0)
		{
			var rand = new Random();
			var chosenPos = possibleGrowthPositions[rand.Next(possibleGrowthPositions.Count)]; // found this online
			currentElementArray[chosenPos.Item1, chosenPos.Item2] = new Root(parentSeed);
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

	override public void update(Element[,] oldElementArray, int x, int y, int maxX, int maxY, int T)
	{
		// if parent seed no longer exists, turn into soil with same nutrient and wetness to not lose resources
		Seed parent = getParentSeed(oldElementArray, maxX, maxY);
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
			absorbNutrientsAndWetness(oldElementArray, x, y, maxX, maxY);
			transferNutrientsUpwards(oldElementArray, x, y, maxX, maxY);
			growRoot(oldElementArray, oldElementArray, x, y, maxX, maxY);
		}

		burn(oldElementArray, currentElementArray, x, y, maxX, maxY, T);
		updateColor(T);
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