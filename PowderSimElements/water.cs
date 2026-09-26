using Godot;
using System;

public class Water : Element, ILiquid
{
    public int directionX { get; set; } = 1;
	public int maxLifetime { get; set; } = 60 * 3;
	public int lifetime { get; set; }
	public float modulationIntensity = 0.075f;
	private float random_offset;
	private float evaporationChance = 0.0004f;

	public Water()
	{
		lifetime = maxLifetime;
		random_offset = Random.Shared.NextSingle() * 3.0f;
		density = 5;
		color = Colors.Blue;
		wetness = 1.0f;
		modulateColor(0.05f);
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

	public virtual void onEvaporate(Element[,] currentElementArray, int x, int y)
	{
		if (currentElementArray[x, y] != this) return;
		currentElementArray[x, y] = null;
	}


    // This definitely needs to be refactored, but for now it works
	public override void update(Element[,] oldElementArray, int x, int y, int maxX, int maxY, int T)
	{
		if (wetness <= 0)
		{
			DeleteManager.Instance.AttemptDelete(oldElementArray, x, y);
			return;
		}

		if (rng.Randf() < evaporationChance && y - 1 > 0 && oldElementArray[x, y - 1] == null)
		{
			onEvaporate(currentElementArray, x, y);
			return;
		}

		if (currentElementArray[x, y] != this) return;

		if (lifetime <= 0
		&& !burning
		&& (y - 1 == maxY
		|| (y + 2 < maxY
		&& currentElementArray[x, y + 1] is not ILiquid
		&& currentElementArray[x, y + 2] is not ILiquid)))
		{
			onEvaporate(currentElementArray, x, y);
			return;
		}

		LiquidBehavior.update(this, oldElementArray, x, y, maxX, maxY, T);

		burn(oldElementArray, x, y, maxX, maxY, T);
		updateColor(T);
	}
}
