using Godot;

public class Ash : Element, IPowder, ILife
{
	public float nutrient { get; set; }= 0;
	public float maxNutrient => 100.0f;
	public float wetness { get; set; } = 0f;
	public float maxWetness => 100.0f;
	public Ash(float nutrientAmount = 0.0f, float wetnessAmount = 0.0f)
	{
		density = 19;
		color = Colors.Gray;
		nutrient = nutrientAmount; // ash is very nutritious (yum :3)
		wetness = wetnessAmount;
	}
	
	public override void update(Element[,] oldGrid, int x, int y, int maxX, int maxY, int T)
	{
		if (wetness <= 0.0f && nutrient <= 0.0f)
		{
			GridManager.Instance.RequestDeletion(x, y, maxX, maxY, null);
			return;
		}
		PowderBehavior.Update(this, oldGrid, x, y, maxX, maxY, T);
	}
}
