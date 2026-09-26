using Godot;

public class Fruit : Element, ILife, ISolid
{
	public bool pollinated = false;
	private int lifetimeOnSoil = 300 * 60; // ticks
	public Fruit()
	{
		ashCreationPercentage = 0.0f;
		density = 40;
		color = Colors.Red;
		flammability = 3;
	}

    public override void updateColor(int T)
    {
		if (pollinated)
		{
			color = Colors.DarkRed;
		}
        base.updateColor(T);
    }

	public override void update(Element[,] oldElementArray, Element[,] currentElementArray, int x, int y, int maxX, int maxY, int T)
	{
		if (y + 1 >= maxY) return; // out of bounds below

		if (oldElementArray[x, y + 1] is not Leaf)
		{
			lifetimeOnSoil--;
		}
		if (lifetimeOnSoil <= 0 && currentElementArray[x, y] == this)
		{
			// Fruit has withered away
			SurfBiomass biomass = new SurfBiomass(wetness, nutrient);
			currentElementArray[x, y] = biomass;
			return;
		}

		if (currentElementArray[x, y + 1] == null || currentElementArray[x, y + 1] is IGas || currentElementArray[x, y + 1] is ILiquid)
		{

			// chance to strafe left or right while falling
			if (Random.Shared.NextSingle() < 0.4f)
			{
				if (rng.RandiRange(0, 1) == 0)
				{
					if (x - 1 >= 0 && (currentElementArray[x - 1, y + 1] == null || currentElementArray[x - 1, y + 1] is IGas || currentElementArray[x - 1, y + 1] is ILiquid))
						move(oldElementArray, currentElementArray, x, y, maxX, maxY, -1, 0);
					return;
				}
				else
				{
					if (x + 1 < maxX && (currentElementArray[x + 1, y + 1] == null || currentElementArray[x + 1, y + 1] is IGas || currentElementArray[x + 1, y + 1] is ILiquid))
						move(oldElementArray, currentElementArray, x, y, maxX, maxY, 1, 0);
					return;
				}
			}

			move(oldElementArray, currentElementArray, x, y, maxX, maxY, 0, 1);
			return;
		}

		// if polliated and on soil, try to grow a seed
		if (pollinated && oldElementArray[x, y + 1] is Soil)
		{
			// 1% chance each tick to grow a seed
			if (GD.Randf() < 0.01f)
			{
				currentElementArray[x, y] = new Seed();
				return;
			}
		}

		burn(oldElementArray, currentElementArray, x, y, maxX, maxY, T);
		updateColor(T);
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
