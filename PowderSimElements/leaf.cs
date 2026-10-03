using Godot;
using System;
using System.Collections.Generic;

// TODO : Remove the need of getParentSeed() -> make the seed signaling be a real signal that is transmitted leaf to leaf. The growth must also be driven by nutrient and wetness transfer, and triggered when enough nutrient and wetness is stored in the leaf. 
public class Leaf : Element, ILife, ISolid, IFlammable
{
	// it takes 0.2 wetness and 1 nutrient to grow a new leaf, and it can transfer 0.2 nutrient and 0.1 wetness to each child leaf per activity tick
	const float BASE_LEAF_WETNESS_COST = 0.2f;
	const float BASE_LEAF_NUTRIENT_COST = 1.0f;
	const float BASE_FRUIT_NUTRIENT_COST = 4.0f;
	const float BASE_FRUIT_WETNESS_COST = 1.0f;

	public float wetness { get; set; } = BASE_LEAF_WETNESS_COST;
	public float maxWetness => 1.0f;
	public float nutrient { get; set; } = BASE_LEAF_NUTRIENT_COST;
	public float maxNutrient => 5.0f;

	public int flammability { get; set; } = 10;
	public bool burning { get; set; } = false;
	public int burningLifetime { get; set; }

	private int lastGrowthTick = 0;
	private int growthInterval = 3 * 60; // ticks
	private LeafState leafState = LeafState.Growing;
	private bool alignedToPlantColor = false;
	private List<(int, int)> childLeafs = [];
	private enum LeafState
	{
		Growing,
		Sleeping,
		Dying
	}

	private (int, int) parentSeed;

	public Leaf() { } // DO NOT USE EXCEPT IF YOU'RE GONNA SET A STATE RIGHT AFTER

	public Leaf((int, int) parentSeed)
	{
		this.parentSeed = parentSeed;
		density = 15;
		color = Colors.Green;
		modulateColor(); // idk why it there
	}
	public Seed getParentSeed(Element[,] oldGrid)
	{
		if (oldGrid[parentSeed.Item1, parentSeed.Item2] is Seed seed)
		{
			return seed;
		}
		return null;
	}

	public void alignToPlantColor(Element[,] oldGrid)
	{
		Seed seed = getParentSeed(oldGrid);
		if (seed != null)
		{
			color = new Color(seed.plantColor);
		}
	}
	private bool IsBehindGrowthDirection(int pos, int origin, int dir)
	{
		return dir != 0 && ((dir > 0 && pos <= origin) || (dir < 0 && pos >= origin));
	}

	private bool isValidLeafGrowthPosition(Element[,] oldGrid, int x, int y, int maxX, int maxY, (int, int) growthDir, (int, int) growthOrigin)
	{
		if (x < 0 || x >= maxX || y < 0 || y >= maxY) return false; // out of bounds
		if (oldGrid[x, y] != null) return false; // must be empty
		int adjacentLeaves = 0;

		for (int nx = x - 1; nx <= x + 1; nx++)
		{
			for (int ny = y - 1; ny <= y + 1; ny++)
			{
				if ((nx, ny) == (x, y)) continue;
				if (nx >= 0 && nx < maxX && ny >= 0 && ny < maxY)
				{
					// ignore everything behind growth direction
					if (IsBehindGrowthDirection(nx, growthOrigin.Item1, growthDir.Item1) ||
						IsBehindGrowthDirection(ny, growthOrigin.Item2, growthDir.Item2))
					{
						continue;
					}

					if (oldGrid[nx, ny] is Leaf)
					{
						adjacentLeaves++;
					}
				}
			}
		}
		if (adjacentLeaves > 0) return false; // prevent too dense leaf growth
		return true;
	}

	private int getScoreForGrowthPosition((int, int) pos, int parentX, int parentY, int seedX, int seedY)
	{
		int score = 0;
		int badScore = 0; // if all possible positions are under this score, the leaf will go permanently to sleep

		// Prefer growing upwards
		if (pos.Item2 < parentY) score += 30;
		
		// Prefer positions closer to the seed horizontally
		int distToSeed = Math.Abs(pos.Item1 - seedX);
		score -= distToSeed * 5;

		// Prefer positions higher up
		score += Math.Abs(pos.Item2 - seedY) * 5;

		// add or subtract 30% of the score randomly, but that variation can not dip a passing score below the badScore (to prevent softlock)
		int variation = (int)(score * (Random.Shared.NextSingle() * 0.6f - 0.3f));
		if (score > badScore)
		{
			score = Math.Max(score + variation, badScore);
		}

		return score;
	}

