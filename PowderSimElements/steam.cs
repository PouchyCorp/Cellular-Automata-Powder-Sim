using Godot;
using System;

public class Steam : Element, IGas
{
	public int cloudLineY = 10;
	public bool sleeping = false;

	public Steam()
	{
		density = 1;
		color = new Color(Colors.WhiteSmoke.R, Colors.WhiteSmoke.G, Colors.WhiteSmoke.B, 0.05f);
		flammability = 0;
		wetness = 1.0f;
	}

	public override bool canMoveUpOnElement(Element elementWhereMovement)
	{
		return elementWhereMovement == null || elementWhereMovement.density < density || elementWhereMovement is IGas;
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

	public override void update(Element[,] oldElementArray, Element[,] currentElementArray, int x, int y, int maxX, int maxY, int T)
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
				currentElementArray[x, y] = new Water();
				if (y - 1 >= 0 && currentElementArray[x, y - 1] == null && Random.Shared.NextSingle() < 0.1f)
				{
					currentElementArray[x, y - 1] = new Water();
				}
				return;
			}
		}

		if (sleeping)
		{
			sleeping = false;
			return;
		}
		sleeping = true;

		if (currentElementArray[x, y] != this) return;

		float decision = Random.Shared.NextSingle();
		int distFromCloudLine = Math.Abs(cloudLineY - y) + 1;

		if (decision < 0.25f)
		{
			move(oldElementArray, currentElementArray, x, y, maxX, maxY, -1, 0);
		}
		else if (decision < 0.5f)
		{
			move(oldElementArray, currentElementArray, x, y, maxX, maxY, 1, 0);
		}
		else
		{
			float distr = Random.Shared.NextSingle();
			int dir = 1;
			if (cloudLineY - y >= 0)
			{
				dir = -1;
			}

			if (distr < (1f / distFromCloudLine))
			{
				move(oldElementArray, currentElementArray, x, y, maxX, maxY, 0, dir);
			}
			else if (Random.Shared.NextSingle() > 0.7f)
			{
				move(oldElementArray, currentElementArray, x, y, maxX, maxY, 0, -dir);
			}
		}

		burn(oldElementArray, currentElementArray, x, y, maxX, maxY, T);
		updateColor(T);
	}
}
