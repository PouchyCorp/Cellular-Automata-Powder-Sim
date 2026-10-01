using Godot;
public class Wood : Element, ILife, ISolid, IFlammable
{
	public float nutrient { get; set; } = 0f;
	public float maxNutrient => 10f;
	public float wetness { get; set; } = 0f;
	public float maxWetness => 1f;
	public bool burning { get; set; } = false;
	public int flammability { get; set; } = 20; // the chance that the element will catch fire when in contact with fire
	public int burningLifetime { get; set; } // how long the element has been burning, in ticks
	public Wood()
	{
		density = 1500;
		color = Colors.Brown;
		modulateColor(0.05f);
	}
	override public void update(Element[,] oldGrid, int x, int y, int maxX, int maxY, int T)
	{
		FlammableBehavior.burn(this, oldGrid, x, y, maxX, maxY, T);
		updateColor(T, x, y);
		// Wood just peacefully exists
	}
}