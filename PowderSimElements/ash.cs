using Godot;

public class Ash : Element, IPowder, ILife
{
	public float nutrient { get; set; }
	public float maxNutrient { get; set; } = 100.0f;
	public Ash(float nutrientAmount = 0.0f)
	{
		density = 19;
		color = Colors.Gray;
		nutrient = nutrientAmount; // ash is very nutritious (yum :3)
	}

	public override bool canMoveDownOnElement(Element elementWhereMovement)
	{
		return PowderBehavior.CanMoveDownOnElement(elementWhereMovement);
	}

	public override bool canMoveSideOnElement(Element elementWhereMovement)
	{
		return PowderBehavior.CanMoveSideOnElement(elementWhereMovement);
	}

	public override void update(Element[,] oldElementArray, int x, int y, int maxX, int maxY, int T)
	{
		PowderBehavior.Update(this, oldElementArray, x, y, maxX, maxY, T);
	}
}
