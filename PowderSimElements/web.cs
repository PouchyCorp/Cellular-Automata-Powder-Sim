using Godot;

public class Web : Element, ILife, ISolid
{
	private int lifetime = 100 * 60; // ticks
	public Web()
	{
		ashCreationPercentage = 0.0f;
		density = 3;
		color = Colors.White;
		flammability = 3;
	}

	public void resetLifetime()
	{
		lifetime = 100 * 60;
	}
	public override void update(Element[,] oldElementArray, Element[,] currentElementArray, int x, int y, int maxX, int maxY, int T)
	{

		lifetime--;
		if (lifetime <= 0 && currentElementArray[x, y] == this)
		{
			currentElementArray[x, y] = null;
			return;
		}
		burn(oldElementArray, currentElementArray, x, y, maxX, maxY, T);
		updateColor(T);
	}


}
