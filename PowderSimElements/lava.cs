using Godot;
using System;

public class Lava : Element, ILiquid
{
    public int directionX { get; set; } = 1;
	public int maxLifetime { get; set; } = 60 * 3;
	public int lifetime { get; set; }
	public float modulationIntensity = 0.075f;
	private float random_offset;

	public Lava()
	{
		lifetime = maxLifetime;
		random_offset = Random.Shared.NextSingle() * 3.0f;
		density = 5;
		color = Colors.OrangeRed;
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
		
		// scan around for flammable elements and set them on fire
		for (int dx = -1; dx <= 1; dx++)
		{
			for (int dy = -1; dy <= 1; dy++)
			{
				if (dx == 0 && dy == 0) continue; // skip self
				int nx = x + dx;
				int ny = y + dy;
				if (nx >= 0 && nx < maxX && ny >= 0 && ny < maxY)
				{
					Element neighbor = oldGrid[nx, ny];
					if (neighbor is IFlammable flammableNeighbor)
					{
						if (!flammableNeighbor.burning && Random.Shared.Next(0, 100) < 10) // 10% chance to ignite
						{
							flammableNeighbor.burning = true;
							flammableNeighbor.burningLifetime = 60 * 5; // burn for 5 seconds
						}
					}

					if (neighbor is Water waterNeighbor)
					{
						// If water is adjacent, turn lava into stone
						GridManager.Instance.RequestDeletion(x, y, maxX, maxY, new Stone());
						
						// Also turn the water into steam
						GridManager.Instance.RequestDeletion(nx, ny, maxX, maxY, new Steam(waterNeighbor.wetness));
						return;
					}
				}
			}
		}

		LiquidBehavior.update(this, oldGrid, x, y, maxX, maxY, T);

		updateColor(T, x, y);
	}
}