	private (int, int) getBestGrowthPosition((int, int)[] possiblePositions, int maxX, int maxY, int x, int y, int seedX, int seedY)
	{
		(int, int) bestPos = possiblePositions[0];
		int bestScore = getScoreForGrowthPosition(bestPos, x, y, seedX, seedY);

		foreach (var pos in possiblePositions)
		{
			int score = getScoreForGrowthPosition(pos, x, y, seedX, seedY);
			if (score > bestScore)
			{
				bestScore = score;
				bestPos = pos;
			}
		}

		if (bestScore < 0) // Arbitrary threshold to prevent bad growth
		{
			//GD.Print("No suitable growth position found due to low score. bestScore: " + bestScore);
			leafState = LeafState.Sleeping; // to prevent constant growth attempt
			return (-1, -1);
		}

		return bestPos;
	}

	public bool growLeaf(Element[,] oldGrid, int x, int y, int maxX, int maxY)
	{
		if (nutrient < BASE_LEAF_NUTRIENT_COST) return false;
		if (wetness < BASE_LEAF_WETNESS_COST) return false;
		if (y - 1 < 0) return false;

		List<(int, int)> possibleGrowthPositions = [];

		Seed seed = getParentSeed(oldGrid);
		if (seed?.leafCount >= seed?.maxLeafCount) return false;


		// 3 cardinal growth direction
		foreach ((int dx, int dy) in new (int, int)[] { (-1, 0), (1, 0), (0, -1) })
		{
			int nx = x + dx;
			int ny = y + dy;
			if (isValidLeafGrowthPosition(oldGrid, nx, ny, maxX, maxY, (nx - x, ny - y), (x, y))) // ------------------------------------------ 
			{
				possibleGrowthPositions.Add((nx, ny));
			}
		}

		if (possibleGrowthPositions.Count > 0)
		{
			var chosenPos = getBestGrowthPosition(possibleGrowthPositions.ToArray(), maxX, maxY, x, y, parentSeed.Item1, parentSeed.Item2);
			//var chosenPos = possibleGrowthPositions[rand.Next(possibleGrowthPositions.Count)];
			if (chosenPos == (-1, -1)) return false; // No valid position found
			GridManager.Instance.RequestSpawn(chosenPos.Item1, chosenPos.Item2, new Leaf(parentSeed), maxX, maxY);
			childLeafs.Add(chosenPos);
			var parent = getParentSeed(oldGrid);
			if (parent != null)
			{
				parent.leafCount++;
			}

			// because the Leaf is created with BASE_NUTRIENT_COST and BASE_WETNESS_COST, we need to subtract those from the parent leaf to keep the total nutrient and wetness constant
			// It is not needed to use the NutrientManager here (encapsulation is still here)
			nutrient -= BASE_LEAF_NUTRIENT_COST;
			wetness -= BASE_LEAF_WETNESS_COST;
			return true;
		}
		else
		{
			leafState = LeafState.Sleeping; // to prevent constant growth attempt
			return false;
		}
	}

