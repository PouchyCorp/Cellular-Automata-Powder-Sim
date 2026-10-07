using Godot;
using System;
using System.Collections.Generic;

// TODO : Remove the need of getParentSeed() -> make the seed signaling be a real signal that is transmitted leaf to leaf. The growth must also be driven by nutrient and wetness transfer, and triggered when enough nutrient and wetness is stored in the leaf. 
public class Leaf : Element, ILife, ISolid, IFlammable
{
	// it takes 0.2 wetness and 1 nutrient to grow a new leaf, and it can transfer 0.2 nutrient and 0.1 wetness to each child leaf per activity tick
	public const float BASE_LEAF_WETNESS_COST = 0.2f;
	public const float BASE_LEAF_NUTRIENT_COST = 1.0f;

	public float wetness { get; set; } = BASE_LEAF_WETNESS_COST;
	public float minWetness => BASE_LEAF_WETNESS_COST;
	public float maxWetness => 1.0f;

	public float nutrient { get; set; } = BASE_LEAF_NUTRIENT_COST;
	public float minNutrient => BASE_LEAF_NUTRIENT_COST;
	public float maxNutrient => 5.0f;

	public int flammability { get; set; } = 10;
	public bool burning { get; set; } = false;
	public int burningLifetime { get; set; }

	private int lastGrowthTick = 0;
	private int growthInterval; // ticks
	public LeafState leafState = LeafState.Growing;
	private List<(int, int)> childLeafs = [];
	private bool sleeping = false;
	public enum LeafState
	{
		Growing = 0,
		Flowering = 1,
		ProducedFruit = 2,
		Dying = 3
		
	}

	private (int, int) parentLeaf; // coordinates of the parent seed
	private int leafCount;

	public Leaf() { } // DO NOT USE EXCEPT IF YOU'RE GONNA SET A STATE RIGHT AFTER

	public Leaf((int, int) parentLeaf, int leafCount)
	{
		this.parentLeaf = parentLeaf;
		this.leafCount = leafCount;
		density = 15;
		color = Colors.Green;
		growthInterval = Random.Shared.Next(30, 60); // random growth interval between 30 and 60 ticks
		modulateColor(); // idk why it there
	}

	override public void updateColor(int T, int x, int y)
	{
		base.updateColor(T, x, y);

		color = baseColor.Lerp(Colors.DarkGreen, Math.Min(nutrient, 1.0f)); // more nutrient = darker color

	}

	private bool isValidLeafGrowthPosition(Element[,] oldGrid, int callingLeafX, int callingLeafY, int x, int y, int maxX, int maxY)
	{
		if (x < 0 || x >= maxX || y < 0 || y >= maxY) return false; // out of bounds
		if (oldGrid[x, y] != null) return false; // must be empty

		int vectorX = x - callingLeafX; // these are to ignore the row on the opposit of the parent leaf (the stick of the parentleaf)
		int vectorY = y - callingLeafY;
		
		int adjacentLeaves = 0;

		for (int nx = x - 1; nx <= x + 1; nx++)
		{
			for (int ny = y - 1; ny <= y + 1; ny++)
			{
				if ((nx, ny) == (x, y)) continue;
				if ((x - nx) == vectorX || (y - ny) == vectorY) continue; // ignore the row on the opposite side of the parent leaf

				if (nx >= 0 && nx < maxX && ny >= 0 && ny < maxY)
				{
					
					if (oldGrid[nx, ny] is Leaf or Root)
					{
						adjacentLeaves++;
					}
				}
			}
		}

		if (adjacentLeaves > 0) return false; // prevent too dense leaf growth
		return true;
	}

	public bool growLeaf(Element[,] oldGrid, int x, int y, int maxX, int maxY)
	{
		if (nutrient - minNutrient < BASE_LEAF_NUTRIENT_COST) return false;
		if (wetness - minWetness < BASE_LEAF_WETNESS_COST) return false;
		if (y - 1 < 0) return false;

		List<(int, int)> possibleGrowthPositions = [];

		// 3 cardinal growth direction
		foreach ((int dx, int dy) in new (int, int)[] { (-1, 0), (1, 0), (0, -1) })
		{
			int nx = x + dx;
			int ny = y + dy;
			if (isValidLeafGrowthPosition(oldGrid, x, y, nx, ny, maxX, maxY))
			{
				possibleGrowthPositions.Add((nx, ny));
			}
		}

		if (possibleGrowthPositions.Count > 0)
		{
			//var chosenPos = getBestGrowthPosition(possibleGrowthPositions.ToArray(), maxX, maxY, x, y, parentSeed.Item1, parentSeed.Item2);
			var chosenPos = possibleGrowthPositions[Random.Shared.Next(possibleGrowthPositions.Count)];
			if (chosenPos == (-1, -1)) return false; // No valid position found

			int decrement = Random.Shared.Next(0, Math.Min(leafCount, 3)); // decrement between 0 and 3 (or leafCount if it's less than 3)

			if (GridManager.Instance.RequestSpawn(chosenPos.Item1, chosenPos.Item2, new Leaf((x, y), leafCount - decrement), maxX, maxY))
			{
				childLeafs.Add(chosenPos);

				// because the Leaf is created with BASE_NUTRIENT_COST and BASE_WETNESS_COST, we need to subtract those from the parent leaf to keep the total nutrient and wetness constant
				// It is not needed to use the NutrientManager here (encapsulation is still here)
				nutrient -= BASE_LEAF_NUTRIENT_COST;
				wetness -= BASE_LEAF_WETNESS_COST;
				return true;
			}
			
		}
		sleeping = true; // no valid growth position, go to sleep until a child leaf is removed
		return false;
	}

