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
		flammability = 0;
		wetness = 0.0f;
	}

	// override public bool move(Element[,] oldElementArray, Element[,] currentElementArray, int x, int y, int maxX, int maxY, int movementX, int movementY)
	// {
	// 	int newX = x + movementX, newY = y + movementY;
	// 	if (newY < 0 || newY >= maxY || newX < 0 || newX >= maxX) return false;

	// 	if (currentElementArray[newX, newY] is Web)
	// 	{
	// 		currentElementArray[x, y] = null;
	// 		currentElementArray[newX, newY] = this;
	// 		return true;
	// 	}
	// 	return base.move(oldElementArray, currentElementArray, x, y, maxX, maxY, movementX, movementY);
	// }

	public override void update(Element[,] oldElementArray, int x, int y, int maxX, int maxY, int T)
	{
		if (sleeping)
		{
			sleeping = false;
			return;
		}
		sleeping = true;

		GasBehavior.update(this, oldElementArray, x, y, maxX, maxY, T);

		burn(oldElementArray, x, y, maxX, maxY, T);
		updateColor(T);
	}
}