	public void transferNutrientsToChildLeafs(Element[,] currentGrid)
	{
		if (childLeafs.Count == 0) return;
		float maxNutrientTransferAmountPerChild = Math.Min(nutrient / childLeafs.Count, 0.2f); // max 0.2 nutrient per child per activity tick
		float maxWetnessTransferAmountPerChild = Math.Min(wetness / childLeafs.Count, 0.1f); // max 0.1 wetness per child per activity tick
		foreach (var pos in childLeafs)
		{
			if (currentGrid[pos.Item1, pos.Item2] is Leaf childLeaf)
			{
				// transfer nutrients
				if (childLeaf.nutrient < childLeaf.maxNutrient && nutrient - maxNutrientTransferAmountPerChild > 0)
				{
					float actualTransfer = Math.Min(maxNutrientTransferAmountPerChild, childLeaf.maxNutrient - childLeaf.nutrient);
					nutrient -= actualTransfer;
					childLeaf.nutrient += actualTransfer;
				}

				// transfer wetness
				if (childLeaf.wetness < 1f && wetness - maxWetnessTransferAmountPerChild > 0)
				{
					float actualWetnessTransfer = Math.Min(maxWetnessTransferAmountPerChild, 1f - childLeaf.wetness);
					wetness -= actualWetnessTransfer;
					childLeaf.wetness += actualWetnessTransfer;
				}
			}
		}
	}
	override public void update(Element[,] oldGrid, int x, int y, int maxX, int maxY, int T)
	{
		Seed seed = getParentSeed(oldGrid);
		if (seed == null || seed?.plantState == Seed.PlantState.Dying) // if parent seed is gone or dying, start dying
		{
			leafState = LeafState.Dying;
		}

		if (seed != null
		&& leafState == LeafState.Growing
		&& seed.plantState == Seed.PlantState.Mature
		&& nutrient >= BASE_FRUIT_NUTRIENT_COST
		&& wetness >= BASE_FRUIT_WETNESS_COST
		&& seed.fruitCount < seed.maxFruitCount
		)
		{
			if (Random.Shared.NextSingle() < 0.01f && y - 1 >= 0 && oldGrid[x, y - 1] == null && y + 1 < maxY && oldGrid[x, y + 1] is Leaf) // 1% chance each tick to grow fruit
			{
				// grow fruit
				GridManager.Instance.RequestSpawn(x, y - 1, new Fruit(BASE_FRUIT_NUTRIENT_COST, BASE_FRUIT_WETNESS_COST), maxX, maxY);
				nutrient -= BASE_FRUIT_NUTRIENT_COST;
				wetness -= BASE_FRUIT_WETNESS_COST;
				seed.fruitCount++;
				return;
			}
		}

		if (leafState == LeafState.Dying && Random.Shared.NextSingle() < 0.01f) // 1% chance to die definitively each tick
		{
			// Return the nutrients and wetness to the environment
			SurfBiomass biomass = new SurfBiomass(wetness, nutrient); // add the creation nutrient and wetness
			GridManager.Instance.RequestDeletion(x, y, maxX, maxY, biomass);
			return;
		}
		// Try to grow leaves if possible
		if (leafState == LeafState.Growing && T - lastGrowthTick >= growthInterval)
		{
			lastGrowthTick = T;
			growLeaf(oldGrid, x, y, maxX, maxY);

		}

		if (leafState == LeafState.Sleeping && Random.Shared.NextSingle() < 0.001f) // 0.1% chance to wake up each tick
		{
			leafState = LeafState.Growing;
		}

		transferNutrientsToChildLeafs(oldGrid);

		if (!alignedToPlantColor){
			alignToPlantColor(oldGrid);
			alignedToPlantColor = true;
		}
		FlammableBehavior.update(this, oldGrid, x, y, maxX, maxY, T);
		updateColor(T, x, y);
	}

	override public string getState()
	{
		string childLeafText = "";
		foreach ((int, int) childLeaf in childLeafs) {
			childLeafText += childLeaf.Item1 + ":" + childLeaf.Item2 + ",";
		}
		if (childLeafText == "") childLeafText = "none";

		return base.getState()
		+ ";" + lastGrowthTick
		+ ";" + (int)leafState
		+ ";" + childLeafText
		+ ";" + parentSeed.Item1
		+ ";" + parentSeed.Item2;
		
	}

	override public int setState(string state)
	{
		int i = base.setState(state);
		string[] stateArgs = state.Split(";", false);
		lastGrowthTick = stateArgs[i++].ToInt();
		leafState = (LeafState) stateArgs[i++].ToInt();
		string childLeafTexts = stateArgs[i++];
		parentSeed.Item1 = stateArgs[i++].ToInt();
		parentSeed.Item2 = stateArgs[i++].ToInt();

		//Handle child leafs
		if (childLeafTexts != "none")
		{
			string[] childLeafTextsList = childLeafTexts.Split(",", false);
			foreach (string childLeafTxt in childLeafTextsList)
			{
				string[] coos = childLeafTxt.Split(":", false);
				childLeafs.Add((coos[0].ToInt(), coos[1].ToInt()));
			}
		}

		if (leafState != LeafState.Dying) color = Colors.Green;

		return i;
	}

	override public string inspectInfo()
	{
		return $"  Leaf State: {leafState}\n  Child Leafs: {childLeafs.Count}\n";
	}
}
