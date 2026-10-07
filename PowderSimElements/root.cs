using System;
using System.Collections.Generic;
using Godot;
public class Root : Element, ILife, ISolid
{
	public float nutrient { get; set; } = 0f;
	public float maxNutrient => 10f;
	public float wetness { get; set; } = 0f;
	public float maxWetness => 1f;
	
	private (int, int) parent;
	private bool firstRoot;
	private int distance; // Decreasing starting from the seed
	private int lastActivity = 0;
	private int activityInterval = 30;
	private int lastGrowthTick = 0;
	public enum RootState {
		Alive,
		Dying
	}
	public RootState rootState = RootState.Alive;

	public Root() {} // DO NOT USE EXCEPT IF YOU'RE GONNA SET A STATE RIGHT AFTER

	public Root((int, int) parent, int distance, bool isFirst)
	{
		this.parent = parent;
		this.distance = distance;
		this.firstRoot = isFirst; // Premi�re racine (connect�e � la graine)
		density = 21;
		color = Colors.SandyBrown;
		modulateColor();
	}

	/// <summary>
	/// Returns the parent seed if it still exists, otherwise null
	/// </summary>
	public Seed getParentSeed(Element[,] currentGrid, int maxX, int maxY)
	{
		if (currentGrid[parent.Item1, parent.Item2] is Seed seed)
		{
			return seed;
		}
		return null;
	}

	public Root getParentRoot(Element[,] currentGrid, int maxX, int maxY) {
		if (currentGrid[parent.Item1, parent.Item2] is Root root)
		{
			return root;
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

	private void transferNutrientsToParentSeed(Element[,] currentGrid, int x, int y, int maxX, int maxY)
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
			/* seed.nutrient += transferableNutrients;
			nutrient -= transferableNutrients;
			seed.wetness += transferableWetness;
			wetness -= transferableWetness; */

			NutrientManager.Instance.AddGiveNutrientRequest(new GiveNutrientRequest(x, y, parent.Item1, parent.Item2, transferableNutrients), maxX, maxY);
			NutrientManager.Instance.AddGiveWetnessRequest(new GiveWetnessRequest(x, y, parent.Item1, parent.Item2, transferableWetness), maxX, maxY);
			
		}
	}

	private void transferNutrientsToParentRoot(Element[,] currentGrid, int x, int y, int maxX, int maxY)
	{
		// transfer nutrients to parent root if it exists
		Root root = getParentRoot(currentGrid, maxX, maxY);
		if (root != null)
		{
			if (root.nutrient >= root.maxNutrient && root.wetness >= 1f) return; // parent root full

			float transferableNutrients = Math.Max(nutrient - 1, 0); // keep at least 1 nutrient in root
			transferableNutrients = Math.Min(transferableNutrients, root.maxNutrient - root.nutrient); // don't overfill root
			float transferableWetness = Math.Max(wetness - 0.2f, 0); // keep at least 0.2 wetness in root
			transferableWetness = Math.Min(transferableWetness, 1f - root.wetness); // don't overfill root
			/* root.nutrient += transferableNutrients;
			nutrient -= transferableNutrients;
			root.wetness += transferableWetness;
			wetness -= transferableWetness; */

			NutrientManager.Instance.AddGiveNutrientRequest(new GiveNutrientRequest(x, y, parent.Item1, parent.Item2, transferableNutrients), maxX, maxY);
			NutrientManager.Instance.AddGiveWetnessRequest(new GiveWetnessRequest(x, y, parent.Item1, parent.Item2, transferableWetness), maxX, maxY);
			
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

	public bool growRoot(Element[,] currentGrid, int x, int y, int maxX, int maxY)
	{
		
		if (nutrient < 1) return false;
		if (wetness < 0.2f) return false;
		if (y + 1 >= maxY) return false;

		if (distance < 0) return false;

		List<(int, int)> possibleGrowthPositions = [];

		for (int nx = x - 1; nx <= x + 1; nx	++)
		{
			for (int ny = y; ny <= y + 1; ny++) // Only down
			{
				if (isValidRootGrowthPosition(currentGrid, nx, ny, maxX, maxY))
				{
					possibleGrowthPositions.Add((nx, ny));
				}
			}
		}
		if (possibleGrowthPositions.Count > 0)
		{
			var rand = new Random();
			var chosenPos = possibleGrowthPositions[rand.Next(possibleGrowthPositions.Count)]; // found this online
			// currentGrid[chosenPos.Item1, chosenPos.Item2] = new Root(parentSeed);
			if (GridManager.Instance.RequestDeletion(chosenPos.Item1, chosenPos.Item2, maxX, maxY, new Root((x, y), distance - 1, false))) {
				nutrient -= 1f;
				wetness -= 0.2f;
				return true;
			} else {
				return false;
			}
	
		}
		else
		{
			return false;
		}
	}

	private bool shouldDie(Seed parentSeed, Root parentRoot) {
		return false;
		// return (firstRoot && (parentSeed == null || parentSeed.plantState == Seed.PlantState.Dying)) || (!firstRoot && (parentRoot == null || parentRoot.rootState == Root.RootState.Dying));
	}
	override public void update(Element[,] currentGrid, int x, int y, int maxX, int maxY, int T)
	{
		UpdateManager.Instance.RequestUpdateNextFrame(x, y); // request an update for the snail every frame
		// if parent seed no longer exists, turn into soil with same nutrient and wetness to not lose resources
		Seed parentSeed = getParentSeed(currentGrid, maxX, maxY);
		Root parentRoot = getParentRoot(currentGrid, maxX, maxY);

		if (shouldDie(parentSeed, parentRoot)) {
			this.rootState = RootState.Dying;
			if (Random.Shared.NextSingle() > 0.01f) return; // 99% chance to delay transformation to biomass
			Biomass biomass = new Biomass(wetness, nutrient);
			GridManager.Instance.RequestDeletion(x, y, maxX, maxY, biomass);
			return;
		}

		// take nutrients from soil adjacent to root in cardinal directions
		if (T - lastActivity >= activityInterval)
		{
			lastActivity = T;
			absorbNutrientsAndWetness(currentGrid, x, y, maxX, maxY);
			transferNutrientsToParentRoot(currentGrid, x, y, maxX, maxY); // Fait rien si pas firstRoot
			transferNutrientsToParentSeed(currentGrid, x, y, maxX, maxY);
			growRoot(currentGrid, x, y, maxX, maxY);
		}

		updateColor(T, x, y);
	}

	public override string getState()
	{
		return base.getState() + ";"
			+ ";" + parent.Item1
			+ ";" + parent.Item2
			+ ";" + lastActivity
			+ ";" + lastGrowthTick;
	}

	override public int setState(string state)
	{
		int i = base.setState(state);;
		string[] stateArgs = state.Split(";", false);
		parent.Item1 = stateArgs[i++].ToInt();
		parent.Item2 = stateArgs[i++].ToInt();
		lastActivity = stateArgs[i++].ToInt();
		lastGrowthTick = stateArgs[i++].ToInt();
		return i;
	}
	
	override public string inspectInfo()
	{
		return $"  Wetness: {wetness:F3}\n  Nutrient: {nutrient:F3}\n  Parent: ({parent.Item1}, {parent.Item2})\n";
	}
} 
