using Godot;
using System;

public class Oil : Element, ILiquid, IFlammable
{
	public int directionX { get; set; } = 1;
	public int maxLifetime { get; set; } = 60 * 3;
	public int lifetime { get; set; }

	public bool burning { get; set; } = false;
	public double flammability { get; set; } = 0.2; // the chance that the element will catch fire when in contact with fire
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
		ashCreationPercentage = 0;
		wetness = 0.0f;
		modulateColor(0.001f);
	}

	public override void updateColor(int T)
	{
		base.updateColor(T);
		float modulationSpeed = random_offset * 0.005f;
		float modulation = (Mathf.Sin(T * modulationSpeed + random_offset) + 1) / 2;
		float w = modulation * modulationIntensity;
		float z = (1 - modulation) * modulationIntensity;
		color = color.Lightened(w);
		color = color.Darkened(z);
	}

	override public bool move(Element[,] oldElementArray, Element[,] currentElementArray, int x, int y, int maxX, int maxY, int movementX, int movementY)
	{
		int newX = x + movementX, newY = y + movementY;
		if (newY < 0 || newY >= maxY || newX < 0 || newX >= maxX) return false;

		if (currentElementArray[newX, newY] is Web)
		{
			currentElementArray[x, y] = null;
			currentElementArray[newX, newY] = this;
			return true;
		}
		return base.move(oldElementArray, currentElementArray, x, y, maxX, maxY, movementX, movementY);
	}

	public override void update(Element[,] oldElementArray, int x, int y, int maxX, int maxY, int T)
	{

		if (lifetime <= 0
		&& !burning
		&& (y - 1 == maxY
		|| (y + 2 < maxY
		&& oldElementArray[x, y + 1] is not ILiquid
		&& oldElementArray[x, y + 2] is not ILiquid))) // I don't remember the reason for the last condition
		{
			GridManager.Instance.RequestDeletion(x, y);
			return;
		}

		LiquidBehavior.update(this, oldElementArray, x, y, maxX, maxY, T);

		burn(oldElementArray, x, y, maxX, maxY, T);
		updateColor(T);
	}
}
