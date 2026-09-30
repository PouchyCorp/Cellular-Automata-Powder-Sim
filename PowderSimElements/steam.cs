using Godot;
using System;

public class Steam : Element, IGas
{
	public int cloudLineY { get; set; } = 10;
	public float wetness;
	public float maxWetness => 1.0f;
	public bool sleeping = false;

	public Steam(float wetness)
	{
		density = 1;
		color = new Color(Colors.WhiteSmoke.R, Colors.WhiteSmoke.G, Colors.WhiteSmoke.B, 0.05f);
		this.wetness = wetness;
	}
	public override void update(Element[,] oldElementArray, int x, int y, int maxX, int maxY, int T)
	{
		if (Random.Shared.NextSingle() < 0.01f)
		{
			int neighbourCount = 0;
			for (int nx = Math.Max(0, x - 1); nx < Math.Min(x + 1, maxX); nx++)
			{
				for (int ny = Math.Max(0, y - 1); ny < Math.Min(y + 1, maxY); ny++)
				{
					if ((nx, ny) != (x, y) && oldElementArray[nx, ny] is Steam)
					{
						neighbourCount++;
					}
				}
			}

			if (neighbourCount >= 3)
			{
				GridManager.Instance.RequestDeletion(x, y, maxX, maxY, new Water(wetness));
				return;
			}
		}

		if (sleeping)
		{
			sleeping = false;
			return;
		}
		sleeping = true;

		GasBehavior.update(this, oldElementArray, x, y, maxX, maxY, T);

		updateColor(T, x, y);
	}
}
