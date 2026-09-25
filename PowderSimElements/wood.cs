using Godot;
public class Wood : Element, ILife, ISolid
{
	public Wood()
	{
		density = 1500;
		color = Colors.Brown;
		flammability = 10;
		ashCreationPercentage = 0.8f;
		modulateColor(0.05f);
	}
	override public void update(Element[,] oldElementArray, Element[,] currentElementArray, int x, int y, int maxX, int maxY, int T)
	{
		burn(oldElementArray, currentElementArray, x, y, maxX, maxY, T);
		updateColor(T);
		// Wood just peacefully exists
	}
}