	public void transferNutrientsToChildLeafs(Element[,] currentGrid, int x, int y, int maxX, int maxY)
	{
		if (childLeafs.Count == 0) return;

		const float maxTransferPerChild = 0.2f; // limit the transfer to 0.2 nutrient per child leaf
		float availableNutrientsToTransfer = Math.Max(0, nutrient - BASE_LEAF_NUTRIENT_COST);
		float availableWetnessToTransfer = Math.Max(0, wetness - BASE_LEAF_WETNESS_COST);

		float transferAmountPerChildNutrient = Math.Min(maxTransferPerChild, availableNutrientsToTransfer / childLeafs.Count);

		float transferAmountPerChildWetness = Math.Min(maxTransferPerChild, availableWetnessToTransfer / childLeafs.Count);

		foreach ((int childX, int childY) in childLeafs)
		{
			if (currentGrid[childX, childY] is not Leaf childLeaf)
			{
				continue;
			}

			if (leafState > childLeaf.leafState)
			{
				childLeaf.leafState = leafState; // propagate the state to the child leaf
			} else if (leafState < childLeaf.leafState)
			{
				leafState = childLeaf.leafState; // propagate the state from the child leaf to the parent leaf
			}

			if (childLeaf.nutrient >= nutrient) continue; // skip if the child leaf is already full

			NutrientManager.Instance.AddGiveNutrientRequest(new GiveNutrientRequest(x, y, childX, childY, transferAmountPerChildNutrient), maxX, maxY);

			NutrientManager.Instance.AddGiveWetnessRequest(new GiveWetnessRequest(x, y, childX, childY, transferAmountPerChildWetness), maxX, maxY); // transfer half the amount of wetness compared to nutrient
		}
	}
	override public void update(Element[,] oldGrid, int x, int y, int maxX, int maxY, int T)
	{
		UpdateManager.Instance.RequestUpdateNextFrame(x, y); // request an update for the snail every frame

		if (leafState == LeafState.Flowering
		&& nutrient >= Fruit.BASE_FRUIT_NUTRIENT_COST
		&& wetness >= Fruit.BASE_FRUIT_WETNESS_COST
		)
		{
			if (y - 2 >= 0 && oldGrid[x, y - 1] == null && y + 1 < maxY && oldGrid[x, y + 1] is Leaf) // 1% chance each tick to grow fruit
			{
				// grow fruit
				if (GridManager.Instance.RequestSpawn(x, y - 1, new Fruit(), maxX, maxY))
				{
					nutrient -= Fruit.BASE_FRUIT_NUTRIENT_COST;
					wetness -= Fruit.BASE_FRUIT_WETNESS_COST;
					leafState = LeafState.ProducedFruit; // change the state to ProducedFruit
					transferNutrientsToChildLeafs(oldGrid, x, y, maxX, maxY);
					return;
				}
			}
		}

		if (parentLeaf != (-1, -1) && oldGrid[parentLeaf.Item1, parentLeaf.Item2] is not Leaf parentLeafElement)
		{
			leafState = LeafState.Dying; // if the parent leaf is gone, this leaf will die
		}

		if (leafState == LeafState.Dying && Random.Shared.NextSingle() < 0.01f) // 1% chance to die definitively each tick
		{
			// Return the nutrients and wetness to the environment
			SurfBiomass biomass = new SurfBiomass(wetness, nutrient); // add the creation nutrient and wetness
			GridManager.Instance.RequestDeletion(x, y, maxX, maxY, biomass);
			return;
		}
		// Try to grow leaves if possible
		if (leafState == LeafState.Growing && T - lastGrowthTick >= growthInterval && leafCount > 0)
		{
			lastGrowthTick = T;
			growLeaf(oldGrid, x, y, maxX, maxY);

		}

		transferNutrientsToChildLeafs(oldGrid, x, y, maxX, maxY);

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
		+ ";" + childLeafText;
		
	}

	override public int setState(string state)
	{
		int i = base.setState(state);
		string[] stateArgs = state.Split(";", false);
		lastGrowthTick = stateArgs[i++].ToInt();
		leafState = (LeafState) stateArgs[i++].ToInt();
		string childLeafTexts = stateArgs[i++];
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
		return $"  Leaf State: {leafState}\n  Child Leafs: {childLeafs.Count}\n Wetness: {wetness:F3}\n  Nutrient: {nutrient:F3}\n LeafCount: {leafCount}\n Last Growth Tick: {lastGrowthTick}\n";
	}
}
