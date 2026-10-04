using Godot;

public class Web : Element, IFlammable
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
		UpdateManager.Instance.RequestUpdateNextFrame(x, y); // request an update for the snail every frame
		lifetime--;
		if (lifetime <= 0)
		{
			GridManager.Instance.RequestDeletion(x, y, maxX, maxY);
			return;
		}
		FlammableBehavior.update(this, oldGrid, x, y, maxX, maxY, T);
		updateColor(T, x, y);
	}


}
