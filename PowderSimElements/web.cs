using Godot;

public class Web : Element, ILife, IFlammable
{
	private int lifetime = 100 * 60; // ticks

	public int flammability { get; set; } = 50;
	public bool burning { get; set; } = false;
	public int burningLifetime { get; set; }
	public Web()
	{
		density = 1;
		color = Colors.White;
	}

	public void resetLifetime()
	{
		lifetime = 100 * 60;
	}
	public override void update(Element[,] oldGrid, int x, int y, int maxX, int maxY, int T)
	{
		lifetime--;
		if (lifetime <= 0)
		{
			GridManager.Instance.RequestDeletion(x, y, maxX, maxY);
			return;
		}
		FlammableBehavior.burn(this, oldGrid, x, y, maxX, maxY, T);
		updateColor(T, x, y);
	}


}
