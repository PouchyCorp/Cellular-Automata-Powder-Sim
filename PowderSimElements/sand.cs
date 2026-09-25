using Godot;

public class Sand : Element, IPowder, ISolid
{
	public Sand()
	{
		density = 20;
		color = Colors.Yellow;
		modulateColor(0.2f);
	}

	public override bool canMoveDownOnElement(Element elementWhereMovement)
	{
		return PowderBehavior.CanMoveDownOnElement(elementWhereMovement);
	}

	public override bool canMoveSideOnElement(Element elementWhereMovement)
	{
		return PowderBehavior.CanMoveSideOnElement(elementWhereMovement);
	}

	public override void modulateColor(float intensity = 0.05F)
	{
		float w = rng.RandfRange(0.0f, intensity);
		color = color.Lightened(w);
		float z = rng.RandfRange(0.0f, intensity);
		color = color.Darkened(z);
	}

	public override void update(Element[,] oldElementArray, Element[,] currentElementArray, int x, int y, int maxX, int maxY, int T)
	{
		PowderBehavior.Update(this, oldElementArray, currentElementArray, x, y, maxX, maxY, T);
	}
}
