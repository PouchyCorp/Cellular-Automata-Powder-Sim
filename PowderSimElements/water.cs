using Godot;
using System;

public class Water : Element, ILiquid, ILife
{
    public int directionX { get; set; } = 1;
	public int maxLifetime { get; set; } = 60 * 3;
	public int lifetime { get; set; }
	public float nutrient { get; set; } = 0f;
	public float maxNutrient => 0f;
	public float wetness { get; set; } = 1.0f;
	public float maxWetness => 1.0f;
	public float modulationIntensity = 0.075f;
	private float random_offset;
	private float evaporationChance = 0.0004f;

	public Water(float starting_wetness = 1.0f)
	{
		lifetime = maxLifetime;
		random_offset = Random.Shared.NextSingle() * 3.0f;
		wetness = starting_wetness;
		density = 5;
		color = Colors.Blue;
		modulateColor(0.05f);
	}

	public override void updateColor(int T, int x, int y)
	{
		base.updateColor(T, x, y);
		float modulationSpeed = random_offset * 0.005f;
		float modulation = (Mathf.Sin(T * modulationSpeed + random_offset) + 1) / 2;
		float w = modulation * modulationIntensity;
		float z = (1 - modulation) * modulationIntensity;
		color = color.Lightened(w);
		color = color.Darkened(z);
	}

    // This definitely needs to be refactored, but for now it works
	public override void update(Element[,] oldGrid, int x, int y, int maxX, int maxY, int T)
	{
		if (wetness <= 0)
		{
			GridManager.Instance.RequestDeletion(x, y, maxX, maxY, null);
			return;
		}

		if (y - 1 > 0 && oldGrid[x, y - 1] == null)
		{
			UpdateManager.Instance.RequestUpdateNextFrame(x, y); // To not get frozen

			if (Random.Shared.NextSingle() < evaporationChance){
				GridManager.Instance.RequestDeletion(x, y, maxX, maxY, new Steam(wetness));
				return;
			}
		}

		if (lifetime <= 0)
		{
			if (wetness > 0)
			{
				GridManager.Instance.RequestDeletion(x, y, maxX, maxY, new Steam(wetness));
			}
			else
			{
				GridManager.Instance.RequestDeletion(x, y, maxX, maxY, null);
			}
			return;
		}

		LiquidBehavior.update(this, oldGrid, x, y, maxX, maxY, T);

		updateColor(T, x, y);
	}


}
