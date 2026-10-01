using Godot;
using System;

public class Smoke : Element, IGas
{
	int IGas.cloudLineY { get; set; } = 10;
	public bool sleeping = false;

	public Smoke()
	{
		density = 0.1f;
		color = Colors.DarkGray;
	}

	// override public bool move(Element[,] oldGrid, Element[,] currentGrid, int x, int y, int maxX, int maxY, int movementX, int movementY)
	// {
	// 	int newX = x + movementX, newY = y + movementY;
	// 	if (newY < 0 || newY >= maxY || newX < 0 || newX >= maxX) return false;

	// 	if (currentGrid[newX, newY] is Web)
	// 	{
	// 		currentGrid[x, y] = null;
	// 		currentGrid[newX, newY] = this;
	// 		return true;
	// 	}
	// 	return base.move(oldGrid, currentGrid, x, y, maxX, maxY, movementX, movementY);
	// }

	public override void update(Element[,] oldGrid, int x, int y, int maxX, int maxY, int T)
	{
		if (sleeping)
		{
			sleeping = false;
			return;
		}
		sleeping = true;

		GasBehavior.update(this, oldGrid, x, y, maxX, maxY, T);

		updateColor(T, x, y);
	}
}
