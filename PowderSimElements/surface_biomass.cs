using Godot;

public class SurfBiomass : Element, IPowder, ISolid, ILife
{
	public float wetness;
	public float nutrient;
	public float maxNutrient { get; set; } = 10.0f;
	
	public SurfBiomass(float startingWetness, float startingNutrient)
	{
		density = 20;
		color = Colors.DarkGreen;
		wetness = startingWetness;
		nutrient = startingNutrient;
		modulateColor();
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

	override public string getState()
	{
		return base.getState() + ";" + wetness + ";" + nutrient;
	}

	override public int setState(string state)
	{
		int i = base.setState(state);
		string[] stateArgs = state.Split(";", false);
		wetness = stateArgs[i++].ToFloat();
		nutrient = stateArgs[i++].ToFloat();
		return i;
	}

	override public string inspectInfo()
	{
		return base.inspectInfo() + $"  Wetness: {wetness:F3}\n  Nutrient: {nutrient:F3}\n";
	}
}
