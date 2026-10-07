using Godot;
using System;

public class Fruit : Element, ILife, ISolid, IFlammable
{
	public const float BASE_FRUIT_NUTRIENT_COST = 4.0f;
	public const float BASE_FRUIT_WETNESS_COST = 1.0f;
	public float wetness { get; set; } = BASE_FRUIT_WETNESS_COST;
	public float maxWetness => BASE_FRUIT_WETNESS_COST;
	public float nutrient { get; set; } = BASE_FRUIT_NUTRIENT_COST;
	public float maxNutrient => BASE_FRUIT_NUTRIENT_COST * 2;

	public bool burning { get; set; } = false;
	public int burningLifetime { get; set; }
	public int flammability { get; set; } = 2;
	public bool pollinated = false;
	private int lifetimeOnSoil = 300 * 60; // ticks

	public bool sterile = false; // if true, the fruit will not grow a seed when pollinated and on soil
	public Fruit()
	{
		density = 40;
		color = Colors.LightPink;
		modulateColor();
	}
	
    public override void updateColor(int T, int x, int y)
    {
		if (pollinated)
		{
			color = Colors.DarkRed;
		}
        base.updateColor(T, x, y);
    }

	public bool pollinate()
	{
		if (!pollinated && !sterile)
		{
			pollinated = true;
			return true;
		}
		return false;
	}

	public override void update(Element[,] oldGrid, int x, int y, int maxX, int maxY, int T)
	{
		UpdateManager.Instance.RequestUpdateNextFrame(x, y); // request an update for the snail every frame
		if (y + 1 >= maxY) return; // out of bounds below

		if (oldGrid[x, y + 1] is not Leaf)
		{
			lifetimeOnSoil--;
		}
		if (lifetimeOnSoil <= 0)
		{
			// Fruit has withered away
			SurfBiomass biomass = new SurfBiomass(wetness, nutrient);
			GridManager.Instance.RequestDeletion(x, y, maxX, maxY, biomass);
			return;
		}

		if (oldGrid[x, y + 1] is not ISolid)
		{
			
			int strafeChance = Random.Shared.Next(1, 4);
			// chance to strafe left or right while falling

				// strafe left (25% chance)
			if (strafeChance == 2 && x - 1 >= 0)
			{
				MoveManager.Instance.AttemptMove(oldGrid, x, y, -1, 1, maxX, maxY);
				return;
			}
					
				
			// strafe right (25% chance)
			if (strafeChance == 1 && x + 1 < maxX)
			{
				MoveManager.Instance.AttemptMove(oldGrid, x, y, 1, 1, maxX, maxY);
				return;
			}


			// move down (50% chance)
			MoveManager.Instance.AttemptMove(oldGrid, x, y, 0, 1, maxX, maxY);
			return;
		}

		// if polliated and on soil, try to grow a seed
		if (pollinated && !sterile && oldGrid[x, y + 1] is Soil)
		{
			// 2% chance each tick to grow a seed
			if (Random.Shared.NextSingle() < 0.02f)
			{
				// grow a seed
				GridManager.Instance.RequestDeletion(x, y, maxX, maxY, new Seed(wetness, nutrient));
				return;
			}
		}

		if (pollinated && y + 1 < maxY && oldGrid[x, y + 1] is Leaf leaf) // if pollinated and on a leaf, change the leaf's state (it will propagate to the entire plant)
		{
			leaf.leafState = Leaf.LeafState.ProducedSeed;
		}

		FlammableBehavior.update(this, oldGrid, x, y, maxX, maxY, T);
		updateColor(T, x, y);
	}

	public override string inspectInfo()
	{
		return base.inspectInfo() + $"Pollinated: {pollinated}\nLifetime on soil: {lifetimeOnSoil / 60} seconds\n";
	}

	override public string getState()
	{
		return base.getState() + ";" + pollinated + ";" + lifetimeOnSoil;
	}

	override public int setState(string state)
	{
		int i = base.setState(state);
		string[] stateArgs = state.Split(";", false);
		pollinated = stateArgs[i++] == "True";
		lifetimeOnSoil = stateArgs[i++].ToInt();
		return i;
	}

}
