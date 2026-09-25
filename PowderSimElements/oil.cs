using Godot;
using System;

public class Oil : Element, ILiquid
{
	private int directionX = 1;
	private int maxLifetime = 60 * 3;
	private int lifetime;
	public float modulationIntensity = 0.02f;
	private float random_offset;

	public Oil()
	{
		lifetime = maxLifetime;
		directionX = 2 * rng.RandiRange(0, 1) - 1;
		random_offset = rng.RandfRange(0.0f, 3.0f);
		density = 4;
		color = Colors.LightYellow;
		flammability = 5;
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
		&& currentElementArray[x, y + 1] is not ILiquid
		&& currentElementArray[x, y + 2] is not ILiquid)))
		{
			DeletionManager.Instance.AttemptDelete(oldElementArray, x, y);
			return;
		}

		LiquidBehavior.update(this, oldElementArray, x, y, maxX, maxY, T);

		burn(oldElementArray, currentElementArray, x, y, maxX, maxY, T);
		updateColor(T);
	}
}
