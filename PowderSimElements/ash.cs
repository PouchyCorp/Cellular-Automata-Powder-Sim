using Godot;

public class Ash : Element, IPowder, ISolid, INutrient
{
	public Ash()
	{
		density = 19;
		color = Colors.Gray;
		nutrient = 5.0f; // ash is very nutritious (yum :3)
	}

	public override bool canMoveDownOnElement(Element elementWhereMovement)
	{
		return PowderBehavior.CanMoveDownOnElement(elementWhereMovement);
	}

	public override bool canMoveSideOnElement(Element elementWhereMovement)
	{
		return PowderBehavior.CanMoveSideOnElement(elementWhereMovement);
	}

	public override void update(Element[,] oldElementArray, Element[,] currentElementArray, int x, int y, int maxX, int maxY, int T)
	{
		PowderBehavior.Update(this, oldElementArray, currentElementArray, x, y, maxX, maxY, T);
	}
}
