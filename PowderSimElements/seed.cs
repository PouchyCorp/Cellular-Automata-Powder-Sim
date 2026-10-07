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

	public int floweringLifetime = 120 * 60; // ticks
	private int floweringTime;

	public int growthDuration = 60 * 60; // ticks
	private int growingTime;

	public Color plantColor = Colors.Green;
	public Color basePlantColor = Colors.Green;
	public Color darkerPlantColor = Colors.DarkGreen;
	public int maxFruitCount;
	public int fruitCount = 0;

	private (int, int) startingLeaf = (-1, -1);
	public PlantState plantState = PlantState.Falling;

	public enum PlantState
	{
		Falling,
		Seed,
		Growing,
		Flowering,
		Dying
	}
	public Seed(float startingWetness, float startingNutrients)
	{
		wetness = startingWetness;
		nutrient = startingNutrients;
		density = 15;
		color = Colors.Burlywood;
		setPlantColor();
	}

	private bool growStartingRoot(Element[,] currentGrid, int x, int y, int maxX, int maxY)
	{
		// Try to grow root downwards if there's space
		if (y + 1 < maxY && currentGrid[x, y + 1] is Soil)
		{
			if (GridManager.Instance.RequestDeletion(x, y + 1, maxX, maxY, new Root((x, y), 0, true)))
			{
				nutrient -= 1f;
				return true;
			}
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

		if (GridManager.Instance.RequestSpawn(x, y - 1, new Leaf((-1,-1), 10), maxX, maxY)) // there is a redundant check in the RequestSpawn method, but it's fine to have it here as well
		{
			startingLeaf = (x, y - 1);
			nutrient -= Leaf.BASE_LEAF_NUTRIENT_COST;
			wetness -= Leaf.BASE_LEAF_WETNESS_COST;
			plantState = PlantState.Growing;
			return (x, y - 1);
		}

		return (-1, -1);
	}

	private void transferNutrientsUpwards(Element[,] oldGrid, int x, int y, int maxX, int maxY)
	{
		if (y - 1 < 0) return; // no space above
		if (oldGrid[x, y-1] is Leaf firstLeaf)
		{
			NutrientManager.Instance.AddGiveNutrientRequest(new GiveNutrientRequest(x, y, x, y-1, 0.1f), maxX, maxY); // transfer 0.1 nutrient per tick
			NutrientManager.Instance.AddGiveWetnessRequest(new GiveWetnessRequest(x, y, x, y-1, 0.1f), maxX, maxY); // transfer 0.1 wetness per tick
		}
		else
		{
			plantState = PlantState.Dying; // first leaf no longer exists, die
		}
	}
	public override void update(Element[,] oldGrid, int x, int y, int maxX, int maxY, int T)
	{
		UpdateManager.Instance.RequestUpdateNextFrame(x, y); // request an update for the seed every frame

		// debug
		nutrient = maxNutrient; // seeds always have max nutrient
		wetness = maxWetness; // seeds always have max wetness


		if (y == maxY - 1 || y == 0){
			plantState = PlantState.Dying;
		}

		lifetime--;
		// if it is uprooted why not being in a seed state, it dies
		if (plantState != PlantState.Dying && plantState != PlantState.Seed && plantState != PlantState.Falling && (lifetime <= 0 || oldGrid[x, y + 1] is not Root))
		{
			// Seed has withered away
			plantState = PlantState.Dying;
		}

		// -- Falling state --
		if (y + 1 < maxY && plantState == PlantState.Falling)
		{
			if (!MoveManager.Instance.AttemptMove(oldGrid, x, y, 0, 1, maxX, maxY)) // if cannot fall further
			{
				if (y + 1 < maxY && oldGrid[x, y + 1] is not Soil)
				{
					plantState = PlantState.Dying; // no soil below, die. Poor thing :(
					return;
				}
				plantState = PlantState.Seed; // become a seed
			}
		}

		// -- Seed state --
		if (plantState == PlantState.Seed && T - lastGrowthTick >= growthInterval)
		{
			lastGrowthTick = T;
			// Try to grow roots first
			
			growStartingRoot(oldGrid, x, y, maxX, maxY);
			// try to grow leaves
			growStartingLeaf(oldGrid, x, y, maxX, maxY);

			if (startingLeaf != (-1, -1) && y + 1 < maxY && oldGrid[x, y + 1] is Root) // if we have a leaf and a root, we can start growing
			{
				growingTime = T;
				plantState = PlantState.Growing;
			}
		}

		// -- Growing state --
		if (plantState == PlantState.Growing && nutrient > 0) // only transfer nutrients if we are in growing phase
		{
			transferNutrientsUpwards(oldGrid, x, y, maxX, maxY);
		}
		if (plantState == PlantState.Growing && T - growingTime >= growthDuration)
		{
			plantState = PlantState.Flowering;
			floweringTime = T;
		}
		
		// -- Mature state --
		if (plantState == PlantState.Flowering)
		{
			if (oldGrid[x, y-1] is Leaf firstLeaf)
			{
				if (firstLeaf.leafState < Leaf.LeafState.Flowering){
					firstLeaf.leafState = Leaf.LeafState.Flowering;
				}

				if (firstLeaf.leafState == Leaf.LeafState.ProducedSeed){
					// 1% chance to enter the dying state each tick if a seed has been produced
					if (Random.Shared.NextSingle() < 0.01f)
					{
						plantState = PlantState.Dying;
					}
				}
			}
			transferNutrientsUpwards(oldGrid, x, y, maxX, maxY); // continue transferring nutrients while for the flower

		}
		if (plantState == PlantState.Flowering && T - floweringTime >= floweringLifetime)
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
			+ ";" + floweringTime
			+ ";" + growingTime
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
		floweringTime = stateArgs[i++].ToInt();
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
