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

	public override void update(Element[,] oldGrid, int x, int y, int maxX, int maxY, int T)
	{
		GD.Print($"Oil update called at ({x}, {y}) with lifetime {lifetime} and burning {burning}");
		if (lifetime <= 0
		&& !burning)
		{
			GridManager.Instance.RequestDeletion(x, y, maxX, maxY, null);
			return;
		}

		LiquidBehavior.update(this, oldGrid, x, y, maxX, maxY, T);

		FlammableBehavior.update(this, oldGrid, x, y, maxX, maxY, T);
		updateColor(T, x, y);
	}
}
