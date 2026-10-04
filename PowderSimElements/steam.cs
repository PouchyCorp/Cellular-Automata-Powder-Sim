using Godot;
using System;

public class Steam : Element, IGas, ILife
{
	public float wetness { get; set; }
	public float nutrient { get; set; } = 0.0f;
	public float maxNutrient => 0.0f;
	public float maxWetness => 1.0f;
	public bool sleeping { get; set; } = false;

	public Steam(float wetness)
	{
		density = 1;
		color = new Color(Colors.WhiteSmoke.R, Colors.WhiteSmoke.G, Colors.WhiteSmoke.B, 0.05f);
		this.wetness = wetness;
	}
	public override void update(Element[,] oldGrid, int x, int y, int maxX, int maxY, int T)
	{
		if (Random.Shared.NextSingle() < 0.01f)
		{
			int neighbourCount = 0;
			for (int nx = Math.Max(0, x - 1); nx < Math.Min(x + 1, maxX); nx++)
			{
				for (int ny = Math.Max(0, y - 1); ny < Math.Min(y + 1, maxY); ny++)
				{
					if ((nx, ny) != (x, y) && oldGrid[nx, ny] is Steam)
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
		GasBehavior.update(this, oldGrid, x, y, maxX, maxY, T);

		updateColor(T, x, y);
	}
}
