using Godot;
using System;

public class Oil : Element, ILiquid, IFlammable
{
	public int directionX { get; set; } = 1;
	public int maxLifetime { get; set; } = 60 * 3;
	public int lifetime { get; set; }

	public bool burning { get; set; } = false;
	public int flammability { get; set; } = 20; // the chance that the element will catch fire when in contact with fire
	public int burningLifetime { get; set; } // how long the element has been burning, in ticks
	public float modulationIntensity = 0.02f;
	private float random_offset;

	public Oil()
	{
		lifetime = maxLifetime;
		directionX = 2 * Random.Shared.Next(0, 1) - 1;
		random_offset = Random.Shared.NextSingle() * 3.0f;
		density = 4;
		color = Colors.LightYellow;
		modulateColor(0.001f);
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
	public override void update(Element[,] oldElementArray, int x, int y, int maxX, int maxY, int T)
	{

		if (lifetime <= 0
		&& !burning)
		{
			GridManager.Instance.RequestDeletion(x, y, maxX, maxY, null);
			return;
		}

		LiquidBehavior.update(this, oldElementArray, x, y, maxX, maxY, T);

		FlammableBehavior.burn(this, oldElementArray, x, y, maxX, maxY, T);
		updateColor(T, x, y);
	}
}
