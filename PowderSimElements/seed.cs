using System;
using Godot;

public class Seed : Element, ILife, ISolid
{
	public float wetness { get; set; }
	public float maxWetness => 2.0f;
	public float nutrient { get; set; }
	public float maxNutrient => 5.0f;

	public float lifetime = 600 * 60; // ticks
	private int lastGrowthTick = 0;
	private int growthInterval = 1 * 60; // ticks

	public int floweringLifetime = 120 * 60; // ticks
	private int floweringTime;

	public int growthDuration = 60 * 60 ; // ticks
	private int growingTime;

	public Color plantColor = Colors.Green;
	public Color basePlantColor = Colors.Green;
	public Color darkerPlantColor = Colors.DarkGreen;
	public int maxFruitCount;
	public int fruitCount = 0;

	private (int, int) startingLeaf = (-1, -1);
	private (int, int) startingRoot = (-1, -1);
	public PlantState plantState = PlantState.Falling;

	public enum PlantState
	{
		Falling = 0,
		Seed = 1,
		Growing = 2,
		Flowering = 3,
		Dying = 4
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
		if (y + 1 < maxY && currentGrid[x, y + 1] is Soil soil)
		{
			if (GridManager.Instance.RequestDeletion(x, y + 1, maxX, maxY, new Root((x, y), 7, true, soil.nutrient, soil.wetness))) // la distance décroit jusqu'a 0 en bout de racine (oui c'est pas logique)
			{
				nutrient -= Root.BASE_ROOT_NUTRIENT_COST;
				wetness -= Root.BASE_ROOT_WETNESS_COST;
				startingRoot = (x, y + 1);
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
		basePlantColor = basePlantColor.Lerp(Colors.Yellow, w / 1.5f);
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

	private void growStartingLeaf(Element[,] oldGrid, int x, int y, int maxX, int maxY)
	{
		if (nutrient < 1) return;
		if (y - 1 < 0) return;

		int leafCount = Random.Shared.Next(10, 20); // random leaf count

		if (GridManager.Instance.RequestSpawn(x, y - 1, new Leaf((-1, -1), leafCount), maxX, maxY)) // there is a redundant check in the RequestSpawn method, but it's fine to have it here as well
		{
			startingLeaf = (x, y - 1);
			nutrient -= Leaf.BASE_LEAF_NUTRIENT_COST;
			wetness -= Leaf.BASE_LEAF_WETNESS_COST;
			return;
		}

		return;
	}

	private void transferNutrientsUpwards(Element[,] oldGrid, int x, int y, int maxX, int maxY, int T)
	{
		if (oldGrid[startingLeaf.Item1, startingLeaf.Item2] is Leaf)
		{
			NutrientManager.Instance.AddGiveNutrientRequest(new GiveNutrientRequest(x, y, x, y - 1, 0.1f), maxX, maxY); // transfer 0.1 nutrient per tick
			NutrientManager.Instance.AddGiveWetnessRequest(new GiveWetnessRequest(x, y, x, y - 1, 0.1f), maxX, maxY); // transfer 0.1 wetness per tick
		}
		else
		{
			GD.Print("Seed: First leaf no longer exists, plant is dying. Above: " + oldGrid[x, y - 1]);
			changePlantState(PlantState.Dying, T); // first leaf no longer exists, die

		}
	}

	public void changePlantState(PlantState newState, int T)
	{
		switch (newState)
		{
			case PlantState.Falling:
				// No special action needed for falling state
				break;
			case PlantState.Seed:
				// No special action needed for seed state
				break;
			case PlantState.Growing:
				growingTime = T; 
				break;
			case PlantState.Flowering:
				floweringTime = T;
				break;
			case PlantState.Dying:
				// No special action needed for dying state
				break;
		}

		plantState = newState;
	}

	public override void update(Element[,] oldGrid, int x, int y, int maxX, int maxY, int T)
	{
		UpdateManager.Instance.RequestUpdateNextFrame(x, y); // request an update for the seed every frame

		switch (plantState)
		{
			// -- Falling state --
			case PlantState.Falling:
				{
					if (y + 1 >= maxY) break;
					MoveManager.Instance.AttemptMove(oldGrid, x, y, 0, 1, maxX, maxY);
					if (oldGrid[x, y + 1] is ISolid or IPowder) // if cannot fall further
					{
						changePlantState(PlantState.Seed, T); // become a seed
					}

					if (y == maxY - 1 || y == 0)
					{
						changePlantState(PlantState.Dying, T);
						GD.Print("Seed: Out of bounds, plant is dying.");
					}
					break;
				}

			// -- Seed state --
			case PlantState.Seed:
				{

					// Try to grow roots first
					growStartingRoot(oldGrid, x, y, maxX, maxY);
					// try to grow leaves
					growStartingLeaf(oldGrid, x, y, maxX, maxY);

					if (startingLeaf != (-1, -1) && startingRoot != (-1, -1)) // if we have a leaf and a root, we can start growing
					{
						changePlantState(PlantState.Growing, T);
					}

					lifetime--;

					if (lifetime <= 0)
					{
						changePlantState(PlantState.Dying, T);
						return;
					}

					break;
				}

			// -- Growing state --
			case PlantState.Growing: // only transfer nutrients if we are in growing phase
				{
					if (T - growingTime >= growthDuration)
					{
						changePlantState(PlantState.Flowering, T);
					}
					
					transferNutrientsUpwards(oldGrid, x, y, maxX, maxY, T);
					break;
				}

			// -- Mature state --
			case PlantState.Flowering:
				{
					if (T - floweringTime >= floweringLifetime)
					{
						changePlantState(PlantState.Dying, T);
						GD.Print("Seed: Flowering time expired, plant is dying.");
					}

					if (oldGrid[x, y - 1] is Leaf firstLeaf)
					{
						if (firstLeaf.leafState < Leaf.LeafState.Flowering)
						{
							firstLeaf.leafState = Leaf.LeafState.Flowering;
						}
					} else
					{
						GD.Print("Seed: First leaf no longer exists, plant is dying.");
						changePlantState(PlantState.Dying, T);
						break;
					}
					
					transferNutrientsUpwards(oldGrid, x, y, maxX, maxY, T); // continue transferring nutrients while for the flower
					break;
				}


			// -- Dying state --
			case PlantState.Dying: // 1% chance to die definitively each tick
				{
					if (Random.Shared.NextSingle() > 0.01f) break;
					if (startingLeaf != (-1, -1) && oldGrid[startingLeaf.Item1, startingLeaf.Item2] is Leaf firstLeaf)
					{
						firstLeaf.leafState = Leaf.LeafState.Dying; // make the leaf die as well
					}
					GridManager.Instance.RequestDeletion(x, y, maxX, maxY, new SurfBiomass(wetness, nutrient)); // add the creation nutrient and wetness
					break;
				}

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
			+ ";" + (int)plantState;
	}

	override public int setState(string state)
	{
		int i = base.setState(state); ;
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
		return base.inspectInfo() + $"  Seed Nutrient: {nutrient:F3}\n  Seed Wetness: {wetness:F3}\n  Plant State: {plantState}\n";
	}

}
