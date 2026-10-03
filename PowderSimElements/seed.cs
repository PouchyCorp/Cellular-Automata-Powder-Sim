using System;
using Godot;

public class Seed : Element, ILife, ISolid
{
	public float wetness { get; set; }
	public float maxWetness => 1.0f;
	public float nutrient { get; set; }
	public float maxNutrient => 5.0f;

	public float lifetime = 600 * 60; // ticks
	private int lastGrowthTick = 0;
	private int growthInterval = 1 * 60; // ticks

	public int matureLifetime = 120 * 60; // ticks
	private int maturityTime;

	public Color plantColor = Colors.Green;
	public Color basePlantColor = Colors.Green;
	public Color darkerPlantColor = Colors.DarkGreen;

	public int maxLeafCount;
	public int leafCount = 0;
	
	public int maxRootCount;
	public int rootCount = 0;

	public int maxFruitCount;
	public int fruitCount = 0;

	private (int, int) startingLeaf = (-1, -1);
	public PlantState plantState = PlantState.Falling;

	public enum PlantState
	{
		Falling,
		Seed,
		Growing,
		Mature,
		Dying
	}
	public Seed(float startingWetness, float startingNutrients)
	{
		wetness = startingWetness;
		nutrient = startingNutrients;
		maxLeafCount = Random.Shared.Next(30, 50);
		maxRootCount = Random.Shared.Next(10, 20);
		maxFruitCount = Random.Shared.Next(1, 2);
		density = 15;
		color = Colors.Burlywood;
		setPlantColor();
	}

	private bool growStartingRoot(Element[,] currentGrid, int x, int y, int maxX, int maxY)
	{
		// Try to grow root downwards if there's space
		if (y + 1 < maxY && currentGrid[x, y + 1] is Soil)
		{
			GridManager.Instance.RequestDeletion(x, y + 1, maxX, maxY, new Root((x, y)));
			rootCount++;
			nutrient -= 1f;
			return true;
		}
		return false;
	}
	
	private void setPlantColor()
	{
		// same to modulateColor



		float w = Random.Shared.NextSingle() * 0.7f;
		float z = Random.Shared.NextSingle() * 0.4f;
		basePlantColor = basePlantColor.Lerp(Colors.Yellow, w/1.5f);
		basePlantColor = basePlantColor.Lerp(Colors.Brown, z);

		Color modulatedColor = basePlantColor.Lightened(w);
		modulatedColor = modulatedColor.Darkened(z);

		darkerPlantColor = modulatedColor.Darkened(0.5f);
		basePlantColor = modulatedColor;
	}
	private void updatePlantColor()
	{
		if (leafCount == 0) return;
		// Update plant color based on number of leaves (more leaves = lighter green)
		float t = Math.Min((float)leafCount / (maxLeafCount - 20), 1f);
		plantColor = darkerPlantColor.Lerp(basePlantColor, t);

		if (plantState == PlantState.Dying)
		{
			// slowly turn brown when dying (currently not implemented)
			plantColor = plantColor.Lerp(Colors.SaddleBrown, 0.01f);
		}
	}

	private (int, int) growStartingLeaf(Element[,] oldGrid, int x, int y, int maxX, int maxY)
	{
		if (nutrient < 1) return (-1, -1); // not enough nutrient to grow
		if (y - 1 < 0) return (-1, -1); // no space above

		// Try to grow leaves upwards if there's space
		if (oldGrid[x, y - 1] == null)
		{
			GridManager.Instance.RequestSpawn(x, y - 1, new Leaf((x, y)), maxX, maxY); // there is a redundant check in the RequestSpawn method, but it's fine to have it here as well
			startingLeaf = (x, y - 1);
			nutrient -= 1f;
			leafCount++;
			return (x, y - 1);
		}

		return (-1, -1);
	}

	private void transferNutrientsUpwards(Element[,] oldGrid, int x, int y, int maxX, int maxY)
	{
		if (y - 1 < 0) return; // no space above
		if (oldGrid[x, y-1] is Leaf firstLeaf)
		{
			if (firstLeaf.nutrient < 5f)
			{
				float transferAmount = Math.Min(nutrient, 0.5f); // transfer up to 0.5 nutrient per tick
				transferAmount = Math.Min(transferAmount, firstLeaf.maxNutrient - firstLeaf.nutrient); // don't overfill leaf
				nutrient -= transferAmount;
				firstLeaf.nutrient += transferAmount;
			}

			// same thing with wetness
			if (firstLeaf.wetness < 1f)
			{
				float wetnessTransfer = Math.Min(wetness, 0.5f); // transfer up to 0.5 wetness per tick
				wetnessTransfer = Math.Min(wetnessTransfer, 1f - firstLeaf.wetness); // don't overfill leaf
				wetness -= wetnessTransfer;
				firstLeaf.wetness += wetnessTransfer;
			}

		}
		else
		{
			plantState = PlantState.Dying; // first leaf no longer exists, die
		}
	}
	public override void update(Element[,] oldGrid, int x, int y, int maxX, int maxY, int T)
	{
		if (y == maxY - 1 || y == 0){
			plantState = PlantState.Dying;
		}

		lifetime--;
		// if it is uprooted why not being in a seed state, it dies
		if (plantState != PlantState.Dying && plantState != PlantState.Seed && (lifetime <= 0 || oldGrid[x, y + 1] is not Root))
		{
			// Seed has withered away
			plantState = PlantState.Dying;
		}

		// -- Falling state --
		if (y + 1 < maxY && plantState == PlantState.Falling)
		{
			if (!MoveManager.Instance.AttemptMove(oldGrid, x, y, 0, 1, maxX, maxY)) // if cannot fall further
			{
				plantState = PlantState.Seed; // become a seed
			}
		}

		// -- Seed state --
		if (plantState == PlantState.Seed && T - lastGrowthTick >= growthInterval)
		{
			lastGrowthTick = T;
			// Try to grow roots first
			if (y + 1 < maxY && oldGrid[x, y + 1] is not Soil && oldGrid[x, y + 1] is not Root)
			{
				plantState = PlantState.Dying; // no soil below, die. Poor thing :(
				return;
			}

			if (y - 1 >= 0 && oldGrid[x, y - 1] is Leaf)
			{
				plantState = PlantState.Dying; // no space above to grow leaves, die. Poor thing :(
				return;
			}
			
			if (!growStartingRoot(oldGrid, x, y, maxX, maxY))
			{
				// try to grow leaves
				growStartingLeaf(oldGrid, x, y, maxX, maxY);
			}
		}

		// -- Growing state --
		if (plantState == PlantState.Growing && nutrient > 0) // only transfer nutrients if we are in growing phase
		{
			transferNutrientsUpwards(oldGrid, x, y, maxX, maxY);
		}
		if (plantState == PlantState.Growing && leafCount >= maxLeafCount)
		{
			plantState = PlantState.Mature;
			maturityTime = T;
		}
		
		// -- Mature state --
		if (plantState == PlantState.Mature) // stopping transfer of nutrients will stop the growth of leaves
		{
			// fruit logic incoming 
		}
		if (plantState == PlantState.Mature && T - maturityTime >= matureLifetime)
		{
			plantState = PlantState.Dying;
		}

		// -- Dying state --
		if (plantState == PlantState.Dying && Random.Shared.NextSingle() < 0.01f) // 1% chance to die definitively each tick
		{
			GridManager.Instance.RequestDeletion(x, y, maxX, maxY, new SurfBiomass(wetness, nutrient)); // add the creation nutrient and wetness
			return;
		}
		updatePlantColor();
		updateColor(T, x, y);
	}

	public override string getState()
	{
		return base.getState() + ";"
			+ ";" + wetness
			+ ";" + nutrient
			+ ";" + lastGrowthTick
			+ ";" + maturityTime
			+ ";" + leafCount
			+ ";" + rootCount
			+ ";" + startingLeaf.Item1
			+ ";" + startingLeaf.Item2
			+ ";" + (int) plantState;
	}

	override public int setState(string state)
	{
		int i = base.setState(state);;
		string[] stateArgs = state.Split(";", false);
		wetness = stateArgs[i++].ToFloat();
		nutrient = stateArgs[i++].ToFloat();
		lastGrowthTick = stateArgs[i++].ToInt();
		maturityTime = stateArgs[i++].ToInt();
		leafCount = stateArgs[i++].ToInt();
		rootCount = stateArgs[i++].ToInt();
		startingLeaf.Item1 = stateArgs[i++].ToInt();
		startingLeaf.Item2 = stateArgs[i++].ToInt();
		plantState = (PlantState)stateArgs[i++].ToInt();
		return i;
	}

	override public string inspectInfo()
	{
		return base.inspectInfo() + $"  Seed Nutrient: {nutrient:F3}\n  Plant State: {plantState}\n";
	}

}
